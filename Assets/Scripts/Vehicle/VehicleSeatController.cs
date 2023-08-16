using Unity.Netcode;
using UnityEngine;

public class VehicleSeatController : NetworkBehaviourWithLogger<VehicleSeatController>
{
    private VehicleInteractionController _interactionController;

    [SerializeField] private Transform _driverSeatPosition;

    private const ulong _EMPTY_DRIVER_SEAT_CLIENT_ID = ulong.MaxValue;
    private const ulong _AI_DRIVER_SEAT_CLIENT_ID = _EMPTY_DRIVER_SEAT_CLIENT_ID - 1;
    private const float _MAX_SEAT_DESYNC_THRESHOLD = 0.1f;

    private NetworkVariable<ulong> _playerInDriverSeatClientId = new(_EMPTY_DRIVER_SEAT_CLIENT_ID, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private bool _isDriverChildOfSeatObject { get => this._driverSeatPosition.transform.childCount != 0; } // Used to stop desync when host parents player to seat

    public bool HasPlayerInDriverSeat { get => this._playerInDriverSeatClientId.Value != _EMPTY_DRIVER_SEAT_CLIENT_ID && this._playerInDriverSeatClientId.Value != _AI_DRIVER_SEAT_CLIENT_ID; }
    public bool HasAiInDriverSeat { get => this._playerInDriverSeatClientId.Value == _AI_DRIVER_SEAT_CLIENT_ID; }

    public void TestAiDriver()
    {
        this._playerInDriverSeatClientId.Value = _AI_DRIVER_SEAT_CLIENT_ID;
    }

    protected override void Awake()
    {
        base.Awake();
        this._interactionController = GetComponent<VehicleInteractionController>();
        this._interactionController.OnInteraction += this.OnInteraction;
        this._playerInDriverSeatClientId.OnValueChanged += this.OnDriverSeatChange;
        MultiplayerSystem.OnPlayerDisconnect += this.OnPlayerDisconnect;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        this._interactionController.OnInteraction -= this.OnInteraction;
        this._playerInDriverSeatClientId.OnValueChanged -= this.OnDriverSeatChange;
        MultiplayerSystem.OnPlayerDisconnect -= this.OnPlayerDisconnect;

        if (this.IsHost)
        {
            Destroy(this._driverSeatPosition.gameObject);
        }
    }

    private void OnInteraction(InteractionType interaction)
    {
        if (!this.IsOwner) { return; }

        switch (interaction)
        {
            case InteractionType.EnterVehicle:
                PlayerManager.LocalPlayer.transform.position = this._driverSeatPosition.position;
                PlayerManager.LocalPlayer.transform.forward = this._driverSeatPosition.forward;
                this._playerInDriverSeatClientId.Value = MultiplayerSystem.LocalClientId;
                break;
            case InteractionType.ExitVehicle:
                this._playerInDriverSeatClientId.Value = _EMPTY_DRIVER_SEAT_CLIENT_ID;
                break;
            default:
                break;
        }
    }

    private void Update()
    {
        if (!this.IsOwner || !this.HasPlayerInDriverSeat || !this._isDriverChildOfSeatObject) { return; }

        // Necessary to sync player position when they enter moving car or immediately move after entering car
        if (Vector3.Distance(PlayerManager.LocalPlayer.transform.localPosition, Vector3.zero) > _MAX_SEAT_DESYNC_THRESHOLD)
        {
            PlayerManager.LocalPlayer.transform.localPosition = Vector3.zero;
            PlayerManager.LocalPlayer.transform.forward = this._driverSeatPosition.forward;
        }
    }

    private void OnDriverSeatChange(ulong previousDriverClientId, ulong currentDriverClientId)
    {
        if (!this.IsHost || this.HasAiInDriverSeat) { return; }

        bool isEnteringDriverSeat = currentDriverClientId != _EMPTY_DRIVER_SEAT_CLIENT_ID;
        ulong playerClientId = isEnteringDriverSeat ? currentDriverClientId : previousDriverClientId;

        bool gotPlayer = PlayerManager.Instance.TryGetPlayer(playerClientId, out PlayerController player);
        if (!gotPlayer)
        {
            this._logger.Log($"Could not find player with the client ID {playerClientId}", Logger.LogLevel.Error);
            return;
        }

        if (isEnteringDriverSeat)
        {
            player.NetworkObject.TrySetParent(this._driverSeatPosition);
        }
        else
        {
            player.NetworkObject.TryRemoveParent();
        }
    }

    private void OnPlayerDisconnect(PlayerData player)
    {
        if (!this.IsHost || this._playerInDriverSeatClientId.Value != player.ClientId) { return; }
        this._playerInDriverSeatClientId.Value = _EMPTY_DRIVER_SEAT_CLIENT_ID;
    }
}
