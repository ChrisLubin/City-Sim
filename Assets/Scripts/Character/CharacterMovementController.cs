using System;
using UnityEngine;

public class CharacterMovementController : NetworkBehaviorAutoDisable<CharacterMovementController>
{
    protected Rigidbody _rigidBody;

    [Header("Inherited Class Control Options")]
    [SerializeField] private Transform _orientation;
    public bool IsNPC = false;
    public bool IsTryingToJump = false;
    public bool IsTryingToSprint = false;
    public bool IsTryingToStayCrouched = false;
    public bool IsTryingToStartCrouch = false;
    public bool IsTryingToEndCrouch = false;
    public bool IsTryingToMoveForward = false;
    public float HorizontalInput;
    public float VerticalInput;

    [Header("Movement")]
    [SerializeField] private float _walkSpeed = 3f;
    [SerializeField] private float _sprintSpeed = 6f;
    [SerializeField] private float _groundDrag = 4f;
    private float _moveSpeed;

    [Header("Jumping")]
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _jumpCooldown = 0.25f;
    [SerializeField] private float _airMultiplier = 0.4f;
    private bool _canJump;

    [Header("Crouching")]
    [SerializeField] private float _crouchSpeed = 1.5f;
    [SerializeField] private float _crouchYScale = 0.5f;
    private float _startYScale;

    [Header("Ground Check")]
    [SerializeField] private float _playerHeight = 2f;
    [SerializeField] private LayerMask _whatIsGround;
    private bool _isGrounded;

    [Header("Slope Handling")]
    [SerializeField] private float _maxSlopeAngle = 40f;
    private RaycastHit _slopeHit;
    private bool _exitingSlope;

    private Vector3 _previousMoveDirection;
    Vector3 _moveDirection;
    private bool _isMoving { get => this._moveDirection != Vector3.zero; }

    [SerializeField] private MovementState _state;
    private bool _isMovingForward = false;
    public event Action<MovementState> OnStateChange;
    public event Action<bool> OnMovementDirectionChange;

    public void OnAwake()
    {
        this._rigidBody = GetComponent<Rigidbody>();
        this._whatIsGround = LayerMask.NameToLayer("Everything");
    }

    public void OnStart()
    {
        this._rigidBody.freezeRotation = true;
        this._canJump = true;
        this._startYScale = transform.localScale.y;
    }

    public void OnUpdate()
    {
        if (!this.IsOwner) { return; }

        this._isGrounded = Physics.Raycast(transform.position, Vector3.down, 0.02f, this._whatIsGround);

        HandleInputUpdate();
        SpeedControlUpdate();
        StateHandlerUpdate();

        this._rigidBody.drag = this._isGrounded ? this._groundDrag : 0;
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void HandleInputUpdate()
    {
        if (this.IsTryingToJump && this._canJump && this._isGrounded)
        {
            DoJump();
            Invoke(nameof(ResetJump), this._jumpCooldown);
        }

        // Start crouch
        if (this.IsTryingToStartCrouch)
        {
            this.IsTryingToStartCrouch = false;
            transform.localScale = new Vector3(transform.localScale.x, this._crouchYScale, transform.localScale.z);
            this._rigidBody.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        // Stop crouch
        if (this.IsTryingToEndCrouch)
        {
            this.IsTryingToEndCrouch = false;
            transform.localScale = new Vector3(transform.localScale.x, this._startYScale, transform.localScale.z);
        }
    }

    private void UpdateState(MovementState newState)
    {
        if (this._state == newState) { return; }

        this._state = newState;
        this.OnStateChange?.Invoke(newState);
    }

    private void StateHandlerUpdate()
    {
        if (this.IsTryingToStayCrouched)
        {
            this.UpdateState(MovementState.Crouching);
            this._moveSpeed = this._crouchSpeed;
        }
        else if (this.IsTryingToSprint && this._isGrounded)
        {
            this.UpdateState(MovementState.Sprinting);
            this._moveSpeed = this._sprintSpeed;
        }
        else if (this._isGrounded && this._isMoving)
        {
            this.UpdateState(MovementState.Walking);
            this._moveSpeed = this._walkSpeed;
        }
        else if (this._isGrounded && !this._isMoving)
        {
            this.UpdateState(MovementState.Idle);
        }
        else
        {
            this.UpdateState(MovementState.Air);
        }
    }

    private void MovePlayer()
    {
        this._previousMoveDirection = this._moveDirection;
        this._moveDirection = !this.IsNPC ? this._orientation.forward * this.VerticalInput + this._orientation.right * this.HorizontalInput : this._orientation.forward;

        if (IsOnSlope() && !this._exitingSlope)
        {
            this._rigidBody.AddForce(GetSlopeMoveDirection() * this._moveSpeed * 20f, ForceMode.Force);

            if (this._rigidBody.velocity.y > 0)
                this._rigidBody.AddForce(Vector3.down * 80f, ForceMode.Force);
        }
        else if (this._isGrounded)
        {
            this._rigidBody.AddForce(this._moveDirection.normalized * this._moveSpeed * 10f, ForceMode.Force);
        }
        else if (!this._isGrounded)
        {
            this._rigidBody.AddForce(this._moveDirection.normalized * this._moveSpeed * 10f * this._airMultiplier, ForceMode.Force);
        }

        // Turn gravity off while on slope
        this._rigidBody.useGravity = !IsOnSlope();

        bool isMovingForward = this.IsTryingToMoveForward;

        if (isMovingForward != this._isMovingForward)
        {
            // Changed movement direction
            this._isMovingForward = isMovingForward;
            this.OnMovementDirectionChange?.Invoke(this._isMovingForward);
        }
    }

    private void SpeedControlUpdate()
    {
        if (IsOnSlope() && !this._exitingSlope)
        {
            // Limit speed on slope
            if (this._rigidBody.velocity.magnitude > this._moveSpeed)
                this._rigidBody.velocity = this._rigidBody.velocity.normalized * this._moveSpeed;
        }
        else
        {
            // Limit speed on ground or in air
            Vector3 flatVel = new(this._rigidBody.velocity.x, 0f, this._rigidBody.velocity.z);

            // Limit velocity if needed
            if (flatVel.magnitude > this._moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * this._moveSpeed;
                this._rigidBody.velocity = new Vector3(limitedVel.x, this._rigidBody.velocity.y, limitedVel.z);
            }
        }
    }

    private void DoJump()
    {
        if (!this._canJump) { return; }

        this._canJump = false;
        this._exitingSlope = true;

        // Reset y velocity
        this._rigidBody.velocity = new Vector3(this._rigidBody.velocity.x, 0f, this._rigidBody.velocity.z);
        this._rigidBody.AddForce(transform.up * this._jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        this._canJump = true;

        this._exitingSlope = false;
    }

    private bool IsOnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out this._slopeHit, this._playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, this._slopeHit.normal);
            return angle < this._maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(this._moveDirection, this._slopeHit.normal).normalized;
    }

    public enum MovementState
    {
        Idle,
        Walking,
        Sprinting,
        Crouching,
        Air
    }
}
