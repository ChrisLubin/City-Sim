using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class VehicleAiMovementController : NetworkBehaviour
{
    private VehicleMovementController _movementController;
    private VehicleSeatController _seatController;

    [SerializeField] private Transform _frontOfVehicle;
    [SerializeField] private Transform _target;
    [SerializeField] private float _maxCollisionLengthDistanceCheck = 7f;
    [SerializeField] private float _maxCollisionWidthDistanceCheck = 6f;
    [SerializeField] private float _maxCollisionHeightDistanceCheck = 1f;
    [SerializeField] private float _turnThreshold = 1f;

    [SerializeField] private float _maxSpeed = 3f;

    void OnDrawGizmosSelected()
    {
        // Draw detection zone
        Gizmos.color = new Color(255, 0, 0);

        // Draw 4 lines going forward
        Vector3 closeTopLeftCorner = this._frontOfVehicle.position + (this._frontOfVehicle.up * this._maxCollisionHeightDistanceCheck / 2f) + (-this._frontOfVehicle.right * this._maxCollisionWidthDistanceCheck / 2f);
        Vector3 farTopLeftCorner = closeTopLeftCorner + (this._frontOfVehicle.forward * this._maxCollisionLengthDistanceCheck);
        Gizmos.DrawLine(closeTopLeftCorner, farTopLeftCorner);
        Vector3 closeBottomLeftCorner = closeTopLeftCorner + (-this._frontOfVehicle.up * this._maxCollisionHeightDistanceCheck);
        Vector3 farBottomLeftCorner = closeBottomLeftCorner + (this._frontOfVehicle.forward * this._maxCollisionLengthDistanceCheck);
        Gizmos.DrawLine(closeBottomLeftCorner, farBottomLeftCorner);
        Vector3 closeTopRightCorner = closeTopLeftCorner + (this._frontOfVehicle.right * this._maxCollisionWidthDistanceCheck);
        Vector3 farTopRightCorner = closeTopRightCorner + (this._frontOfVehicle.forward * this._maxCollisionLengthDistanceCheck);
        Gizmos.DrawLine(closeTopRightCorner, farTopRightCorner);
        Vector3 closeBottomRightCorner = closeTopRightCorner + (-this._frontOfVehicle.up * this._maxCollisionHeightDistanceCheck);
        Vector3 farBottomRightCorner = closeBottomRightCorner + (this._frontOfVehicle.forward * this._maxCollisionLengthDistanceCheck);
        Gizmos.DrawLine(closeBottomRightCorner, farBottomRightCorner);

        // Connect corner points
        Gizmos.DrawLine(closeTopLeftCorner, closeBottomLeftCorner);
        Gizmos.DrawLine(farTopLeftCorner, farBottomLeftCorner);
        Gizmos.DrawLine(closeTopRightCorner, closeBottomRightCorner);
        Gizmos.DrawLine(farTopRightCorner, farBottomRightCorner);

        // Connect across sides
        Gizmos.DrawLine(closeTopLeftCorner, closeTopRightCorner);
        Gizmos.DrawLine(closeBottomLeftCorner, closeBottomRightCorner);
        Gizmos.DrawLine(farTopLeftCorner, farTopRightCorner);
        Gizmos.DrawLine(farBottomLeftCorner, farBottomRightCorner);
    }

    private void Awake()
    {
        this._movementController = GetComponent<VehicleMovementController>();
        this._seatController = GetComponent<VehicleSeatController>();
    }

    void Update()
    {
        if (!this.IsOwner || !this._seatController.HasAiInDriverSeat || this._target == null) { return; }

        // Stop moving if the target is in front of us
        RaycastHit[] objectsInFront = Physics.BoxCastAll(this._frontOfVehicle.position + this._frontOfVehicle.forward * this._maxCollisionLengthDistanceCheck / 2, new(this._maxCollisionLengthDistanceCheck / 2, this._maxCollisionHeightDistanceCheck, this._maxCollisionWidthDistanceCheck / 2), this._frontOfVehicle.forward, transform.rotation, 0.01f);
        if (objectsInFront.Any(obj => obj.collider.gameObject == this._target.gameObject))
        {
            this._movementController.DecelerateCar();
            return;
        }

        // Handle steering
        Vector3 directionsToTarget = this._frontOfVehicle.InverseTransformPoint(this._target.position);
        bool isToLeft = directionsToTarget.x < 0;
        bool isBehind = directionsToTarget.z < 0;
        if (Mathf.Abs(directionsToTarget.x) >= this._turnThreshold)
        {
            if (isToLeft)
                this._movementController.TurnLeft();
            else
                this._movementController.TurnRight();
        }
        else
        {
            this._movementController.ResetSteeringAngle();
        }

        // Press gas if below max speed
        if (this._movementController.carSpeed < this._maxSpeed)
        {
            this._movementController.GoForward();
        }
    }
}
