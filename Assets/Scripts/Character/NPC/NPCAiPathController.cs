using System;
using Cysharp.Threading.Tasks;
using Pathfinding;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(NpcMovementController))]
[RequireComponent(typeof(Seeker))]
public class NpcAiPathController : NetworkBehaviour
{
    private Seeker _seeker;

    private Path _path = null;
    private int _currentWaypoint = 0;
    [SerializeField] private float _waypointEnteredDistance = 4;

    [SerializeField] private Transform _startingDestination;
    private Vector3 _currentDestination = Vector3.zero;

    public event Action<Vector3> OnNextNodeChange;
    public event Action<Direction> OnUpcomingDirectionChange;

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

        this.UpdateUpcomingDirection();
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

    private void UpdateUpcomingDirection()
    {
        float xDirectionChange = 0f;
        float zDirectionChange = 0f;

        if (this._currentWaypoint >= this._path.vectorPath.Count() - 1)
        {
            OnUpcomingDirectionChange?.Invoke(Direction.None);
            return;
        }

        int nodesFound = 0;
        int nodeAmountToFind = 7;

        for (int i = this._currentWaypoint; i < this._path.vectorPath.Count() - 1; i++)
        {
            Vector3 currentNode = this._path.vectorPath[i];
            Vector3 nextNode = this._path.vectorPath[i + 1];
            float xChange = nextNode.x - currentNode.x;
            float zChange = nextNode.z - currentNode.z;

            xDirectionChange += xChange;
            zDirectionChange += zChange;
            nodesFound++;

            if (nodesFound == nodeAmountToFind) { break; }
        }

        if (Math.Abs(zDirectionChange) >= Math.Abs(xDirectionChange))
            OnUpcomingDirectionChange?.Invoke(zDirectionChange <= 0f ? Direction.North : Direction.South);
        else
            OnUpcomingDirectionChange?.Invoke(xDirectionChange <= 0f ? Direction.East : Direction.West);
    }
}
