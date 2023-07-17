using System;
using UnityEngine;

public class PlayerMovementController : NetworkBehaviorAutoDisable<PlayerMovementController>
{
    private PlayerInteractorController _playerInteractorController;
    private BoxCollider _collider;

    [Header("Movement")]
    private float _moveSpeed;
    [SerializeField] private float _walkSpeed = 3f;
    [SerializeField] private float _sprintSpeed = 6f;
    [SerializeField] private float _groundDrag = 4f;

    [Header("Jumping")]
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _jumpCooldown = 0.25f;
    [SerializeField] private float _airMultiplier = 0.4f;
    private bool _canJump;

    [Header("Crouching")]
    [SerializeField] private float _crouchSpeed = 1.5f;
    [SerializeField] private float _crouchYScale = 0.5f;
    private float _startYScale;

    [Header("Keybinds")]
    [SerializeField] private KeyCode _jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode _sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode _crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    [SerializeField] private float _playerHeight;
    [SerializeField] private LayerMask _whatIsGround;
    private bool _isGrounded;

    [Header("Slope Handling")]
    [SerializeField] private float _maxSlopeAngle;
    private RaycastHit _slopeHit;
    private bool _exitingSlope;

    [SerializeField] private Transform _orientation;

    private float _horizontalInput;
    private float _verticalInput;

    private Vector3 _previousMoveDirection;
    Vector3 _moveDirection;
    private bool _isMoving { get => this._moveDirection != Vector3.zero; }

    Rigidbody _rigidBody;

    [SerializeField] private MovementState _state;
    private bool _isMovingForward = false;
    public event Action<MovementState> OnStateChange;
    public event Action<bool> OnMovementDirectionChange;

    private void Awake()
    {
        this._rigidBody = GetComponent<Rigidbody>();
        this._collider = GetComponent<BoxCollider>();
        this._playerInteractorController = GetComponent<PlayerInteractorController>();
        this._playerInteractorController.OnDidInteraction += this.OnPlayerDidInteraction;
    }

    private void Start()
    {
        this._rigidBody.freezeRotation = true;
        this._canJump = true;
        this._startYScale = transform.localScale.y;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        this._playerInteractorController.OnDidInteraction -= this.OnPlayerDidInteraction;
    }

    private void Update()
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
        this._horizontalInput = Input.GetAxisRaw("Horizontal");
        this._verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(this._jumpKey) && this._canJump && this._isGrounded)
        {
            DoJump();
            Invoke(nameof(ResetJump), this._jumpCooldown);
        }

        // Start crouch
        if (Input.GetKeyDown(this._crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, this._crouchYScale, transform.localScale.z);
            this._rigidBody.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        // Stop crouch
        if (Input.GetKeyUp(this._crouchKey))
        {
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
        if (Input.GetKey(this._crouchKey))
        {
            this.UpdateState(MovementState.Crouching);
            this._moveSpeed = this._crouchSpeed;
        }
        else if (Input.GetKey(this._sprintKey) && this._isGrounded)
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
        this._moveDirection = this._orientation.forward * this._verticalInput + this._orientation.right * this._horizontalInput;

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

        bool isMovingForward = Input.GetKey(KeyCode.W);

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

    private void OnPlayerDidInteraction(InteractionType interaction)
    {
        switch (interaction)
        {
            case InteractionType.EnterVehicle:
                this.enabled = false;
                this._rigidBody.useGravity = false;
                this._collider.enabled = false;
                break;
            case InteractionType.ExitVehicle:
                this.enabled = true;
                this._rigidBody.useGravity = true;
                this._collider.enabled = true;
                break;
            default:
                break;
        }
    }
}
