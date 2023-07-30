using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Pathfinding;
using Unity.Netcode;
using UnityEngine;

public class VehicleAiPathController : NetworkBehaviour
{
    private VehicleSeatController _seatController;
    private Seeker _seeker;

    private Path _currentDirectionPath = null;
    private int _currentWaypoint = 0;
    private DirectionWithTurningPoint[] _directionsWithTurningPoints;
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

    private enum PathDirection
    {
        North,
        East,
        South,
        West,
        None
    }

    private static IDictionary<PathDirection, string> _DIRECTION_TO_GRAPH_NAME_MAP = new Dictionary<PathDirection, string>()
    {
        { PathDirection.None, _ALL_DIRECTIONS_GRAPH_NAME },
        { PathDirection.North, _NORTH_DIRECTION_GRAPH_NAME },
        { PathDirection.East, _EAST_DIRECTION_GRAPH_NAME },
        { PathDirection.South, _SOUTH_DIRECTION_GRAPH_NAME },
        { PathDirection.West, _WEST_DIRECTION_GRAPH_NAME },
    };

    private struct DirectionWithTurningPoint
    {
        public PathDirection Direction { get; private set; }
        public Vector3 TurningPoint { get; private set; }

        public DirectionWithTurningPoint(PathDirection direction, Vector3 turningPoint)
        {
            this.Direction = direction;
            this.TurningPoint = turningPoint;
        }
    }

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

    private void Start() => this.GetWholeProperPath();

    private void Update()
    {
        if (!this.IsOwner || !this._seatController.HasAiInDriverSeat || this._target == null || this._currentDirectionPath == null) { return; }

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

    private async UniTask<Path> GetWholePathWithoutProperLanes()
    {
        this._seeker.drawGizmos = false;
        this._seeker.detailedGizmos = false;
        Path path = this._seeker.StartPath(transform.position, this._target.position, (Path p) => { }, GraphMask.FromGraphName(_ALL_DIRECTIONS_GRAPH_NAME));
        await path.WaitForPath();
        return path;
    }

    private async void GetWholeProperPath()
    {
        this._currentWaypoint = 0;
        this._currentDirectionPath = null;
        this._directionSwitchCount = 0;
        Path pathWithoutProperLanes = await this.GetWholePathWithoutProperLanes();
        this._directionsWithTurningPoints = this.GetDirectionsWithTurningPointsForPath(pathWithoutProperLanes);
        await this.UpdatePath(transform.position, this._directionsWithTurningPoints.First().TurningPoint, this._directionsWithTurningPoints.First().Direction);
    }

    private async UniTask UpdatePath(Vector3 startPoint, Vector3 endPoint, PathDirection direction)
    {
        Path path = this._seeker.StartPath(startPoint, endPoint, (Path p) => { }, GraphMask.FromGraphName(_DIRECTION_TO_GRAPH_NAME_MAP[direction]));
        await path.WaitForPath();
        this._currentDirectionPath = path;
        this._seeker.drawGizmos = true;
        this._seeker.detailedGizmos = true;
        OnNextNodeChange?.Invoke(this._currentDirectionPath.vectorPath[this._currentWaypoint]);
    }

    private Vector3 GetPointInDirection(Vector3 startingPoint, PathDirection[] directions, float moveInterval = 5f)
    {
        Vector3 newPoint = startingPoint;

        foreach (PathDirection direction in directions)
        {
            if (direction == PathDirection.North)
            {
                newPoint.z -= moveInterval;
            }
            else if (direction == PathDirection.East)
            {
                newPoint.x -= moveInterval;
            }
            else if (direction == PathDirection.South)
            {
                newPoint.z += moveInterval;
            }
            else if (direction == PathDirection.West)
            {
                newPoint.x += moveInterval;
            }
        }

        return newPoint;
    }

    private async void OnLastWaypointForDirectionReached()
    {
        if (this._directionSwitchCount == this._directionsWithTurningPoints.Count() - 1)
        {
            // Reached final waypoint
            Destroy(gameObject);
            this._currentWaypoint = 0;
            this._currentDirectionPath = null;
            this._directionSwitchCount = 0;
            return;
        }

        this._seeker.drawGizmos = false;
        this._seeker.detailedGizmos = false;
        this._currentWaypoint = 0;
        this._currentDirectionPath = null;

        bool isOnLastStraightaway = this._directionSwitchCount + 1 == this._directionsWithTurningPoints.Count() - 1;
        PathDirection previousDirection = this._directionsWithTurningPoints[this._directionSwitchCount].Direction;
        PathDirection nextDirection = this._directionsWithTurningPoints[this._directionSwitchCount + 1].Direction;
        Vector3 startPoint = isOnLastStraightaway ? transform.position : this._directionsWithTurningPoints[this._directionSwitchCount].TurningPoint;
        startPoint = this.GetPointInDirection(startPoint, new[] { previousDirection, nextDirection });
        Vector3 endPoint = isOnLastStraightaway ? this._target.position : this._directionsWithTurningPoints[this._directionSwitchCount + 1].TurningPoint;

        await this.UpdatePath(startPoint, endPoint, nextDirection);
        this._directionSwitchCount++;
    }

    private bool IsPointInDirection(Vector3 startPoint, Vector3 endPoint, PathDirection direction) => direction == PathDirection.North && startPoint.z > endPoint.z || direction == PathDirection.East && startPoint.x > endPoint.x || direction == PathDirection.South && startPoint.z < endPoint.z || direction == PathDirection.West && startPoint.x < endPoint.x;

    private DirectionWithTurningPoint[] GetDirectionsWithTurningPointsForPath(Path path)
    {
        List<PathDirection> directions = new();
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
            if (this.IsPointInDirection(previousWaypointNode, waypointNode, PathDirection.North))
            {
                northCounts = iterationNorthWasIncremented == i - 1 ? northCounts + 1 : 1;
                iterationNorthWasIncremented = i;
            }
            if (this.IsPointInDirection(previousWaypointNode, waypointNode, PathDirection.East))
            {
                eastCounts = iterationEastWasIncremented == i - 1 ? eastCounts + 1 : 1;
                iterationEastWasIncremented = i;
            }
            if (this.IsPointInDirection(previousWaypointNode, waypointNode, PathDirection.South))
            {
                southCounts = iterationSouthWasIncremented == i - 1 ? southCounts + 1 : 1;
                iterationSouthWasIncremented = i;
            }
            if (this.IsPointInDirection(previousWaypointNode, waypointNode, PathDirection.West))
            {
                westCounts = iterationWestWasIncremented == i - 1 ? westCounts + 1 : 1;
                iterationWestWasIncremented = i;
            }

            if (northCounts == 2)
            {
                directions.Add(PathDirection.North);
                eastCounts = 0;
                southCounts = 0;
                westCounts = 0;

                if (directions.Count > 1)
                {
                    PathDirection previousDirection = directions[^2];
                    Vector3 turningPoint = waypointNode;
                    bool isTurningLeft = previousDirection == PathDirection.East;

                    // Ensure turning point is set to the correct point before more calculations because A* path is inconsistent
                    if (isTurningLeft && _ALL_NORTH_POINTS.Any(northPoint => northPoint.IsEqual(turningPoint)))
                    {
                        turningPoint = this.GetPointInDirection(turningPoint, new[] { PathDirection.West });
                    }

                    if (isTurningLeft)
                        turningPoint = this.GetPointInDirection(turningPoint, new[] { PathDirection.South }, 15f);
                    else
                        turningPoint = this.GetPointInDirection(turningPoint, new[] { PathDirection.South, PathDirection.South, PathDirection.East });

                    directionsWithTurningPoint.Add(new(previousDirection, turningPoint));
                }
            }
            else if (eastCounts == 2)
            {
                directions.Add(PathDirection.East);
                northCounts = 0;
                southCounts = 0;
                westCounts = 0;

                if (directions.Count > 1)
                {
                    PathDirection previousDirection = directions[^2];
                    Vector3 turningPoint = waypointNode;
                    bool isTurningLeft = previousDirection == PathDirection.South;

                    // Ensure turning point is set to the correct point before more calculations because A* path is inconsistent
                    if (isTurningLeft && _ALL_EAST_POINTS.Any(eastPoint => eastPoint.IsEqual(turningPoint)))
                    {
                        turningPoint = this.GetPointInDirection(turningPoint, new[] { PathDirection.North });
                    }

                    if (isTurningLeft)
                        turningPoint = this.GetPointInDirection(turningPoint, new[] { PathDirection.West }, 15f);
                    else
                        turningPoint = this.GetPointInDirection(turningPoint, new[] { PathDirection.West, PathDirection.West, PathDirection.South });

                    directionsWithTurningPoint.Add(new(previousDirection, turningPoint));
                }
            }
            else if (southCounts == 2)
            {
                directions.Add(PathDirection.South);
                northCounts = 0;
                eastCounts = 0;
                westCounts = 0;

                if (directions.Count > 1)
                {
                    PathDirection previousDirection = directions[^2];
                    Vector3 turningPoint = waypointNode;
                    bool isTurningLeft = previousDirection == PathDirection.West;

                    // Ensure turning point is set to the correct point before more calculations because A* path is inconsistent
                    if (isTurningLeft && _ALL_SOUTH_POINTS.Any(southPoint => southPoint.IsEqual(turningPoint)))
                    {
                        turningPoint = this.GetPointInDirection(turningPoint, new[] { PathDirection.East });
                    }

                    if (isTurningLeft)
                        turningPoint = this.GetPointInDirection(turningPoint, new[] { PathDirection.North }, 15f);
                    else
                        turningPoint = this.GetPointInDirection(turningPoint, new[] { PathDirection.North, PathDirection.North, PathDirection.West });

                    directionsWithTurningPoint.Add(new(previousDirection, turningPoint));
                }
            }
            else if (westCounts == 2)
            {
                directions.Add(PathDirection.West);
                northCounts = 0;
                eastCounts = 0;
                southCounts = 0;

                if (directions.Count > 1)
                {
                    PathDirection previousDirection = directions[^2];
                    Vector3 turningPoint = waypointNode;
                    bool isTurningLeft = previousDirection == PathDirection.North;

                    // Ensure turning point is set to the correct point before more calculations because A* path is inconsistent
                    if (isTurningLeft && _ALL_WEST_POINTS.Any(westPoint => westPoint.IsEqual(turningPoint)))
                    {
                        turningPoint = this.GetPointInDirection(turningPoint, new[] { PathDirection.South });
                    }

                    if (isTurningLeft)
                        turningPoint = this.GetPointInDirection(turningPoint, new[] { PathDirection.East }, 15f);
                    else
                        turningPoint = this.GetPointInDirection(turningPoint, new[] { PathDirection.East, PathDirection.East, PathDirection.North });

                    directionsWithTurningPoint.Add(new(previousDirection, turningPoint));
                }
            }
        }

        // Add last direction
        directionsWithTurningPoint.Add(new(directions.Last(), Vector3.zero));
        return directionsWithTurningPoint.ToArray();
    }
}
