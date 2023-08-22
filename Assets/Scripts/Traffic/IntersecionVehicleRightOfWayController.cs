using System.Collections.Generic;
using UnityEngine;

public class IntersecionVehicleRightOfWayController : MonoBehaviour
{
    private IntersectionTrafficLightsController _trafficLightController;

    [Header("Enter Checkpoints")]
    [SerializeField] private Transform _northEnterCheckPoint;
    [SerializeField] private Transform _eastEnterCheckPoint;
    [SerializeField] private Transform _southEnterCheckPoint;
    [SerializeField] private Transform _westEnterCheckPoint;

    [Header("Exit Checkpoints")]
    [SerializeField] private Transform _northExitCheckPoint;
    [SerializeField] private Transform _eastExitCheckPoint;
    [SerializeField] private Transform _southExitCheckPoint;
    [SerializeField] private Transform _westExitCheckPoint;

    private const float _VEHICLE_DIRECTION_CHECK_DISTANCE = 5f;
    private bool _doesNorthSouthHaveGreenLight = false;
    private bool _doesEastWestHaveGreenLight = false;
    private HashSet<AiVehicleInIntersectionData> _aiCarsDataInsideIntersection = new();

    private IDictionary<Direction, Transform> _directionToEnterCheckPointMap;
    private IDictionary<Direction, Transform> _directionToExitCheckPointMap;

    public struct AiVehicleInIntersectionData
    {
        public VehicleAiMovementController MovementController { get; private set; }
        public Direction DirectionComingIn { get; private set; }
        public Direction DirectionComingOut { get; private set; }

        public AiVehicleInIntersectionData(VehicleAiMovementController movementController, Direction directionComingIn, Direction directionComingOut)
        {
            this.MovementController = movementController;
            this.DirectionComingIn = directionComingIn;
            this.DirectionComingOut = directionComingOut;
        }
    }

    private void OnDrawGizmos()
    {
        // Draw enter detection zone
        Gizmos.color = new Color(255, 0, 0);
        Gizmos.DrawLine(this._northEnterCheckPoint.position, this._northEnterCheckPoint.position.GetPointInDirection(new[] { Direction.South }, _VEHICLE_DIRECTION_CHECK_DISTANCE));
        Gizmos.DrawLine(this._eastEnterCheckPoint.position, this._eastEnterCheckPoint.position.GetPointInDirection(new[] { Direction.West }, _VEHICLE_DIRECTION_CHECK_DISTANCE));
        Gizmos.DrawLine(this._southEnterCheckPoint.position, this._southEnterCheckPoint.position.GetPointInDirection(new[] { Direction.North }, _VEHICLE_DIRECTION_CHECK_DISTANCE));
        Gizmos.DrawLine(this._westEnterCheckPoint.position, this._westEnterCheckPoint.position.GetPointInDirection(new[] { Direction.East }, _VEHICLE_DIRECTION_CHECK_DISTANCE));

        // Draw exit detection zone
        Gizmos.color = new Color(255, 255, 0);
        Gizmos.DrawLine(this._northExitCheckPoint.position, this._northExitCheckPoint.position.GetPointInDirection(new[] { Direction.South }, _VEHICLE_DIRECTION_CHECK_DISTANCE));
        Gizmos.DrawLine(this._eastExitCheckPoint.position, this._eastExitCheckPoint.position.GetPointInDirection(new[] { Direction.West }, _VEHICLE_DIRECTION_CHECK_DISTANCE));
        Gizmos.DrawLine(this._southExitCheckPoint.position, this._southExitCheckPoint.position.GetPointInDirection(new[] { Direction.North }, _VEHICLE_DIRECTION_CHECK_DISTANCE));
        Gizmos.DrawLine(this._westExitCheckPoint.position, this._westExitCheckPoint.position.GetPointInDirection(new[] { Direction.East }, _VEHICLE_DIRECTION_CHECK_DISTANCE));
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
        VehicleAiMovementController vehicleEnteringInNorthboundLane = this.GetAiVehicleMovementControllerInDirection(Direction.North, true);
        VehicleAiMovementController vehicleEnteringInEastboundLane = this.GetAiVehicleMovementControllerInDirection(Direction.East, true);
        VehicleAiMovementController vehicleEnteringInSouthboundLane = this.GetAiVehicleMovementControllerInDirection(Direction.South, true);
        VehicleAiMovementController vehicleEnteringInWestboundLane = this.GetAiVehicleMovementControllerInDirection(Direction.West, true);

        VehicleAiMovementController vehicleExitingInNorthboundLane = this.GetAiVehicleMovementControllerInDirection(Direction.North, false);
        VehicleAiMovementController vehicleExitingInEastboundLane = this.GetAiVehicleMovementControllerInDirection(Direction.East, false);
        VehicleAiMovementController vehicleExitingInSouthboundLane = this.GetAiVehicleMovementControllerInDirection(Direction.South, false);
        VehicleAiMovementController vehicleExitingInWestboundLane = this.GetAiVehicleMovementControllerInDirection(Direction.West, false);

        this.OnVehicleInEnterCheckpointUpdate(true, vehicleEnteringInNorthboundLane);
        this.OnVehicleInEnterCheckpointUpdate(false, vehicleEnteringInEastboundLane);
        this.OnVehicleInEnterCheckpointUpdate(true, vehicleEnteringInSouthboundLane);
        this.OnVehicleInEnterCheckpointUpdate(false, vehicleEnteringInWestboundLane);

        this.OnVehicleExitingIntersectionUpdate(vehicleExitingInNorthboundLane);
        this.OnVehicleExitingIntersectionUpdate(vehicleExitingInEastboundLane);
        this.OnVehicleExitingIntersectionUpdate(vehicleExitingInSouthboundLane);
        this.OnVehicleExitingIntersectionUpdate(vehicleExitingInWestboundLane);

        this.HandleGreenLightTrafficUpdate(true, vehicleEnteringInNorthboundLane, vehicleEnteringInSouthboundLane, Direction.North);
        this.HandleGreenLightTrafficUpdate(false, vehicleEnteringInEastboundLane, vehicleEnteringInWestboundLane, Direction.East);
    }

    private void OnVehicleInEnterCheckpointUpdate(bool isNorthSouthTraffic, VehicleAiMovementController movementController)
    {
        if (movementController == null) { return; }

        bool doesVehicleHaveGreenLight = isNorthSouthTraffic ? this._doesNorthSouthHaveGreenLight : this._doesEastWestHaveGreenLight;

        movementController.SetIsAtIntersection(true);
        movementController.SetIsAtRedLightOrStopSign(!doesVehicleHaveGreenLight);

        if (!doesVehicleHaveGreenLight)
            movementController.SetHasRightOfWay(false);
    }

    private void HandleGreenLightTrafficUpdate(bool isNorthSouthTraffic, VehicleAiMovementController firstDirectionVehicle, VehicleAiMovementController secondDirectionVehicle, Direction firstVehicleDirection)
    {
        bool isFirstVehicleNull = firstDirectionVehicle == null;
        bool isSecondVehicleNull = secondDirectionVehicle == null;

        bool doesTrafficHaveGreenLight = isNorthSouthTraffic ? this._doesNorthSouthHaveGreenLight : this._doesEastWestHaveGreenLight;

        if (!doesTrafficHaveGreenLight || (isFirstVehicleNull && isSecondVehicleNull)) { return; }

        // At least one vehicle in either direction
        VehicleAiMovementController vehicleToProcessFirst = null;
        VehicleAiMovementController vehicleToProcessSecond = null;

        // Only one direction has vehicle
        if (!isFirstVehicleNull && isSecondVehicleNull || isFirstVehicleNull && !isSecondVehicleNull)
        {
            vehicleToProcessFirst = !isFirstVehicleNull ? firstDirectionVehicle : secondDirectionVehicle;
        }
        else
        {
            // Both directions have vehicle
            bool isFirstVehicleTurningLeft = firstVehicleDirection.IsNextDirectionLeftTurn(firstDirectionVehicle.UpcomingDirection);
            bool isFirstVehicleGoingStraight = firstVehicleDirection == firstDirectionVehicle.UpcomingDirection;
            bool isFirstVehicleTurningRight = !isFirstVehicleTurningLeft && !isFirstVehicleGoingStraight;

            vehicleToProcessFirst = isFirstVehicleGoingStraight || isFirstVehicleTurningRight ? firstDirectionVehicle : secondDirectionVehicle;
            vehicleToProcessSecond = isFirstVehicleGoingStraight || isFirstVehicleTurningRight ? secondDirectionVehicle : firstDirectionVehicle;
        }

        if (vehicleToProcessFirst != null)
            this.OnDetectedVehicleEnteringOnGreenLight(vehicleToProcessFirst, vehicleToProcessFirst == firstDirectionVehicle ? firstVehicleDirection : firstVehicleDirection.GetOppositeDirection());
        if (vehicleToProcessSecond != null)
            this.OnDetectedVehicleEnteringOnGreenLight(vehicleToProcessSecond, vehicleToProcessSecond == secondDirectionVehicle ? firstVehicleDirection.GetOppositeDirection() : firstVehicleDirection);
    }

    private void OnDetectedVehicleEnteringOnGreenLight(VehicleAiMovementController movementController, Direction directionComingIn)
    {
        movementController.SetIsAtRedLightOrStopSign(false);
        bool isTurningLeft = directionComingIn.IsNextDirectionLeftTurn(movementController.UpcomingDirection);
        bool isGoingStraight = directionComingIn == movementController.UpcomingDirection;
        bool isTurningRight = !isTurningLeft && !isGoingStraight;

        if (isTurningRight)
        {
            bool isACarTurningLeftIntoSameLane = this.IsAiVehicleInIntersection(directionComingIn.GetOppositeDirection(), movementController.UpcomingDirection);

            if (isACarTurningLeftIntoSameLane)
                movementController.SetHasRightOfWay(false);
            else
                this.OnCarHavingRightOfWay(movementController, directionComingIn);
        }
        else if (isGoingStraight)
        {
            bool isACarTurningLeftAcrossFromMe = this.IsAiVehicleTurningLeftInIntersection(directionComingIn.GetOppositeDirection());

            if (isACarTurningLeftAcrossFromMe)
                movementController.SetHasRightOfWay(false);
            else
                this.OnCarHavingRightOfWay(movementController, directionComingIn);
        }
        else if (isTurningLeft)
        {
            bool isACarAcrossFromMeGoingStraight = this.IsAiVehicleInIntersection(directionComingIn.GetOppositeDirection(), directionComingIn.GetOppositeDirection());
            bool isACarTurningRightIntoSameLane = this.IsAiVehicleInIntersection(directionComingIn.GetOppositeDirection(), movementController.UpcomingDirection);

            if (isACarAcrossFromMeGoingStraight)
                movementController.SetHasRightOfWay(false);
            else if (isACarTurningRightIntoSameLane)
                movementController.SetHasRightOfWay(false);
            else
                this.OnCarHavingRightOfWay(movementController, directionComingIn);
        }
    }

    private void OnVehicleExitingIntersectionUpdate(VehicleAiMovementController movementController)
    {
        if (movementController == null) { return; }

        movementController.SetIsAtIntersection(false);
        movementController.SetHasRightOfWay(false);
        this._aiCarsDataInsideIntersection.RemoveWhere((vehicleData) => vehicleData.MovementController == movementController);
    }

    private void OnCarHavingRightOfWay(VehicleAiMovementController movementController, Direction directionComingIn)
    {
        movementController.SetHasRightOfWay(true);
        this._aiCarsDataInsideIntersection.Add(new(movementController, directionComingIn, movementController.UpcomingDirection));
    }

    private bool IsAiVehicleTurningLeftInIntersection(Direction directionComingIn)
    {
        bool isCarTurningLeftInIntersectionFromDirection = false;

        foreach (AiVehicleInIntersectionData data in this._aiCarsDataInsideIntersection)
        {
            if (data.DirectionComingIn == directionComingIn && directionComingIn.IsNextDirectionLeftTurn(data.MovementController.UpcomingDirection))
            {
                isCarTurningLeftInIntersectionFromDirection = true;
                break;
            }
        }

        return isCarTurningLeftInIntersectionFromDirection;
    }

    private bool IsAiVehicleInIntersection(Direction directionComingIn, Direction directionComingOut)
    {
        bool isCarInIntersection = false;

        foreach (AiVehicleInIntersectionData data in this._aiCarsDataInsideIntersection)
        {
            if (data.DirectionComingIn == directionComingIn && data.DirectionComingOut == directionComingOut)
            {
                isCarInIntersection = true;
                break;
            }
        }

        return isCarInIntersection;
    }

    private void OnTrafficLightColorChange(TrafficDirection direction, bool isGreen)
    {
        if (direction == TrafficDirection.NorthSouth)
            this._doesNorthSouthHaveGreenLight = isGreen;
        else
            this._doesEastWestHaveGreenLight = isGreen;
    }

    private bool IsTurningLeft(Direction currentDirection, Direction nextDirection) => currentDirection == Direction.North && nextDirection == Direction.West || currentDirection == Direction.East && nextDirection == Direction.North || currentDirection == Direction.South && nextDirection == Direction.East || currentDirection == Direction.West && nextDirection == Direction.South;

    private VehicleAiMovementController GetAiVehicleMovementControllerInDirection(Direction direction, bool isEntering)
    {
        Vector3 directionCheckPoint = isEntering ? this._directionToEnterCheckPointMap[direction].position : this._directionToExitCheckPointMap[direction].position;
        bool isVehicleInDirectionBoundLane = Physics.Linecast(directionCheckPoint, directionCheckPoint.GetPointInDirection(new[] { direction.GetOppositeDirection() }, _VEHICLE_DIRECTION_CHECK_DISTANCE), out RaycastHit hit, LayerMask.GetMask(Constants.LayerNames.Vehicle));
        if (!isVehicleInDirectionBoundLane) { return null; }

        bool didFindMovementController = hit.collider.transform.parent.TryGetComponent(out VehicleAiMovementController movementController);
        if (!didFindMovementController) { return null; }

        return movementController;
    }

    private void InitializeMap()
    {
        this._directionToEnterCheckPointMap = new Dictionary<Direction, Transform>()
        {
            { Direction.North, this._northEnterCheckPoint },
            { Direction.East, this._eastEnterCheckPoint },
            { Direction.South, this._southEnterCheckPoint },
            { Direction.West, this._westEnterCheckPoint },
        };
        this._directionToExitCheckPointMap = new Dictionary<Direction, Transform>()
        {
            { Direction.North, this._northExitCheckPoint },
            { Direction.East, this._eastExitCheckPoint },
            { Direction.South, this._southExitCheckPoint },
            { Direction.West, this._westExitCheckPoint },
        };
    }
}
