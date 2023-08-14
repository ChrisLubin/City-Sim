using UnityEngine;

public class NPCMovementController : CharacterMovementController
{
    private NPCAiPathController _pathController;

    private bool _hasTarget { get => this._target != Vector3.zero; }
    private Vector3 _target = Vector3.zero;
    private Vector3 _previousPosition;

    private void Awake()
    {
        base.OnAwake();

        this._pathController = GetComponent<NPCAiPathController>();
        this._pathController.OnNextNodeChange += this.OnTargetChange;

        this.IsNPC = true;
        this._previousPosition = transform.position;
    }

    private void Start() => base.OnStart();

    private void Update()
    {
        if (!this.IsOwnedByServer || !this._hasTarget) { return; }

        transform.LookAt(this._target);
        this.IsTryingToMoveForward = true;
        this.IsTryingToJump = this._previousPosition.IsEqual(transform.position);

        base.OnUpdate();

        this._previousPosition = transform.position;
    }

    private void OnTargetChange(Vector3 nextTarget) => this._target = nextTarget;
}
