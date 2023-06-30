using System.Linq;
using UnityEngine;

public class VehicleProximityOwnershipController : NetworkBehaviorAutoDisable<VehicleProximityOwnershipController>
{
    private VehicleSeatController _seatController;

    [SerializeField] private float _maxPlayerDistanceCheck = 8f;

    void OnDrawGizmosSelected()
    {
        // Draw detection zone
        Gizmos.color = new Color(0, 0, 0);
        Gizmos.DrawWireCube(transform.position + Vector3.up, new(this._maxPlayerDistanceCheck, 1f, this._maxPlayerDistanceCheck));
    }

    private void Awake()
    {
        this._seatController = GetComponent<VehicleSeatController>();
    }

    private void Update()
    {
        if (!this.IsHost || this._seatController.HasPlayerInDriverSeat) { return; }

        RaycastHit[] playersThatAreNear = Physics.BoxCastAll(transform.position + Vector3.up, new(this._maxPlayerDistanceCheck, 0.01f, this._maxPlayerDistanceCheck), Vector3.forward, Quaternion.identity, 0.01f, LayerMask.GetMask(Constants.LayerNames.Player));
        if (playersThatAreNear.Length == 0) { return; }

        IOrderedEnumerable<RaycastHit> sortedPlayersByDistance = playersThatAreNear.OrderBy(player => Vector3.Distance(transform.position, player.collider.transform.position));
        Collider closestPlayerCollider = sortedPlayersByDistance.FirstOrDefault().collider;

        bool didFindPlayerComponent = closestPlayerCollider.TryGetComponent(out PlayerController player);
        if (!didFindPlayerComponent) { return; }

        ulong closestPlayerClientId = player.OwnerClientId;
        if (this.OwnerClientId == closestPlayerClientId) { return; }

        this.NetworkObject.ChangeOwnership(closestPlayerClientId);
    }
}
