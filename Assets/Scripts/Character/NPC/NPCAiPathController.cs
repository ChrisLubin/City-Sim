using System;
using Cysharp.Threading.Tasks;
using Pathfinding;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(NPCMovementController))]
[RequireComponent(typeof(Seeker))]
public class NPCAiPathController : NetworkBehaviour
{
    private Seeker _seeker;

    private Path _path = null;
    private int _currentWaypoint = 0;
    [SerializeField] private float _waypointEnteredDistance = 4;

    [SerializeField] private Transform _startingDestination;
    private Vector3 _currentDestination = Vector3.zero;

    public event Action<Vector3> OnNextNodeChange;

    private const string _PEDESTRIAN_GRAPH_NAME = "Pedestrian Graph";
    private static Vector3[] _ALL_PEDESTRIAN_POINTS;

    private void Awake()
    {
        this._seeker = GetComponent<Seeker>();
        if (_ALL_PEDESTRIAN_POINTS == null)
            _ALL_PEDESTRIAN_POINTS = GameObject.FindGameObjectsWithTag(Constants.TagNames.PedestrianPoints).Select(obj => obj.transform.position).ToArray();
    }

    private async UniTask Start()
    {
        if (this._startingDestination == null)
        {
            int randomTargetIndex = UnityEngine.Random.Range(0, _ALL_PEDESTRIAN_POINTS.Length);
            this._currentDestination = _ALL_PEDESTRIAN_POINTS[randomTargetIndex];
        }
        else
        {
            this._currentDestination = this._startingDestination.position;
        }

        this._currentWaypoint = 0;
        this._path = null;
        this._seeker.drawGizmos = false;
        this._seeker.detailedGizmos = false;
        Path path = this._seeker.StartPath(transform.position, this._currentDestination, (Path p) => { }, GraphMask.FromGraphName(_PEDESTRIAN_GRAPH_NAME));
        await path.WaitForPath();
        this._seeker.drawGizmos = true;
        this._seeker.detailedGizmos = true;
        this._path = path;
    }

    private void Update()
    {
        if (!this.IsOwner || this._path == null) { return; }

        float distanceToNextWaypoint;

        // Check in a loop if we are close enough to the current waypoint to switch to the next one
        while (true)
        {
            distanceToNextWaypoint = Vector3.Distance(transform.position, this._path.vectorPath[this._currentWaypoint]);

            if (distanceToNextWaypoint >= this._waypointEnteredDistance) { break; }
            if (this._currentWaypoint + 1 >= this._path.vectorPath.Count)
            {
                // We reached the last waypoint
                this.OnLastWaypointReached();
                break;
            }

            // Entered waypoint
            this._currentWaypoint++;
            OnNextNodeChange?.Invoke(this._path.vectorPath[this._currentWaypoint]);
        }
    }

    private async void OnLastWaypointReached()
    {
        this._currentWaypoint = 0;
        this._path = null;
        int randomTargetIndex = UnityEngine.Random.Range(0, _ALL_PEDESTRIAN_POINTS.Length);
        this._currentDestination = _ALL_PEDESTRIAN_POINTS[randomTargetIndex];

        Path path = this._seeker.StartPath(transform.position, this._currentDestination, (Path p) => { }, GraphMask.FromGraphName(_PEDESTRIAN_GRAPH_NAME));
        await path.WaitForPath();
        this._path = path;
    }
}
