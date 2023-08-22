using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class IntersecionNpcRightOfWayController : NetworkBehaviour
{
    private IntersectionTrafficLightsController _trafficLightController;

    [Header("Checkpoints")]
    [SerializeField] private Transform _northEastCheckPoint;
    [SerializeField] private Transform _southEastCheckPoint;
    [SerializeField] private Transform _southWestCheckPoint;
    [SerializeField] private Transform _northWestCheckPoint;

    private const float _PEDESTRIAN_CHECK_DISTANCE_X = 3f;
    private const float _PEDESTRIAN_CHECK_DISTANCE_Y = 0.5f;
    private const float _PEDESTRIAN_CHECK_DISTANCE_Z = 3.5f;

    private bool _doesNorthSouthHaveGreenLight = false;
    private bool _doesEastWestHaveGreenLight = false;

    private IDictionary<IntersectionCornerDirection, Transform> _directionToCheckPointMap;

    private void OnDrawGizmos()
    {
        // Draw detection zone
        Gizmos.color = new Color(255, 0, 0);
        Gizmos.DrawWireCube(this._northEastCheckPoint.position, new(_PEDESTRIAN_CHECK_DISTANCE_X, _PEDESTRIAN_CHECK_DISTANCE_Y, _PEDESTRIAN_CHECK_DISTANCE_Z));
        Gizmos.DrawWireCube(this._southEastCheckPoint.position, new(_PEDESTRIAN_CHECK_DISTANCE_X, _PEDESTRIAN_CHECK_DISTANCE_Y, _PEDESTRIAN_CHECK_DISTANCE_Z));
        Gizmos.DrawWireCube(this._southWestCheckPoint.position, new(_PEDESTRIAN_CHECK_DISTANCE_X, _PEDESTRIAN_CHECK_DISTANCE_Y, _PEDESTRIAN_CHECK_DISTANCE_Z));
        Gizmos.DrawWireCube(this._northWestCheckPoint.position, new(_PEDESTRIAN_CHECK_DISTANCE_X, _PEDESTRIAN_CHECK_DISTANCE_Y, _PEDESTRIAN_CHECK_DISTANCE_Z));
    }

    private void Awake()
    {
        this._trafficLightController = GetComponent<IntersectionTrafficLightsController>();
        this._trafficLightController.OnTrafficLightColorChange += this.OnTrafficLightColorChange;
        this.InitializeMap();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        this._trafficLightController.OnTrafficLightColorChange -= this.OnTrafficLightColorChange;
    }

    private void Update()
    {
        if (!this.IsHost) { return; }

        this.HandlePedestrianCornerUpdate(IntersectionCornerDirection.NorthEast);
        this.HandlePedestrianCornerUpdate(IntersectionCornerDirection.SouthEast);
        this.HandlePedestrianCornerUpdate(IntersectionCornerDirection.SouthWest);
        this.HandlePedestrianCornerUpdate(IntersectionCornerDirection.NorthWest);
    }

    private void HandlePedestrianCornerUpdate(IntersectionCornerDirection corner)
    {
        NpcMovementController[] cornerNpcs = this.GetNpcMovementControllersInCorner(corner);

        foreach (NpcMovementController npc in cornerNpcs)
        {
            if (this.isNpcGoingIntoIntersection(corner, npc.UpcomingDirection))
            {
                npc.SetIsAtIntersection(true);
                npc.SetIsAtRedLightOrStopSign(!this.doesNpcHaveGreenLight(npc.UpcomingDirection));
                npc.SetHasRightOfWay(this.doesNpcHaveGreenLight(npc.UpcomingDirection));
            }
            else
            {
                npc.SetIsAtIntersection(false);
                npc.SetIsAtRedLightOrStopSign(false);
                npc.SetHasRightOfWay(false);
            }
        }
    }

    private bool isNpcGoingIntoIntersection(IntersectionCornerDirection corner, Direction direction)
    {
        if (corner == IntersectionCornerDirection.NorthEast)
            return direction == Direction.South || direction == Direction.West;
        else if (corner == IntersectionCornerDirection.SouthEast)
            return direction == Direction.North || direction == Direction.West;
        else if (corner == IntersectionCornerDirection.SouthWest)
            return direction == Direction.East || direction == Direction.North;
        else if (corner == IntersectionCornerDirection.NorthWest)
            return direction == Direction.East || direction == Direction.South;

        return false;
    }

    private bool doesNpcHaveGreenLight(Direction direction)
    {
        if (this._doesNorthSouthHaveGreenLight)
            return direction == Direction.North || direction == Direction.South;
        else if (this._doesEastWestHaveGreenLight)
            return direction == Direction.East || direction == Direction.West;

        return false;
    }

    private NpcMovementController[] GetNpcMovementControllersInCorner(IntersectionCornerDirection direction)
    {
        List<NpcMovementController> controllers = new();
        Vector3 checkPoint = this._directionToCheckPointMap[direction].position;
        RaycastHit[] charactersInCheckPoint = Physics.BoxCastAll(checkPoint, new(_PEDESTRIAN_CHECK_DISTANCE_X / 2f, _PEDESTRIAN_CHECK_DISTANCE_Y / 2f, _PEDESTRIAN_CHECK_DISTANCE_Z / 2f), Vector3.forward, Quaternion.identity, 0.1f, LayerMask.GetMask(Constants.LayerNames.Character));
        RaycastHit[] npcInCheckPoint = charactersInCheckPoint.Where(character => character.collider.tag != Constants.TagNames.Player).ToArray();
        if (npcInCheckPoint.Length == 0) { return controllers.ToArray(); }

        foreach (RaycastHit npc in npcInCheckPoint)
        {
            bool didFindMovementController = npc.collider.transform.TryGetComponent(out NpcMovementController movementController);

            if (didFindMovementController)
                controllers.Add(movementController);
        }

        return controllers.ToArray();
    }

    private void OnTrafficLightColorChange(TrafficDirection direction, bool isGreen)
    {
        if (direction == TrafficDirection.NorthSouth)
            this._doesNorthSouthHaveGreenLight = isGreen;
        else
            this._doesEastWestHaveGreenLight = isGreen;
    }

    private void InitializeMap()
    {
        this._directionToCheckPointMap = new Dictionary<IntersectionCornerDirection, Transform>()
        {
            { IntersectionCornerDirection.NorthEast, this._northEastCheckPoint },
            { IntersectionCornerDirection.SouthEast, this._southEastCheckPoint },
            { IntersectionCornerDirection.SouthWest, this._southWestCheckPoint },
            { IntersectionCornerDirection.NorthWest, this._northWestCheckPoint },
        };
    }

    private enum IntersectionCornerDirection
    {
        NorthEast,
        SouthEast,
        SouthWest,
        NorthWest
    }
}
