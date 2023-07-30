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
    private Path _route = null;
    private int _currentWaypoint = 0;
    [SerializeField] private float _waypointEnteredDistance = 3;

    [SerializeField] private Transform _target;

    public event Action<Vector3> OnNextNodeChange;

    private const string _ALL_DIRECTIONS_GRAPH_NAME = "AllDirections";
    private const string _NORTH_DIRECTION_GRAPH_NAME = "North";
    private const string _EAST_DIRECTION_GRAPH_NAME = "East";
    private const string _SOUTH_DIRECTION_GRAPH_NAME = "South";
    private const string _WEST_DIRECTION_GRAPH_NAME = "West";

    private enum PathDirection
    {
        North,
        East,
        South,
        West,
        None
    }

    private IDictionary<PathDirection, string> _directionToGraphNameMap = new Dictionary<PathDirection, string>()
    {
        { PathDirection.None, _ALL_DIRECTIONS_GRAPH_NAME },
        { PathDirection.North, _NORTH_DIRECTION_GRAPH_NAME },
        { PathDirection.East, _EAST_DIRECTION_GRAPH_NAME },
        { PathDirection.South, _SOUTH_DIRECTION_GRAPH_NAME },
        { PathDirection.West, _WEST_DIRECTION_GRAPH_NAME },
    };

    private static IDictionary<PathDirection, Vector3[]> _DIRECTION_TO_DIRECTION_POINTS_MAP = new Dictionary<PathDirection, Vector3[]>();

    private Vector3 _test;

    private void Awake()
    {
        this._seatController = GetComponent<VehicleSeatController>();
        this._seeker = GetComponent<Seeker>();
        Debug.Log(transform.forward);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        this._seatController.TestAiDriver();
    }

    private static Vector3[] _ALL_DIRECTION_POINTS;
    private static Vector3[] _NORTH_DIRECTION_POINTS;
    private static Vector3[] _EAST_DIRECTION_POINTS;
    private static Vector3[] _SOUTH_DIRECTION_POINTS;
    private static Vector3[] _WEST_DIRECTION_POINTS;

    private void Start()
    {
        if (VehicleAiPathController._ALL_DIRECTION_POINTS == null)
        {
            Debug.Log("INIT");
            VehicleAiPathController._ALL_DIRECTION_POINTS = GameObject.FindGameObjectsWithTag(Constants.TagNames.AllDirections).Select(obj => obj.transform.position).ToArray();
            VehicleAiPathController._NORTH_DIRECTION_POINTS = GameObject.FindGameObjectsWithTag(Constants.TagNames.NorthDirection).Select(obj => obj.transform.position).ToArray();
            VehicleAiPathController._EAST_DIRECTION_POINTS = GameObject.FindGameObjectsWithTag(Constants.TagNames.EastDirection).Select(obj => obj.transform.position).ToArray();
            VehicleAiPathController._SOUTH_DIRECTION_POINTS = GameObject.FindGameObjectsWithTag(Constants.TagNames.SouthDirection).Select(obj => obj.transform.position).ToArray();
            VehicleAiPathController._WEST_DIRECTION_POINTS = GameObject.FindGameObjectsWithTag(Constants.TagNames.WestDirection).Select(obj => obj.transform.position).ToArray();
            VehicleAiPathController._DIRECTION_TO_DIRECTION_POINTS_MAP[PathDirection.North] = VehicleAiPathController._NORTH_DIRECTION_POINTS;
            VehicleAiPathController._DIRECTION_TO_DIRECTION_POINTS_MAP[PathDirection.East] = VehicleAiPathController._EAST_DIRECTION_POINTS;
            VehicleAiPathController._DIRECTION_TO_DIRECTION_POINTS_MAP[PathDirection.South] = VehicleAiPathController._SOUTH_DIRECTION_POINTS;
            VehicleAiPathController._DIRECTION_TO_DIRECTION_POINTS_MAP[PathDirection.West] = VehicleAiPathController._WEST_DIRECTION_POINTS;
        }
        // this.GetNextDirection();
        this.NextTry();
    }

    private async void NextTry()
    {
        this._directionSwitchCount = 0;
        this._currentWaypoint = 0;
        await this.GetWholeRoute();
        this._seeker.drawGizmos = true;
        this._seeker.detailedGizmos = true;
        List<DirectionWithTurningPoint> directionsWithTurningPoints = this.GetDirectionsWithTurningPointsForRoute();

        Debug.Log("DIRECTIONS WITH TURNING POINTS");
        foreach (var direction in directionsWithTurningPoints)
        {
            Debug.Log(direction.Direction);
            Debug.Log(direction.TurningPoint);
        }
        Debug.Log("-----------");

        ok = directionsWithTurningPoints.ToList();

        Path route = this._seeker.StartPath(transform.position, ok[this._currentWaypoint].TurningPoint, (Path p) => { }, GraphMask.FromGraphName(this._directionToGraphNameMap[ok[this._currentWaypoint].Direction]));
        await route.WaitForPath();
        this._route = route;
        this._seeker.drawGizmos = true;
        this._seeker.detailedGizmos = true;
        OnNextNodeChange?.Invoke(this._route.vectorPath[this._currentWaypoint]);
    }

    private void Update()
    {
        if (!this.IsOwner || !this._seatController.HasAiInDriverSeat || this._target == null || this._route == null) { return; }

        float distanceToNextWaypoint;

        // Check in a loop if we are close enough to the current waypoint to switch to the next one
        while (true)
        {
            distanceToNextWaypoint = Vector3.Distance(transform.position, this._route.vectorPath[this._currentWaypoint]);

            if (distanceToNextWaypoint >= this._waypointEnteredDistance) { break; }
            if (this._currentWaypoint + 1 >= this._route.vectorPath.Count)
            {
                // We reached the last waypoint
                // this.GetNextDirection();
                this.Doof();
                break;
            }

            // Entered waypoint
            this._currentWaypoint++;
            OnNextNodeChange?.Invoke(this._route.vectorPath[this._currentWaypoint]);
        }
    }

    List<DirectionWithTurningPoint> ok = new();

    private int _directionSwitchCount = 0;

    private async void Doof()
    {
        if (this._directionSwitchCount == ok.Count - 1)
        {
            this._route = null;
            Debug.Log("REACHED TARGET WAYPOINT");
            return;
        }

        this._currentWaypoint = 0;
        this._route = null;
        Path route;
        if (this._directionSwitchCount + 1 < ok.Count - 1)
        {
            Debug.Log(1);
            Vector3 startingPoint = ok[this._directionSwitchCount].TurningPoint;
            PathDirection previousDirection = ok[this._directionSwitchCount].Direction;
            PathDirection nextDirection = ok[this._directionSwitchCount + 1].Direction;

            if (previousDirection == PathDirection.North || nextDirection == PathDirection.North)
            {
                startingPoint.z -= 5f;
            }
            if (previousDirection == PathDirection.East || nextDirection == PathDirection.East)
            {
                startingPoint.x -= 5f;
            }
            if (previousDirection == PathDirection.South || nextDirection == PathDirection.South)
            {
                startingPoint.z += 5f;
            }
            if (previousDirection == PathDirection.West || nextDirection == PathDirection.West)
            {
                startingPoint.x += 5f;
            }

            route = this._seeker.StartPath(startingPoint, ok[this._directionSwitchCount + 1].TurningPoint, (Path p) => { }, GraphMask.FromGraphName(this._directionToGraphNameMap[ok[this._directionSwitchCount + 1].Direction]));
        }
        else
        {
            Debug.Log(2);
            Debug.Log("_directionSwitchCount");
            Debug.Log(_directionSwitchCount);
            Vector3 startingPoint = transform.position;
            PathDirection previousDirection = ok[^2].Direction;
            PathDirection nextDirection = ok[^1].Direction;
            Debug.Log("previousDirection");
            Debug.Log(previousDirection);
            Debug.Log("nextDirection");
            Debug.Log(nextDirection);

            if (previousDirection == PathDirection.North || nextDirection == PathDirection.North)
            {
                startingPoint.z -= 5f;
            }
            if (previousDirection == PathDirection.East || nextDirection == PathDirection.East)
            {
                startingPoint.x -= 5f;
            }
            if (previousDirection == PathDirection.South || nextDirection == PathDirection.South)
            {
                startingPoint.z += 5f;
            }
            if (previousDirection == PathDirection.West || nextDirection == PathDirection.West)
            {
                startingPoint.x += 5f;
            }

            route = this._seeker.StartPath(startingPoint, this._target.position, (Path p) => { }, GraphMask.FromGraphName(this._directionToGraphNameMap[nextDirection]));
        }
        await route.WaitForPath();
        this._route = route;
        this._seeker.drawGizmos = true;
        this._seeker.detailedGizmos = true;
        OnNextNodeChange?.Invoke(this._route.vectorPath[this._currentWaypoint]);
        this._directionSwitchCount++;
    }

    private async UniTask GetWholeRoute()
    {
        this._currentWaypoint = 0;
        this._route = null;

        Path route = this._seeker.StartPath(transform.position, this._target.position, (Path p) => { }, GraphMask.FromGraphName(_ALL_DIRECTIONS_GRAPH_NAME));
        await route.WaitForPath();
        this._route = route;
        OnNextNodeChange?.Invoke(this._route.vectorPath[this._currentWaypoint]);
    }

    private struct DirectionWithTurningPoint
    {
        public PathDirection Direction;
        public Vector3 TurningPoint;

        public DirectionWithTurningPoint(PathDirection direction, Vector3 turningPoint)
        {
            this.Direction = direction;
            this.TurningPoint = turningPoint;
        }
    }

    private List<DirectionWithTurningPoint> GetDirectionsWithTurningPointsForRoute()
    {
        PathDirection currentDirection = PathDirection.None;
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

        for (int i = 1; i < this._route.vectorPath.Count; i++)
        {
            Vector3 previousWaypointNode = this._route.vectorPath[i - 1];
            Vector3 waypointNode = this._route.vectorPath[i];

            if (previousWaypointNode.z > waypointNode.z)
            {
                if (iterationNorthWasIncremented == i - 1)
                {
                    northCounts++;
                }
                else
                {
                    northCounts = 1;
                }

                iterationNorthWasIncremented = i;
            }
            if (previousWaypointNode.x > waypointNode.x)
            {
                if (iterationEastWasIncremented == i - 1)
                {
                    eastCounts++;
                }
                else
                {
                    eastCounts = 1;
                }
                iterationEastWasIncremented = i;
            }
            if (previousWaypointNode.z < waypointNode.z)
            {
                if (iterationSouthWasIncremented == i - 1)
                {
                    southCounts++;
                }
                else
                {
                    southCounts = 1;
                }
                iterationSouthWasIncremented = i;
            }
            if (previousWaypointNode.x < waypointNode.x)
            {
                if (iterationWestWasIncremented == i - 1)
                {
                    westCounts++;
                }
                else
                {
                    westCounts = 1;
                }

                iterationWestWasIncremented = i;
            }

            if (northCounts == 2)
            {
                currentDirection = PathDirection.North;
                eastCounts = 0;
                southCounts = 0;
                westCounts = 0;
                directions.Add(currentDirection);

                if (directions.Count > 1)
                {
                    PathDirection previousDirection = directions[directions.Count - 2];
                    Vector3 turningPoint = waypointNode;
                    bool isTurningLeft = currentDirection == PathDirection.North && previousDirection == PathDirection.East || currentDirection == PathDirection.East && previousDirection == PathDirection.South || currentDirection == PathDirection.South && previousDirection == PathDirection.West || currentDirection == PathDirection.West && previousDirection == PathDirection.North;
                    // turningPoint.z += 10f;
                    if (isTurningLeft)
                    {
                        turningPoint.z += 15f;
                    }
                    else
                    {
                        turningPoint.z += 10f;
                        turningPoint.x -= 5f;
                    }

                    directionsWithTurningPoint.Add(new(previousDirection, turningPoint));
                }
            }
            else if (eastCounts == 2)
            {
                currentDirection = PathDirection.East;
                northCounts = 0;
                southCounts = 0;
                westCounts = 0;
                directions.Add(currentDirection);

                if (directions.Count > 1)
                {
                    PathDirection previousDirection = directions[directions.Count - 2];
                    Vector3 turningPoint = waypointNode;
                    bool isTurningLeft = currentDirection == PathDirection.North && previousDirection == PathDirection.East || currentDirection == PathDirection.East && previousDirection == PathDirection.South || currentDirection == PathDirection.South && previousDirection == PathDirection.West || currentDirection == PathDirection.West && previousDirection == PathDirection.North;
                    // turningPoint.x += 10f;
                    if (isTurningLeft)
                    {
                        turningPoint.x += 15f;
                    }
                    else
                    {
                        turningPoint.x += 10f;
                        turningPoint.z += 5f;
                    }

                    directionsWithTurningPoint.Add(new(previousDirection, turningPoint));
                }
            }
            else if (southCounts == 2)
            {
                currentDirection = PathDirection.South;
                northCounts = 0;
                eastCounts = 0;
                westCounts = 0;
                directions.Add(currentDirection);

                if (directions.Count > 1)
                {
                    PathDirection previousDirection = directions[directions.Count - 2];
                    Vector3 turningPoint = waypointNode;
                    bool isTurningLeft = currentDirection == PathDirection.North && previousDirection == PathDirection.East || currentDirection == PathDirection.East && previousDirection == PathDirection.South || currentDirection == PathDirection.South && previousDirection == PathDirection.West || currentDirection == PathDirection.West && previousDirection == PathDirection.North;
                    // turningPoint.z -= 10f;
                    if (isTurningLeft)
                    {
                        turningPoint.z -= 15f;
                    }
                    else
                    {
                        turningPoint.z -= 10f;
                        turningPoint.x += 5f;
                    }

                    directionsWithTurningPoint.Add(new(previousDirection, turningPoint));
                }
            }
            else if (westCounts == 2)
            {
                currentDirection = PathDirection.West;
                northCounts = 0;
                eastCounts = 0;
                southCounts = 0;
                directions.Add(currentDirection);

                if (directions.Count > 1)
                {
                    PathDirection previousDirection = directions[directions.Count - 2];
                    Vector3 turningPoint = waypointNode;
                    bool isTurningLeft = currentDirection == PathDirection.North && previousDirection == PathDirection.East || currentDirection == PathDirection.East && previousDirection == PathDirection.South || currentDirection == PathDirection.South && previousDirection == PathDirection.West || currentDirection == PathDirection.West && previousDirection == PathDirection.North;
                    // turningPoint.x -= 10f;
                    if (isTurningLeft)
                    {
                        turningPoint.x -= 15f;
                    }
                    else
                    {
                        turningPoint.x -= 10f;
                        turningPoint.z -= 5f;
                    }

                    directionsWithTurningPoint.Add(new(previousDirection, turningPoint));
                }
            }
        }

        // Add last direction
        directionsWithTurningPoint.Add(new(directions.Last(), Vector3.zero));

        return directionsWithTurningPoint;
    }
}
