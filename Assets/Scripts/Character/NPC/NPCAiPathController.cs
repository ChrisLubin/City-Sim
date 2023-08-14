using System;
using Cysharp.Threading.Tasks;
using Pathfinding;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NPCMovementController))]
[RequireComponent(typeof(Seeker))]
public class NPCAiPathController : NetworkBehaviour
{
    private Seeker _seeker;

    private Path _path = null;
    private int _currentWaypoint = 0;
    [SerializeField] private float _waypointEnteredDistance = 3;
    [SerializeField] private Transform _target;

    public event Action<Vector3> OnNextNodeChange;

    private const string _PEDESTRIAN_GRAPH_NAME = "Pedestrian Graph";

    private void Awake() => this._seeker = GetComponent<Seeker>();

    private async UniTask Start()
    {
        if (this._target == null)
        {
            this.enabled = false;
            return;
        }

        this._currentWaypoint = 0;
        this._path = null;
        this._seeker.drawGizmos = false;
        this._seeker.detailedGizmos = false;
        Path path = this._seeker.StartPath(transform.position, this._target.position, (Path p) => { }, GraphMask.FromGraphName(_PEDESTRIAN_GRAPH_NAME));
        await path.WaitForPath();
        this._seeker.drawGizmos = true;
        this._seeker.detailedGizmos = true;
        this._path = path;
    }

    private void Update()
    {
        if (!this.IsOwner || this._target == null || this._path == null) { return; }

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

    private void OnLastWaypointReached()
    {
        this._currentWaypoint = 0;
        this._path = null;
        OnNextNodeChange?.Invoke(Vector3.zero);
        Destroy(gameObject);
    }
}
