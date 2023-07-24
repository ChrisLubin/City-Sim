using System;
using Cysharp.Threading.Tasks;
using Pathfinding;
using Unity.Netcode;
using UnityEngine;

public class VehicleAiPathController : NetworkBehaviour
{
    private VehicleSeatController _seatController;

    private Seeker _seeker;
    private Path _currentPath = null;
    private int _currentWaypoint = 0;
    [SerializeField] private float _waypointEnteredDistance = 3;

    [SerializeField] private Transform _target;

    public event Action<Vector3> OnNextNodeChange;

    private readonly string[] _GRAPH_NAMES = new string[] { "Graph1", "Graph2" };

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
        this.RefreshPath();
    }

    private void Update()
    {
        if (!this.IsOwner || !this._seatController.HasAiInDriverSeat || this._target == null || this._currentPath == null) { return; }

        float distanceToNextWaypoint;

        // Check in a loop if we are close enough to the current waypoint to switch to the next one
        while (true)
        {
            distanceToNextWaypoint = Vector3.Distance(transform.position, this._currentPath.vectorPath[this._currentWaypoint]);

            if (distanceToNextWaypoint >= this._waypointEnteredDistance) { break; }
            if (this._currentWaypoint + 1 >= this._currentPath.vectorPath.Count)
            {
                // We reached the last waypoint
                break;
            }

            // Entered waypoint
            this._currentWaypoint++;
            OnNextNodeChange?.Invoke(this._currentPath.vectorPath[this._currentWaypoint]);
        }
    }

    private async void RefreshPath()
    {
        this._currentWaypoint = 0;
        this._currentPath = null;

        string correctGraph = null;
        float shortestStartingDistance = float.MaxValue;

        foreach (string graphName in _GRAPH_NAMES)
        {
            Path path = this._seeker.StartPath(transform.position, this._target.position, (Path p) => { }, GraphMask.FromGraphName(graphName));
            await path.WaitForPath();
            float startingDistance = Vector3.Distance(transform.position, path.vectorPath[0]);

            if (correctGraph == null || startingDistance < shortestStartingDistance)
            {
                correctGraph = graphName;
                shortestStartingDistance = startingDistance;
            }
        }

        Path correctPath = this._seeker.StartPath(transform.position, this._target.position, (Path p) => { }, GraphMask.FromGraphName(correctGraph));
        await correctPath.WaitForPath();
        this._currentPath = correctPath;
        this._seeker.drawGizmos = true;
        this._seeker.detailedGizmos = true;
        OnNextNodeChange?.Invoke(this._currentPath.vectorPath[this._currentWaypoint]);
    }
}
