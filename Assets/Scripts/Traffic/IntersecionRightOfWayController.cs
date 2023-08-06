using System.Collections.Generic;
using UnityEngine;

public class IntersecionRightOfWayController : MonoBehaviour
{
    private IntersectionTrafficLightsController _trafficLightController;

    [SerializeField] private Transform _northCheckPoint;
    [SerializeField] private Transform _eastCheckPoint;
    [SerializeField] private Transform _southCheckPoint;
    [SerializeField] private Transform _westCheckPoint;

    private const float _VEHICLE_DIRECTION_CHECK_DISTANCE = 5f;
    private bool _doesNorthSouthHaveRightOfWay = false;
    private bool _doesEastWestHaveRightOfWay = false;

    private IDictionary<Direction, Transform> _directionToCheckPointMap;

    private void OnDrawGizmos()
    {
        // Draw detection zone
        Gizmos.color = new Color(255, 0, 0);

        Gizmos.DrawLine(this._northCheckPoint.position, this._northCheckPoint.position.GetPointInDirection(new[] { Direction.South }, _VEHICLE_DIRECTION_CHECK_DISTANCE));
        Gizmos.DrawLine(this._eastCheckPoint.position, this._eastCheckPoint.position.GetPointInDirection(new[] { Direction.West }, _VEHICLE_DIRECTION_CHECK_DISTANCE));
        Gizmos.DrawLine(this._southCheckPoint.position, this._southCheckPoint.position.GetPointInDirection(new[] { Direction.North }, _VEHICLE_DIRECTION_CHECK_DISTANCE));
        Gizmos.DrawLine(this._westCheckPoint.position, this._westCheckPoint.position.GetPointInDirection(new[] { Direction.East }, _VEHICLE_DIRECTION_CHECK_DISTANCE));
    }

    private void Awake()
    {
        this._trafficLightController = GetComponent<IntersectionTrafficLightsController>();
        this._trafficLightController.OnTrafficLightColorChange += this.OnTrafficLightColorChange;
        this.InitializeMap();
    }

    private void OnDestroy()
    {
        this._trafficLightController.OnTrafficLightColorChange -= this.OnTrafficLightColorChange;
    }

    private void Update()
    {
        IAiMovementController vehicleInNorthboundLane = this.GetAiVehicleMovementControllerInDirection(Direction.North);
        IAiMovementController vehicleInEastboundLane = this.GetAiVehicleMovementControllerInDirection(Direction.East);
        IAiMovementController vehicleInSouthboundLane = this.GetAiVehicleMovementControllerInDirection(Direction.South);
        IAiMovementController vehicleInWestboundLane = this.GetAiVehicleMovementControllerInDirection(Direction.West);

        vehicleInNorthboundLane?.SetHasRightOfWay(this._doesNorthSouthHaveRightOfWay);
        vehicleInEastboundLane?.SetHasRightOfWay(this._doesEastWestHaveRightOfWay);
        vehicleInSouthboundLane?.SetHasRightOfWay(this._doesNorthSouthHaveRightOfWay);
        vehicleInWestboundLane?.SetHasRightOfWay(this._doesEastWestHaveRightOfWay);
    }

    private void OnTrafficLightColorChange(TrafficDirection direction, bool isGreen)
    {
        if (direction == TrafficDirection.NorthSouth)
            this._doesNorthSouthHaveRightOfWay = isGreen;
        else
            this._doesEastWestHaveRightOfWay = isGreen;
    }

    private IAiMovementController GetAiVehicleMovementControllerInDirection(Direction direction)
    {
        Vector3 directionCheckPoint = this._directionToCheckPointMap[direction].position;
        bool isVehicleInDirectionBoundLane = Physics.Linecast(directionCheckPoint, directionCheckPoint.GetPointInDirection(new[] { direction.GetOppositeDirection() }, _VEHICLE_DIRECTION_CHECK_DISTANCE), out RaycastHit hit, LayerMask.GetMask(Constants.LayerNames.Vehicle));
        if (!isVehicleInDirectionBoundLane) { return null; }

        bool didFindMovementController = hit.collider.transform.parent.TryGetComponent(out IAiMovementController movementController);
        if (!didFindMovementController) { return null; }

        return movementController;
    }

    private void InitializeMap()
    {
        this._directionToCheckPointMap = new Dictionary<Direction, Transform>()
        {
            { Direction.North, this._northCheckPoint },
            { Direction.East, this._eastCheckPoint },
            { Direction.South, this._southCheckPoint },
            { Direction.West, this._westCheckPoint },
        };
    }
}
