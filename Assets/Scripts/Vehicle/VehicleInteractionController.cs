using System;
using UnityEngine;

public class VehicleInteractionController : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform _driverSeatPosition;
    private PlayerInteractorController _playerInDriverSeat;
    private PlayerInteractorController _localPlayer;

    public event Action<PlayerInteractorController> DriverSeatPlayerChanged;

    private void Awake()
    {
        PlayerManager.OnLocalPlayerSpawn += this.OnLocalPlayerSpawn;
    }

    private void OnDestroy()
    {
        PlayerManager.OnLocalPlayerSpawn -= this.OnLocalPlayerSpawn;
    }

    private void Update()
    {
        if (this._playerInDriverSeat == null || this._playerInDriverSeat != this._localPlayer) { return; }

        if (Input.GetKeyDown(KeyCode.E))
        {
            PlayerInteractorController player = this._playerInDriverSeat;
            this._playerInDriverSeat = null;
            player.transform.parent = null;
            this.DriverSeatPlayerChanged?.Invoke(null);
            player.DidInteraction(InteractionType.ExitVehicle);
        }
    }

    public void DoInteract(PlayerInteractorController player)
    {
        if (this._playerInDriverSeat != null) { return; }

        player.transform.position = this._driverSeatPosition.position;
        player.transform.forward = this._driverSeatPosition.forward;
        player.transform.parent = this._driverSeatPosition;
        this._playerInDriverSeat = player;
        this.DriverSeatPlayerChanged?.Invoke(player);
        player.DidInteraction(InteractionType.EnterVehicle);
    }

    private void OnLocalPlayerSpawn() => this._localPlayer = PlayerManager.LocalPlayer.GetComponent<PlayerInteractorController>();
}
