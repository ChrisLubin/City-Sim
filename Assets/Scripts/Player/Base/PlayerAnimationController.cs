using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator _animator;
    private PlayerMovementController _movementController;

    private const string _IS_WALKING_PARAMETER = "isWalking";
    private const string _IS_SPRINTING_PARAMETER = "isSprinting";
    private const string _IS_MOVING_PARAMETER = "isMovingForward";
    private const string _DO_IDLE_PARAMETER = "doIdle";
    private const string _DO_JUMP_PARAMETER = "doJump";

    private void Awake()
    {
        this._animator = GetComponent<Animator>();
        this._movementController = GetComponent<PlayerMovementController>();
        this._movementController.OnStateChange += this.OnMovementStateChange;
        this._movementController.OnMovementDirectionChange += this.OnMovementDirectionChange;
    }

    private void OnDestroy()
    {
        this._movementController.OnStateChange -= this.OnMovementStateChange;
        this._movementController.OnMovementDirectionChange -= this.OnMovementDirectionChange;
    }

    private void OnMovementStateChange(PlayerMovementController.MovementState state)
    {
        switch (state)
        {
            case PlayerMovementController.MovementState.Idle:
                this._animator.SetTrigger(_DO_IDLE_PARAMETER);
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
                this._animator.SetTrigger(_DO_JUMP_PARAMETER);
                break;
            default:
                break;
        }
    }

    private void OnMovementDirectionChange(bool isMovingForward) => this._animator.SetBool(_IS_MOVING_PARAMETER, isMovingForward);
}
