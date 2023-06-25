using UnityEngine;

public class PlayerMovementController : NetworkBehaviorAutoDisable<PlayerMovementController>
{
    private PlayerInteractorController _playerInteractorController;

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

    Vector3 _moveDirection;

    Rigidbody _rigidBody;

    [SerializeField] private MovementState _state;

    private void Awake()
    {
        this._playerInteractorController = GetComponent<PlayerInteractorController>();
        this._playerInteractorController.OnPlayerEnterVehicle += this.OnPlayerEnterVehicle;
        this._playerInteractorController.OnPlayerExitVehicle += this.OnPlayerExitVehicle;
    }

    private void Start()
    {
        this._rigidBody = GetComponent<Rigidbody>();
        this._rigidBody.freezeRotation = true;
        this._canJump = true;
        this._startYScale = transform.localScale.y;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        this._playerInteractorController.OnPlayerEnterVehicle -= this.OnPlayerEnterVehicle;
        this._playerInteractorController.OnPlayerExitVehicle -= this.OnPlayerExitVehicle;
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

    private void StateHandlerUpdate()
    {
        if (Input.GetKey(this._crouchKey))
        {
            this._state = MovementState.Crouching;
            this._moveSpeed = this._crouchSpeed;
        }
        else if (Input.GetKey(this._sprintKey) && this._isGrounded)
        {
            this._state = MovementState.Sprinting;
            this._moveSpeed = this._sprintSpeed;
        }
        else if (this._isGrounded)
        {
            this._state = MovementState.Walking;
            this._moveSpeed = this._walkSpeed;
        }
        else
        {
            this._state = MovementState.Air;
        }
    }

    private void MovePlayer()
    {
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

    private enum MovementState
    {
        Walking,
        Sprinting,
        Crouching,
        Air
    }

    private void OnPlayerEnterVehicle() => this.enabled = false;
    private void OnPlayerExitVehicle() => this.enabled = true;
}
