using UnityEngine;

public class NPCMovementController : CharacterMovementController
{
    private Vector3 _previousPosition;

    private void Awake()
    {
        base.OnAwake();
        this.IsNPC = true;
        this._previousPosition = transform.position;
    }

    private void Start() => base.OnStart();

    private void Update()
    {
        if (!this.IsOwnedByServer) { return; }

        this.IsTryingToMoveForward = true;
        this.IsTryingToJump = this._previousPosition.IsEqual(transform.position);

        base.OnUpdate();

        this._previousPosition = transform.position;
    }
}
