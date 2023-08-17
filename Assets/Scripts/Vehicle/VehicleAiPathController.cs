using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Pathfinding;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(VehicleAiMovementController))]
[RequireComponent(typeof(Seeker))]
public class VehicleAiPathController : NetworkBehaviour
{
    private VehicleSeatController _seatController;
    private Seeker _seeker;

    private Path _currentDirectionPath = null;
    private int _currentWaypoint = 0;
    private DirectionWithTurningPoint[] _directionsWithTurningPoints = new DirectionWithTurningPoint[0];
    private int _directionSwitchCount = 0;
    [SerializeField] private float _waypointEnteredDistance = 3;

    [SerializeField] private Transform _target;

    public event Action<Vector3> OnNextNodeChange;

    private const string _ALL_DIRECTIONS_GRAPH_NAME = "AllDirections";
    private const string _NORTH_DIRECTION_GRAPH_NAME = "North";
    private const string _EAST_DIRECTION_GRAPH_NAME = "East";
    private const string _SOUTH_DIRECTION_GRAPH_NAME = "South";
    private const string _WEST_DIRECTION_GRAPH_NAME = "West";

    private static Vector3[] _ALL_NORTH_POINTS;
    private static Vector3[] _ALL_EAST_POINTS;
    private static Vector3[] _ALL_SOUTH_POINTS;
    private static Vector3[] _ALL_WEST_POINTS;

    private Direction _upcomingDirection;
    private const float _UPCOMING_DIRECTION_NOTICE_DISTANCE = 25f;
    public event Action<Direction> OnUpcomingDirectionChange;

    private static IDictionary<Direction, string> _DIRECTION_TO_GRAPH_NAME_MAP = new Dictionary<Direction, string>()
    {
        { Direction.None, _ALL_DIRECTIONS_GRAPH_NAME },
        { Direction.North, _NORTH_DIRECTION_GRAPH_NAME },
        { Direction.East, _EAST_DIRECTION_GRAPH_NAME },
        { Direction.South, _SOUTH_DIRECTION_GRAPH_NAME },
        { Direction.West, _WEST_DIRECTION_GRAPH_NAME },
    };

    private void Awake()
    {
        this._seatController = GetComponent<VehicleSeatController>();
        this._seeker = GetComponent<Seeker>();

        if (_ALL_NORTH_POINTS == null)
        {
            _ALL_NORTH_POINTS = GameObject.FindGameObjectsWithTag(Constants.TagNames.NorthDirection).Select(obj => obj.transform.position).ToArray();
            _ALL_EAST_POINTS = GameObject.FindGameObjectsWithTag(Constants.TagNames.EastDirection).Select(obj => obj.transform.position).ToArray();
            _ALL_SOUTH_POINTS = GameObject.FindGameObjectsWithTag(Constants.TagNames.SouthDirection).Select(obj => obj.transform.position).ToArray();
            _ALL_WEST_POINTS = GameObject.FindGameObjectsWithTag(Constants.TagNames.WestDirection).Select(obj => obj.transform.position).ToArray();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        this._seatController.TestAiDriver();
    }

    private async void Start()
    {
        this.SetUpcomingDirection(Direction.None);

        if (this._target == null)
        {
            this._target = GameObject.FindGameObjectWithTag("TestTarget").transform;

        }

        await this.GetWholeProperPath();
        this.FixStartingPositionAndRotation();
    }

    private void Update()
    {
        if (!this.IsOwner || !this._seatController.HasAiInDriverSeat || this._target == null || this._currentDirectionPath == null) { return; }

        this.UpdateUpcomingDirection();
        float distanceToNextWaypoint;

        // Check in a loop if we are close enough to the current waypoint to switch to the next one
        while (true)
        {
            distanceToNextWaypoint = Vector3.Distance(transform.position, this._currentDirectionPath.vectorPath[this._currentWaypoint]);

            if (distanceToNextWaypoint >= this._waypointEnteredDistance) { break; }
            if (this._currentWaypoint + 1 >= this._currentDirectionPath.vectorPath.Count)
            {
                // We reached the last waypoint for the current direction
                this.OnLastWaypointForDirectionReached();
                break;
            }

            // Entered waypoint
            this._currentWaypoint++;
            OnNextNodeChange?.Invoke(this._currentDirectionPath.vectorPath[this._currentWaypoint]);
        }
    }

    private void UpdateUpcomingDirection()
    {
        bool isOnLastStraightaway = this._directionSwitchCount == this._directionsWithTurningPoints.Count() - 1;

        if (isOnLastStraightaway) { return; }

        Vector3 nextTurningPoint = this._directionsWithTurningPoints[this._directionSwitchCount].TurningPoint;
        float distanceToNextTurningPoint = Vector3.Distance(transform.position, nextTurningPoint);
        Direction nextDirection = this._directionsWithTurningPoints[this._directionSwitchCount + 1].Direction;

        if (distanceToNextTurningPoint <= _UPCOMING_DIRECTION_NOTICE_DISTANCE)
            this.SetUpcomingDirection(nextDirection);
    }

    private async UniTask<Path> GetWholePathWithoutProperLanes()
    {
        this._seeker.drawGizmos = false;
        this._seeker.detailedGizmos = false;
        Path path = this._seeker.StartPath(transform.position, this._target.position, (Path p) => { }, GraphMask.FromGraphName(_ALL_DIRECTIONS_GRAPH_NAME));
        await path.WaitForPath();
        return path;
    }

    private async UniTask GetWholeProperPath()
    {
        this._currentWaypoint = 0;
        this._currentDirectionPath = null;
        this._directionSwitchCount = 0;
        Path pathWithoutProperLanes = await this.GetWholePathWithoutProperLanes();
        this._directionsWithTurningPoints = this.GetDirectionsWithTurningPointsForPath(pathWithoutProperLanes);
        bool isOnlyOneDirection = this._directionsWithTurningPoints.Count() == 1;
        await this.UpdatePath(transform.position, !isOnlyOneDirection ? this._directionsWithTurningPoints.First().TurningPoint : this._target.position, this._directionsWithTurningPoints.First().Direction);
        this.SetUpcomingDirection(this._directionsWithTurningPoints.First().Direction); // SEE IF THIS WORKS WHEN THERES ONLY ONE DIRECTION (STRAIGHT LINE TO TARGET)
    }

    private async UniTask UpdatePath(Vector3 startPoint, Vector3 endPoint, Direction direction)
    {
        Path path = this._seeker.StartPath(startPoint, endPoint, (Path p) => { }, GraphMask.FromGraphName(_DIRECTION_TO_GRAPH_NAME_MAP[direction]));
        await path.WaitForPath();
        this._currentDirectionPath = path;
        this._seeker.drawGizmos = true;
        this._seeker.detailedGizmos = true;
        OnNextNodeChange?.Invoke(this._currentDirectionPath.vectorPath[this._currentWaypoint]);
    }

    private async void OnLastWaypointForDirectionReached()
    {
        if (this._directionSwitchCount == this._directionsWithTurningPoints.Count() - 1)
        {
            // Reached final waypoint
            NetworkObject.Despawn();
            this._currentWaypoint = 0;
            this._currentDirectionPath = null;
            this._directionSwitchCount = 0;
            this.SetUpcomingDirection(Direction.None);
            return;
        }

        this._seeker.drawGizmos = false;
        this._seeker.detailedGizmos = false;
        this._currentWaypoint = 0;
        this._currentDirectionPath = null;

        bool isOnLastStraightaway = this._directionSwitchCount + 1 == this._directionsWithTurningPoints.Count() - 1;
        Direction previousDirection = this._directionsWithTurningPoints[this._directionSwitchCount].Direction;
        Direction nextDirection = this._directionsWithTurningPoints[this._directionSwitchCount + 1].Direction;
        Vector3 startPoint = isOnLastStraightaway ? transform.position : this._directionsWithTurningPoints[this._directionSwitchCount].TurningPoint;
        startPoint = startPoint.GetPointInDirection(new[] { previousDirection, nextDirection });
        Vector3 endPoint = isOnLastStraightaway ? this._target.position : this._directionsWithTurningPoints[this._directionSwitchCount + 1].TurningPoint;

        this.SetUpcomingDirection(nextDirection);

        await this.UpdatePath(startPoint, endPoint, nextDirection);
        this._directionSwitchCount++;
    }

    private bool IsPointInDirection(Vector3 startPoint, Vector3 endPoint, Direction direction) => direction == Direction.North && startPoint.z > endPoint.z || direction == Direction.East && startPoint.x > endPoint.x || direction == Direction.South && startPoint.z < endPoint.z || direction == Direction.West && startPoint.x < endPoint.x;

    private DirectionWithTurningPoint[] GetDirectionsWithTurningPointsForPath(Path path)
    {
        List<Direction> directions = new();
        List<DirectionWithTurningPoint> directionsWithTurningPoint = new();
        int northCounts = 0;
        int iterationNorthWasIncremented = 0;
        int eastCounts = 0;
        int iterationEastWasIncremented = 0;
        int southCounts = 0;
        int iterationSouthWasIncremented = 0;
        int westCounts = 0;
        int iterationWestWasIncremented = 0;

        for (int i = 1; i < path.vectorPath.Count; i++)
        {
            Vector3 previousWaypointNode = path.vectorPath[i - 1];
            Vector3 waypointNode = path.vectorPath[i];

            // Logic so the counts only become 2 when moving in a new direction twice
            if (this.IsPointInDirection(previousWaypointNode, waypointNode, Direction.North))
            {
                northCounts = iterationNorthWasIncremented == i - 1 ? northCounts + 1 : 1;
                iterationNorthWasIncremented = i;
            }
            if (this.IsPointInDirection(previousWaypointNode, waypointNode, Direction.East))
            {
                eastCounts = iterationEastWasIncremented == i - 1 ? eastCounts + 1 : 1;
                iterationEastWasIncremented = i;
            }
            if (this.IsPointInDirection(previousWaypointNode, waypointNode, Direction.South))
            {
                southCounts = iterationSouthWasIncremented == i - 1 ? southCounts + 1 : 1;
                iterationSouthWasIncremented = i;
            }
            if (this.IsPointInDirection(previousWaypointNode, waypointNode, Direction.West))
            {
                westCounts = iterationWestWasIncremented == i - 1 ? westCounts + 1 : 1;
                iterationWestWasIncremented = i;
            }

            if (northCounts == 2)
            {
                directions.Add(Direction.North);
                eastCounts = 0;
                southCounts = 0;
                westCounts = 0;

                if (directions.Count > 1)
                {
                    Direction previousDirection = directions[^2];
                    Vector3 turningPoint = waypointNode;
                    bool isTurningLeft = previousDirection == Direction.East;

                    // Ensure turning point is set to the correct point before more calculations because A* path is inconsistent
                    if (isTurningLeft && _ALL_NORTH_POINTS.Any(northPoint => northPoint.IsEqual(turningPoint)))
                        turningPoint = turningPoint.GetPointInDirection(new[] { Direction.West });
                    else if (!isTurningLeft && !_ALL_NORTH_POINTS.Any(northPoint => northPoint.IsEqual(turningPoint)))
                        turningPoint = turningPoint.GetPointInDirection(new[] { Direction.East });

                    if (isTurningLeft)
                        turningPoint = turningPoint.GetPointInDirection(new[] { Direction.South }, 15f);
                    else
                        turningPoint = turningPoint.GetPointInDirection(new[] { Direction.South, Direction.South, Direction.East });

                    directionsWithTurningPoint.Add(new(previousDirection, turningPoint));
                }
            }
            else if (eastCounts == 2)
            {
                directions.Add(Direction.East);
                northCounts = 0;
                southCounts = 0;
                westCounts = 0;

                if (directions.Count > 1)
                {
                    Direction previousDirection = directions[^2];
                    Vector3 turningPoint = waypointNode;
                    bool isTurningLeft = previousDirection == Direction.South;

                    // Ensure turning point is set to the correct point before more calculations because A* path is inconsistent
                    if (isTurningLeft && _ALL_EAST_POINTS.Any(eastPoint => eastPoint.IsEqual(turningPoint)))
                        turningPoint = turningPoint.GetPointInDirection(new[] { Direction.North });
                    else if (!isTurningLeft && !_ALL_EAST_POINTS.Any(eastPoint => eastPoint.IsEqual(turningPoint)))
                        turningPoint = turningPoint.GetPointInDirection(new[] { Direction.South });

                    if (isTurningLeft)
                        turningPoint = turningPoint.GetPointInDirection(new[] { Direction.West }, 15f);
                    else
                        turningPoint = turningPoint.GetPointInDirection(new[] { Direction.West, Direction.West, Direction.South });

                    directionsWithTurningPoint.Add(new(previousDirection, turningPoint));
                }
            }
            else if (southCounts == 2)
            {
                directions.Add(Direction.South);
                northCounts = 0;
                eastCounts = 0;
                westCounts = 0;

                if (directions.Count > 1)
                {
                    Direction previousDirection = directions[^2];
                    Vector3 turningPoint = waypointNode;
                    bool isTurningLeft = previousDirection == Direction.West;

                    // Ensure turning point is set to the correct point before more calculations because A* path is inconsistent
                    if (isTurningLeft && _ALL_SOUTH_POINTS.Any(southPoint => southPoint.IsEqual(turningPoint)))
                        turningPoint = turningPoint.GetPointInDirection(new[] { Direction.East });
                    else if (!isTurningLeft && !_ALL_SOUTH_POINTS.Any(southPoint => southPoint.IsEqual(turningPoint)))
                        turningPoint = turningPoint.GetPointInDirection(new[] { Direction.West });

                    if (isTurningLeft)
                        turningPoint = turningPoint.GetPointInDirection(new[] { Direction.North }, 15f);
                    else
                        turningPoint = turningPoint.GetPointInDirection(new[] { Direction.North, Direction.North, Direction.West });

                    directionsWithTurningPoint.Add(new(previousDirection, turningPoint));
                }
            }
            else if (westCounts == 2)
            {
                directions.Add(Direction.West);
                northCounts = 0;
                eastCounts = 0;
                southCounts = 0;

                if (directions.Count > 1)
                {
                    Direction previousDirection = directions[^2];
                    Vector3 turningPoint = waypointNode;
                    bool isTurningLeft = previousDirection == Direction.North;

                    // Ensure turning point is set to the correct point before more calculations because A* path is inconsistent
                    if (isTurningLeft && _ALL_WEST_POINTS.Any(westPoint => westPoint.IsEqual(turningPoint)))
                        turningPoint = turningPoint.GetPointInDirection(new[] { Direction.South });
                    else if (!isTurningLeft && !_ALL_WEST_POINTS.Any(westPoint => westPoint.IsEqual(turningPoint)))
                        turningPoint = turningPoint.GetPointInDirection(new[] { Direction.North });

                    if (isTurningLeft)
                        turningPoint = turningPoint.GetPointInDirection(new[] { Direction.East }, 15f);
                    else
                        turningPoint = turningPoint.GetPointInDirection(new[] { Direction.East, Direction.East, Direction.North });

                    directionsWithTurningPoint.Add(new(previousDirection, turningPoint));
                }
            }
        }

        // Add last direction
        directionsWithTurningPoint.Add(new(directions.Last(), Vector3.zero));
        return directionsWithTurningPoint.ToArray();
    }

    private Direction GetDirectionCurrentlyFacing()
    {
        Direction direction = Direction.None;
        float xRotation = transform.forward.x;
        float zRotation = transform.forward.z;

        if (xRotation <= 0.75f && xRotation >= 0f && zRotation < 0f || xRotation >= -0.75f && xRotation <= 0f && zRotation < 0f)
            direction = Direction.North;
        else if (xRotation <= -0.75f && xRotation >= -1f)
            direction = Direction.East;
        else if (xRotation >= -0.75f && xRotation <= 0f && zRotation > 0f || xRotation <= 0.75f && xRotation >= 0f && zRotation > 0f)
            direction = Direction.South;
        else if (xRotation >= 0.75f)
            direction = Direction.West;

        return direction;
    }

    private void FixStartingPositionAndRotation()
    {
        if (this._directionsWithTurningPoints.Count() == 0) { return; }

        Direction firstPathDirection = this._directionsWithTurningPoints.First().Direction;
        Direction directionCurrentlyFacing = this.GetDirectionCurrentlyFacing();

        if (firstPathDirection == directionCurrentlyFacing) { return; }
        transform.position = this._currentDirectionPath.vectorPath[0];
        transform.LookAt(transform.position.GetPointInDirection(new[] { firstPathDirection }));
    }

    private void SetUpcomingDirection(Direction direction)
    {
        if (this._upcomingDirection == direction) { return; }

        this._upcomingDirection = direction;
        OnUpcomingDirectionChange?.Invoke(this._upcomingDirection);
    }

    private struct DirectionWithTurningPoint
    {
        public Direction Direction { get; private set; }
        public Vector3 TurningPoint { get; private set; }

        public DirectionWithTurningPoint(Direction direction, Vector3 turningPoint)
        {
            this.Direction = direction;
            this.TurningPoint = turningPoint;
        }
    }
}
