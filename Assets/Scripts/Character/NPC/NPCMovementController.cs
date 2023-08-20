using UnityEngine;

public class NPCMovementController : CharacterMovementController
{
    private NPCAiPathController _pathController;

    private bool _hasTarget { get => this._target != Vector3.zero; }
    private Vector3 _target = Vector3.zero;

    private const float _DETECTION_ZONE_DISTANCE = 2f;
    private const float _DETECTION_ZONE_ANGLE_DISTANCE = 1.2f;
    private bool _hasObstacleOnLeft = false;
    private bool _hasObstacleInFront = false;
    private bool _hasObstacleOnRight = false;

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

        this._pathController = GetComponent<NPCAiPathController>();
        this._pathController.OnNextNodeChange += this.OnTargetChange;

        this.IsNPC = true;
    }

    private void Start() => base.OnStart();

    private void Update()
    {
        if (!this.IsOwnedByServer || !this._hasTarget) { return; }

        this.IsTryingToMoveForward = true;

        Vector3 midTorso = transform.position + 1f * Vector3.up;
        this._hasObstacleOnLeft = Physics.Linecast(midTorso, midTorso + _DETECTION_ZONE_DISTANCE * transform.forward + _DETECTION_ZONE_ANGLE_DISTANCE * -transform.right, out RaycastHit hitLeft, LayerMask.GetMask(Constants.LayerNames.Player));
        this._hasObstacleInFront = Physics.Linecast(midTorso, midTorso + _DETECTION_ZONE_DISTANCE * transform.forward, out RaycastHit hitFront, LayerMask.GetMask(Constants.LayerNames.Player));
        this._hasObstacleOnRight = Physics.Linecast(midTorso, midTorso + _DETECTION_ZONE_DISTANCE * transform.forward + _DETECTION_ZONE_ANGLE_DISTANCE * transform.right, out RaycastHit hitRight, LayerMask.GetMask(Constants.LayerNames.Player));

        if (this._hasObstacleInFront)
            this.UpdateLookPoint(this._hasObstacleOnLeft ? Direction.Right : Direction.Left);
        else if (this._hasObstacleOnLeft)
            this.UpdateLookPoint(Direction.Right);
        else if (this._hasObstacleOnRight)
            this.UpdateLookPoint(Direction.Left);
        else
            this.UpdateLookPoint(Direction.Straight);

        base.OnUpdate();

    }

    private void UpdateLookPoint(Direction direction)
    {
        float rotateSpeed = 4f;
        Vector3 lookPoint = Vector3.zero;

        if (direction == Direction.Left)
        {
            lookPoint = Vector3.Lerp(transform.position + transform.forward * _DETECTION_ZONE_ANGLE_DISTANCE, transform.position + _DETECTION_ZONE_DISTANCE * transform.forward + _DETECTION_ZONE_ANGLE_DISTANCE * -transform.right, Time.deltaTime * rotateSpeed);
        }
        else if (direction == Direction.Straight)
        {
            lookPoint = Vector3.Lerp(transform.position + transform.forward * _DETECTION_ZONE_ANGLE_DISTANCE, this._target, Time.deltaTime * rotateSpeed);
        }
        else if (direction == Direction.Right)
        {
            lookPoint = Vector3.Lerp(transform.position + transform.forward * _DETECTION_ZONE_ANGLE_DISTANCE, transform.position + _DETECTION_ZONE_DISTANCE * transform.forward + _DETECTION_ZONE_ANGLE_DISTANCE * transform.right, Time.deltaTime * rotateSpeed);
        }

        transform.LookAt(lookPoint);
    }

    private void OnTargetChange(Vector3 nextTarget) => this._target = nextTarget;

    private enum Direction
    {
        Left,
        Straight,
        Right
    }
}
