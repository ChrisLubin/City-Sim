using UnityEngine;

public class NpcMovementController : CharacterMovementController, IAiMovementController
{
    private NpcAiPathController _pathController;
    private Collider _collider;

    private bool _hasTarget { get => this._target != Vector3.zero; }
    private Vector3 _target = Vector3.zero;

    private const float _DETECTION_ZONE_DISTANCE = 2f;
    private const float _DETECTION_ZONE_ANGLE_DISTANCE = 1.5f;
    private bool _hasObstacleOnLeft = false;
    private bool _hasObstacleInFront = false;
    private bool _hasObstacleOnRight = false;

    public bool _hasRightOfWay = false;
    public bool _isAtIntersection = false;
    public bool _isAtRedLightOrStopSign = false;
    private Direction _upcomingDirection;
    public Direction UpcomingDirection { get => this._upcomingDirection; }

    void OnDrawGizmosSelected()
    {
        // Draw detection zone
        Vector3 midTorso = transform.position + 1f * Vector3.up;

        Gizmos.color = this._hasObstacleOnLeft ? new Color(255, 0, 0) : new Color(0, 255, 0);
        Gizmos.DrawLine(midTorso, midTorso + _DETECTION_ZONE_DISTANCE * transform.forward + _DETECTION_ZONE_ANGLE_DISTANCE * -transform.right);

        Gizmos.color = this._hasObstacleInFront ? new Color(255, 0, 0) : new Color(0, 255, 0);
        Gizmos.DrawLine(midTorso, midTorso + _DETECTION_ZONE_DISTANCE * transform.forward);

        Gizmos.color = this._hasObstacleOnRight ? new Color(255, 0, 0) : new Color(0, 255, 0);
        Gizmos.DrawLine(midTorso, midTorso + _DETECTION_ZONE_DISTANCE * transform.forward + _DETECTION_ZONE_ANGLE_DISTANCE * transform.right);
    }

    private void Awake()
    {
        base.OnAwake();

        this._collider = GetComponent<Collider>();
        this._pathController = GetComponent<NpcAiPathController>();
        this._pathController.OnNextNodeChange += this.OnTargetChange;
        this._pathController.OnUpcomingDirectionChange += this.OnUpcomingDirectionChange;

        this.IsNpc = true;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        this._pathController.OnNextNodeChange -= this.OnTargetChange;
        this._pathController.OnUpcomingDirectionChange -= this.OnUpcomingDirectionChange;
    }

    private void Start() => base.OnStart();

    private void Update()
    {
        if (!this.IsOwnedByServer || !this._hasTarget) { return; }

        base.OnUpdate();

        if (this._isAtIntersection && this._isAtRedLightOrStopSign || this._isAtIntersection && !this._hasRightOfWay)
            this.IsTryingToMoveForward = false;
        else
            this.IsTryingToMoveForward = true;

        this._rigidBody.useGravity = !this._isAtIntersection;
        this._collider.isTrigger = this._isAtIntersection;
        if (!this.IsTryingToMoveForward) { return; }

        Vector3 midTorso = transform.position + 1f * Vector3.up;
        this._hasObstacleOnLeft = Physics.Linecast(midTorso, midTorso + _DETECTION_ZONE_DISTANCE * transform.forward + _DETECTION_ZONE_ANGLE_DISTANCE * -transform.right, out RaycastHit hitLeft, LayerMask.GetMask(Constants.LayerNames.Character));
        this._hasObstacleInFront = Physics.Linecast(midTorso, midTorso + _DETECTION_ZONE_DISTANCE * transform.forward, out RaycastHit hitFront, LayerMask.GetMask(Constants.LayerNames.Character));
        this._hasObstacleOnRight = Physics.Linecast(midTorso, midTorso + _DETECTION_ZONE_DISTANCE * transform.forward + _DETECTION_ZONE_ANGLE_DISTANCE * transform.right, out RaycastHit hitRight, LayerMask.GetMask(Constants.LayerNames.Character));

        if (this._hasObstacleInFront)
            this.UpdateLookPoint(this._hasObstacleOnLeft ? LookDirection.Right : LookDirection.Left);
        else if (this._hasObstacleOnLeft)
            this.UpdateLookPoint(LookDirection.Right);
        else if (this._hasObstacleOnRight)
            this.UpdateLookPoint(LookDirection.Left);
        else
            this.UpdateLookPoint(LookDirection.Straight);

    }

    public void SetIsAtIntersection(bool isAtIntersection) => this._isAtIntersection = isAtIntersection;
    public void SetHasRightOfWay(bool hasRightOfWay) => this._hasRightOfWay = hasRightOfWay;
    public void SetIsAtRedLightOrStopSign(bool isAtRedLightOrStopSign) => this._isAtRedLightOrStopSign = isAtRedLightOrStopSign;
    private void OnUpcomingDirectionChange(Direction direction) => this._upcomingDirection = direction;
    private void OnTargetChange(Vector3 nextTarget) => this._target = nextTarget;

    private void UpdateLookPoint(LookDirection direction)
    {
        float rotateSpeed = 4f;
        Vector3 lookPoint = Vector3.zero;

        if (direction == LookDirection.Left)
        {
            lookPoint = Vector3.Lerp(transform.position + transform.forward * _DETECTION_ZONE_ANGLE_DISTANCE, transform.position + _DETECTION_ZONE_DISTANCE * transform.forward + _DETECTION_ZONE_ANGLE_DISTANCE * -transform.right, Time.deltaTime * rotateSpeed);
        }
        else if (direction == LookDirection.Straight)
        {
            lookPoint = Vector3.Lerp(transform.position + transform.forward * _DETECTION_ZONE_ANGLE_DISTANCE, this._target, Time.deltaTime * rotateSpeed);
        }
        else if (direction == LookDirection.Right)
        {
            lookPoint = Vector3.Lerp(transform.position + transform.forward * _DETECTION_ZONE_ANGLE_DISTANCE, transform.position + _DETECTION_ZONE_DISTANCE * transform.forward + _DETECTION_ZONE_ANGLE_DISTANCE * transform.right, Time.deltaTime * rotateSpeed);
        }

        transform.LookAt(new Vector3(lookPoint.x, transform.position.y, lookPoint.z));
    }

    private enum LookDirection
    {
        Left,
        Straight,
        Right
    }
}
