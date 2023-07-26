using System;
using Cysharp.Threading.Tasks;
using Pathfinding;
using Unity.Netcode;
using UnityEngine;

public class VehicleAiPathController : NetworkBehaviour
{
    private VehicleSeatController _seatController;

    private Seeker _seeker;
    private Path _fullRoute = null;
    private int _currentWaypoint = 0;
    [SerializeField] private float _waypointEnteredDistance = 3;

    [SerializeField] private Transform _target;

    public event Action<Vector3> OnNextNodeChange;

    private const string _ALL_DIRECTIONS_GRAPH_NAME = "AllDirections";
    private const string _NORTH_DIRECTION_GRAPH_NAME = "North";
    private const string _EAST_DIRECTION_GRAPH_NAME = "East";
    private const string _SOUTH_DIRECTION_GRAPH_NAME = "South";
    private const string _WEST_DIRECTION_GRAPH_NAME = "West";

    private Vector3 _test;

    private void Awake()
    {
        this._seatController = GetComponent<VehicleSeatController>();
        this._seeker = GetComponent<Seeker>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        this._seatController.TestAiDriver();
    }

    private void Start()
    {
        this.GetWholeRoute();
    }

    private void Update()
    {
        if (!this.IsOwner || !this._seatController.HasAiInDriverSeat || this._target == null || this._fullRoute == null) { return; }

        float distanceToNextWaypoint;

        // Check in a loop if we are close enough to the current waypoint to switch to the next one
        while (true)
        {
            distanceToNextWaypoint = Vector3.Distance(transform.position, this._fullRoute.vectorPath[this._currentWaypoint]);

            if (distanceToNextWaypoint >= this._waypointEnteredDistance) { break; }
            if (this._currentWaypoint + 1 >= this._fullRoute.vectorPath.Count)
            {
                // We reached the last waypoint
                break;
            }

            // Entered waypoint
            this._currentWaypoint++;
            OnNextNodeChange?.Invoke(this._fullRoute.vectorPath[this._currentWaypoint]);
        }
    }

    private async void GetWholeRoute()
    {
        this._currentWaypoint = 0;
        this._fullRoute = null;

        Path route = this._seeker.StartPath(transform.position, this._target.position, (Path p) => { }, GraphMask.FromGraphName(_ALL_DIRECTIONS_GRAPH_NAME));
        await route.WaitForPath();
        this._fullRoute = route;
        this._seeker.drawGizmos = true;
        this._seeker.detailedGizmos = true;
        OnNextNodeChange?.Invoke(this._fullRoute.vectorPath[this._currentWaypoint]);
    }
}
