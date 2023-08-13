using UnityEngine;

public class PlayerMovementController : CharacterMovementController
{
    private CharacterInteractorController _interactorController;

    private BoxCollider _collider;

    [Header("Keybinds")]
    [SerializeField] private KeyCode _jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode _sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode _crouchKey = KeyCode.LeftControl;

    private void Awake()
    {
        base.OnAwake();
        this.IsNPC = false;
        this._collider = GetComponent<BoxCollider>();
        this._interactorController = GetComponent<CharacterInteractorController>();
        this._interactorController.OnDidInteraction += this.OnPlayerDidInteraction;
    }

    private void Start() => base.OnStart();

    public override void OnDestroy()
    {
        base.OnDestroy();
        this._interactorController.OnDidInteraction -= this.OnPlayerDidInteraction;
    }

    private void Update()
    {
        if (!this.IsOwner) { return; }

        base.OnUpdate();
        this.IsTryingToJump = Input.GetKey(this._jumpKey);
        this.IsTryingToStayCrouched = Input.GetKeyDown(this._crouchKey);
        this.IsTryingToSprint = Input.GetKey(this._sprintKey);
        this.IsTryingToMoveForward = Input.GetKey(KeyCode.W);
        this.HorizontalInput = Input.GetAxisRaw("Horizontal");
        this.VerticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(this._crouchKey))
            this.IsTryingToStartCrouch = true;
        else if (Input.GetKeyUp(this._crouchKey))
            this.IsTryingToEndCrouch = true;
    }

    private void OnPlayerDidInteraction(InteractionType interaction)
    {
        switch (interaction)
        {
            case InteractionType.EnterVehicle:
                this.enabled = false;
                this._rigidBody.useGravity = false;
                this._rigidBody.isKinematic = true;
                this._rigidBody.interpolation = RigidbodyInterpolation.None;
                this._collider.enabled = false;
                break;
            case InteractionType.ExitVehicle:
                this.enabled = true;
                this._rigidBody.useGravity = true;
                this._rigidBody.isKinematic = false;
                this._rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
                this._collider.enabled = true;
                break;
            default:
                break;
        }
    }
}
