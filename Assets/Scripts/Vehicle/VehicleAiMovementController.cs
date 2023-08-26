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
    [SerializeField] private float _collisionCheckOffset = 0.5f;
    [SerializeField] private float _maxCollisionLengthDistanceCheck = 6f;
    [SerializeField] private float _maxCollisionWidthDistanceCheck = 4f;
    [SerializeField] private float _maxCollisionHeightDistanceCheck = 1f;
    [SerializeField] private float _turnThreshold = 1f;
    [SerializeField] private float _maxSpeed = 15f;

    private float _originalMaxCollisionLengthDistanceCheck;
    [SerializeField] private float _intersectionMaxCollisionLengthDistanceCheck = 1f;

    private Vector3 _target = Vector3.zero;
    private bool _hasTarget { get => this._target != Vector3.zero; }
    private bool _hasRightOfWay = false;
    private bool _isAtIntersection = false;
    private bool _isAtRedLightOrStopSign = false;

    private Direction _upcomingDirection;
    public Direction UpcomingDirection { get => this._upcomingDirection; }
    Vector3 LocalDirection => transform.TransformDirection(new(0f, 0f, this._maxCollisionLengthDistanceCheck));

    void OnDrawGizmosSelected()
    {
        // Draw detection zone
        Gizmos.color = new Color(255, 0, 0);
        Vector3 p1 = this._frontOfVehicle.position + this._frontOfVehicle.forward * this._collisionCheckOffset;
        Vector3 p2 = this._frontOfVehicle.position + this._frontOfVehicle.forward * this._collisionCheckOffset + LocalDirection;
        Vector3 extents = new(this._maxCollisionWidthDistanceCheck, this._maxCollisionHeightDistanceCheck, 1f);
        bool boxes = true;

        Vector3 halfExtents = extents / 2;
        Vector3 halfExtentsZ = transform.forward * halfExtents.z;
        Vector3 halfExtentsY = transform.up * halfExtents.y;
        Vector3 halfExtentsX = transform.right * halfExtents.x;

        if (boxes)
        {
            Matrix4x4 matrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(p1, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, extents);
            Gizmos.matrix = Matrix4x4.TRS(p2, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, extents);
            Gizmos.matrix = matrix;
        }

        // Draw lines
        Gizmos.DrawLine(p1 - halfExtentsX - halfExtentsY - halfExtentsZ, p2 - halfExtentsX - halfExtentsY - halfExtentsZ);
        Gizmos.DrawLine(p1 + halfExtentsX - halfExtentsY - halfExtentsZ, p2 + halfExtentsX - halfExtentsY - halfExtentsZ);
        Gizmos.DrawLine(p1 - halfExtentsX + halfExtentsY - halfExtentsZ, p2 - halfExtentsX + halfExtentsY - halfExtentsZ);
        Gizmos.DrawLine(p1 + halfExtentsX + halfExtentsY - halfExtentsZ, p2 + halfExtentsX + halfExtentsY - halfExtentsZ);

        // Draw lines
        Gizmos.DrawLine(p1 - halfExtentsX - halfExtentsY + halfExtentsZ, p2 - halfExtentsX - halfExtentsY + halfExtentsZ);
        Gizmos.DrawLine(p1 + halfExtentsX - halfExtentsY + halfExtentsZ, p2 + halfExtentsX - halfExtentsY + halfExtentsZ);
        Gizmos.DrawLine(p1 - halfExtentsX + halfExtentsY + halfExtentsZ, p2 - halfExtentsX + halfExtentsY + halfExtentsZ);
        Gizmos.DrawLine(p1 + halfExtentsX + halfExtentsY + halfExtentsZ, p2 + halfExtentsX + halfExtentsY + halfExtentsZ);
    }

    private void Awake()
    {
        this._movementController = GetComponent<VehicleMovementController>();
        this._seatController = GetComponent<VehicleSeatController>();
        this._pathController = GetComponent<VehicleAiPathController>();
        this._pathController.OnNextNodeChange += this.OnTargetChange;
        this._pathController.OnUpcomingDirectionChange += this.OnUpcomingDirectionChange;
        this._originalMaxCollisionLengthDistanceCheck = this._maxCollisionLengthDistanceCheck;
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

        this._maxCollisionLengthDistanceCheck = this._isAtIntersection && this._hasRightOfWay ? this._intersectionMaxCollisionLengthDistanceCheck : this._originalMaxCollisionLengthDistanceCheck;

        bool hasVehicleInFront = Physics.BoxCastAll(this._frontOfVehicle.position + this._frontOfVehicle.forward * this._collisionCheckOffset, new Vector3(this._maxCollisionWidthDistanceCheck, this._maxCollisionHeightDistanceCheck, 1f) / 2, LocalDirection, transform.rotation, LocalDirection.magnitude, LayerMask.GetMask(Constants.LayerNames.Vehicle)).Length > 0;
        bool hasCharacterInFront = Physics.BoxCastAll(this._frontOfVehicle.position + this._frontOfVehicle.forward * this._collisionCheckOffset, new Vector3(this._maxCollisionWidthDistanceCheck, this._maxCollisionHeightDistanceCheck, 1f) / 2, LocalDirection, transform.rotation, LocalDirection.magnitude, LayerMask.GetMask(Constants.LayerNames.Character)).Where(c => !c.collider.isTrigger).Count() > 0;

        if (hasVehicleInFront || hasCharacterInFront || this._isAtRedLightOrStopSign || (this._isAtIntersection && !this._hasRightOfWay))
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
