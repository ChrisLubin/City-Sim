using System;
using UnityEngine;

public class VehicleInteractionController : NetworkBehaviourWithLogger<VehicleInteractionController>, IInteractable
{
    private VehicleSeatController _seatController;

    public event Action<InteractionType> OnInteraction;

    protected override void Awake()
    {
        base.Awake();
        this._seatController = GetComponent<VehicleSeatController>();
    }

    private void Update()
    {
        if (!this.IsOwner || !this._seatController.HasPlayerInDriverSeat) { return; }

        if (Input.GetKeyDown(KeyCode.E))
        {
            this._logger.Log("Local player is exiting vehicle");
            this.OnInteraction?.Invoke(InteractionType.ExitVehicle);
            PlayerManager.LocalPlayer.GetComponent<PlayerInteractorController>().DidInteraction(InteractionType.ExitVehicle);
        }
    }

    public void DoInteract(PlayerInteractorController player)
    {
        if (!this.IsOwner || this._seatController.HasPlayerInDriverSeat) { return; }

        this._logger.Log("Local player is entering vehicle");
        this.OnInteraction?.Invoke(InteractionType.EnterVehicle);
        player.DidInteraction(InteractionType.EnterVehicle);
    }
}
