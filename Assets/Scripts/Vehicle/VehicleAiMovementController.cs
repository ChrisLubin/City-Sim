using System.Linq;
using Pathfinding;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(VehicleAiPathController))]
[RequireComponent(typeof(Seeker))]
public class VehicleAiMovementController : NetworkBehaviour, IAiMovementController
{
    private VehicleMovementController _movementController;
    private VehicleSeatController _seatController;
    private VehicleAiPathController _pathController;

    [SerializeField] private Transform _frontOfVehicle;
    [SerializeField] private float _maxCollisionLengthDistanceCheck = 7f;
    [SerializeField] private float _maxCollisionWidthDistanceCheck = 6f;
    [SerializeField] private float _maxCollisionHeightDistanceCheck = 1f;
    [SerializeField] private float _turnThreshold = 1f;

    [SerializeField] private float _maxSpeed = 15f;

    private Vector3 _target = Vector3.zero;
    private bool _hasTarget { get => this._target != Vector3.zero; }
    private bool _hasRightOfWay = true;

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
        this._pathController = GetComponent<VehicleAiPathController>();
        this._pathController.OnNextNodeChange += this.OnTargetChange;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        this._pathController.OnNextNodeChange -= this.OnTargetChange;
    }

    private void OnTargetChange(Vector3 nextTarget) => this._target = nextTarget;
    public void SetHasRightOfWay(bool hasRightOfWay) => this._hasRightOfWay = hasRightOfWay;

    void Update()
    {
        if (!this.IsOwner || !this._seatController.HasAiInDriverSeat || !this._hasTarget) { return; }

        float distance = Vector3.Distance(new(transform.position.x, 0, transform.position.z), new(this._target.x, 0, this._target.z));

        // Stop moving if we are near the target, about to hit something, or don't have the right of way
        RaycastHit[] objectsInFront = Physics.BoxCastAll(this._frontOfVehicle.position + this._frontOfVehicle.forward * this._maxCollisionLengthDistanceCheck / 2, new(this._maxCollisionLengthDistanceCheck / 2, this._maxCollisionHeightDistanceCheck, this._maxCollisionWidthDistanceCheck / 2), this._frontOfVehicle.forward, transform.rotation, 0.01f);
        if (!this._hasRightOfWay || distance < 3 || objectsInFront.Any(obj => obj.collider.gameObject != gameObject))
        {
            this._movementController.DecelerateCar();
            return;
        }

        // Handle steering
        Vector3 directionsToTarget = this._frontOfVehicle.InverseTransformPoint(this._target);
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

        // Maintain max speed
        if (this._movementController.carSpeed < this._maxSpeed)
            this._movementController.GoForward();
        else
            this._movementController.DecelerateCar();
    }
}
