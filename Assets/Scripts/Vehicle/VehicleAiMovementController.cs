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
    private bool _hasRightOfWay = false;
    private bool _isAtIntersection = false;
    private bool _isAtRedLightOrStopSign = false;

    private Direction _upcomingDirection;
    public Direction UpcomingDirection { get => this._upcomingDirection; }

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
        this._pathController.OnUpcomingDirectionChange += this.OnUpcomingDirectionChange;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        this._pathController.OnNextNodeChange -= this.OnTargetChange;
        this._pathController.OnUpcomingDirectionChange -= this.OnUpcomingDirectionChange;
    }

    private void OnTargetChange(Vector3 nextTarget) => this._target = nextTarget;
    public void SetIsAtIntersection(bool isAtIntersection) => this._isAtIntersection = isAtIntersection;
    public void SetHasRightOfWay(bool hasRightOfWay) => this._hasRightOfWay = hasRightOfWay;
    public void SetIsAtRedLightOrStopSign(bool isAtRedLightOrStopSign) => this._isAtRedLightOrStopSign = isAtRedLightOrStopSign;
    private void OnUpcomingDirectionChange(Direction direction) => this._upcomingDirection = direction;

    void Update()
    {
        if (!this.IsOwner || !this._seatController.HasAiInDriverSeat || !this._hasTarget) { return; }

        bool hasVehicleInFront = Physics.BoxCastAll(this._frontOfVehicle.position + this._frontOfVehicle.forward * this._maxCollisionLengthDistanceCheck / 2, new(this._maxCollisionLengthDistanceCheck / 2, this._maxCollisionHeightDistanceCheck, this._maxCollisionWidthDistanceCheck / 2), this._frontOfVehicle.forward, transform.rotation, 0.01f, LayerMask.GetMask(Constants.LayerNames.Vehicle)).Any(obj => obj.collider.gameObject != gameObject);
        bool hasCharacterInFront = Physics.BoxCastAll(this._frontOfVehicle.position + this._frontOfVehicle.forward * this._maxCollisionLengthDistanceCheck / 2, new(this._maxCollisionLengthDistanceCheck / 2, this._maxCollisionHeightDistanceCheck, this._maxCollisionWidthDistanceCheck / 2), this._frontOfVehicle.forward, transform.rotation, 0.01f, LayerMask.GetMask(Constants.LayerNames.Character)).Where(c => !c.collider.isTrigger).Count() > 0;

        if (this._isAtRedLightOrStopSign || (this._isAtIntersection && !this._hasRightOfWay) || (hasVehicleInFront && !this._isAtIntersection) || (hasCharacterInFront && !this._isAtIntersection))
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
