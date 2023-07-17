using UnityEngine;

public class PlayerAnimationController : NetworkBehaviorAutoDisable<PlayerAnimationController>
{
    private Animator _animator;
    private ClientNetworkAnimator _networkAnimator;
    private PlayerMovementController _movementController;
    private PlayerInteractorController _interactorController;

    private const string _IS_WALKING_PARAMETER = "isWalking";
    private const string _IS_SPRINTING_PARAMETER = "isSprinting";
    private const string _IS_MOVING_PARAMETER = "isMovingForward";
    private const string _DO_IDLE_PARAMETER = "doIdle";
    private const string _DO_JUMP_PARAMETER = "doJump";
    private const string _IS_DRIVING_PARAMETER = "isDriving";

    protected override void OnOwnerNetworkSpawn()
    {
        this._animator = GetComponent<Animator>();
        this._networkAnimator = GetComponent<ClientNetworkAnimator>();
        this._movementController = GetComponent<PlayerMovementController>();
        this._interactorController = GetComponent<PlayerInteractorController>();
        this._movementController.OnStateChange += this.OnMovementStateChange;
        this._movementController.OnMovementDirectionChange += this.OnMovementDirectionChange;
        this._interactorController.OnDidInteraction += this.OnPlayerDidInteraction;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (!this.IsOwner) { return; }

        this._movementController.OnStateChange -= this.OnMovementStateChange;
        this._movementController.OnMovementDirectionChange -= this.OnMovementDirectionChange;
        this._interactorController.OnDidInteraction -= this.OnPlayerDidInteraction;
    }

    private void OnMovementStateChange(PlayerMovementController.MovementState state)
    {
        switch (state)
        {
            case PlayerMovementController.MovementState.Idle:
                this._networkAnimator.SetTrigger(_DO_IDLE_PARAMETER);
                this._animator.SetBool(_IS_WALKING_PARAMETER, false);
                this._animator.SetBool(_IS_SPRINTING_PARAMETER, false);
                break;
            case PlayerMovementController.MovementState.Walking:
                this._animator.SetBool(_IS_WALKING_PARAMETER, true);
                this._animator.SetBool(_IS_SPRINTING_PARAMETER, false);
                break;
            case PlayerMovementController.MovementState.Sprinting:
                this._animator.SetBool(_IS_SPRINTING_PARAMETER, true);
                this._animator.SetBool(_IS_WALKING_PARAMETER, false);
                break;
            case PlayerMovementController.MovementState.Air:
                this._networkAnimator.SetTrigger(_DO_JUMP_PARAMETER);
                break;
            default:
                break;
        }
    }

    private void OnPlayerDidInteraction(InteractionType interaction)
    {
        switch (interaction)
        {
            case InteractionType.EnterVehicle:
                this._animator.SetBool(_IS_DRIVING_PARAMETER, true);
                this._animator.SetBool(_IS_WALKING_PARAMETER, false);
                this._animator.SetBool(_IS_SPRINTING_PARAMETER, false);
                break;
            case InteractionType.ExitVehicle:
                this._animator.SetBool(_IS_DRIVING_PARAMETER, false);
                this._animator.SetBool(_IS_WALKING_PARAMETER, false);
                this._animator.SetBool(_IS_SPRINTING_PARAMETER, false);
                this._networkAnimator.SetTrigger(_DO_IDLE_PARAMETER);
                break;
            default:
                break;
        }
    }

    private void OnMovementDirectionChange(bool isMovingForward) => this._animator.SetBool(_IS_MOVING_PARAMETER, isMovingForward);
}
