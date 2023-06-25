using UnityEngine;

public class VehicleMovementController : MonoBehaviour
{
    private VehicleInteractionController _interactionController;
    private PlayerInteractorController _playerInDriverSeat;
    private PlayerInteractorController _localPlayer;

    [SerializeField] private float _forwardSpeed = 10f;
    [SerializeField] private float _rotateSpeed = 60f;

    private void Awake()
    {
        PlayerManager.OnLocalPlayerSpawn += this.OnLocalPlayerSpawn;
        this._interactionController = GetComponent<VehicleInteractionController>();
        this._interactionController.DriverSeatPlayerChanged += this.OnDriverSeatPlayerChanged;
    }

    private void OnDestroy()
    {
        PlayerManager.OnLocalPlayerSpawn -= this.OnLocalPlayerSpawn;
        this._interactionController.DriverSeatPlayerChanged -= this.OnDriverSeatPlayerChanged;
    }

    private void Update()
    {
        if (this._playerInDriverSeat == null || this._playerInDriverSeat != this._localPlayer) { return; }

        if (Input.GetKey(KeyCode.W))
        {
            transform.position += transform.forward * this._forwardSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            transform.position += -transform.forward * this._forwardSpeed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.A))
        {
            transform.Rotate(-Vector3.up * this._rotateSpeed * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            transform.Rotate(Vector3.up * this._rotateSpeed * Time.deltaTime);
        }
    }

    private void OnDriverSeatPlayerChanged(PlayerInteractorController player) => this._playerInDriverSeat = player;
    private void OnLocalPlayerSpawn() => this._localPlayer = PlayerManager.LocalPlayer.GetComponent<PlayerInteractorController>();
}
