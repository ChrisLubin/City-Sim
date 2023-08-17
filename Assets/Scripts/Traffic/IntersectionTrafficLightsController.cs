using System;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class IntersectionTrafficLightsController : NetworkBehaviour
{
    [SerializeField] private TrafficLightController[] _northSouthTrafficLights;
    [SerializeField] private TrafficLightController[] _eastWestTrafficLights;

    private TrafficDirection _currentTrafficDirection;
    private bool _isChangingTrafficDirection = false;
    private float _timeSinceLastChange = 0f;
    private float _timeoutToChangeDirection;
    private NetworkVariable<IntersectionState> _state = new();

    private const float _MIN_CHANGE_DIRECTION_TIMEOUT = 10f;
    private const float _MAX_CHANGE_DIRECTION_TIMEOUT = 17f;
    private const float _TIMEOUT_TO_STOP_PEDESTRIAN_TRAFFIC = 3f;

    public event Action<TrafficDirection, bool> OnTrafficLightColorChange;

    private void Awake()
    {
        this._state.OnValueChanged += this.OnStateChange;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        this._state.OnValueChanged -= this.OnStateChange;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!this.IsHost)
        {
            this.ChangeLights(TrafficDirection.NorthSouth, this._state.Value.NorthSouthLightColor);
            this.ChangeLights(TrafficDirection.EastWest, this._state.Value.EastWestLightColor);

            if (this._state.Value.IsNorthSouthPedestrianLightFlashing)
                this.StartFlashingPedestrianStopLights(TrafficDirection.NorthSouth);
            if (this._state.Value.IsEastWestPedestrianLightFlashing)
                this.StartFlashingPedestrianStopLights(TrafficDirection.EastWest);
            return;
        }

        this._currentTrafficDirection = UnityEngine.Random.value < 0.5f ? TrafficDirection.NorthSouth : TrafficDirection.EastWest;
        this.RandomizeTrafficChangeTimeout();

        TrafficDirection redTrafficDirection = this._currentTrafficDirection == TrafficDirection.NorthSouth ? TrafficDirection.EastWest : TrafficDirection.NorthSouth;
        this.ChangeLights(this._currentTrafficDirection, LightColor.Green);
        this.ChangeLights(redTrafficDirection, LightColor.Red);
    }

    private void Update()
    {
        if (this._isChangingTrafficDirection || !this.IsHost) { return; }

        this._timeSinceLastChange += Time.deltaTime;

        // Turn on pedestrian stop light before light turns yellow
        if (this._timeoutToChangeDirection - this._timeSinceLastChange <= _TIMEOUT_TO_STOP_PEDESTRIAN_TRAFFIC)
        {
            this.StartFlashingPedestrianStopLights(this._currentTrafficDirection);
        }
        if (this._timeSinceLastChange >= this._timeoutToChangeDirection)
        {
            this.TransitionToOtherDirection();
        }
    }

    private void OnStateChange(IntersectionState oldState, IntersectionState newState)
    {
        if (this.IsHost) { return; }

        bool didNorthSouthPedLightFlashChange = oldState.IsNorthSouthPedestrianLightFlashing != newState.IsNorthSouthPedestrianLightFlashing;
        bool didEastWestPedLightFlashChange = oldState.IsEastWestPedestrianLightFlashing != newState.IsEastWestPedestrianLightFlashing;

        this.ChangeLights(TrafficDirection.NorthSouth, newState.NorthSouthLightColor);
        this.ChangeLights(TrafficDirection.EastWest, newState.EastWestLightColor);

        if (didNorthSouthPedLightFlashChange)
        {
            if (newState.IsNorthSouthPedestrianLightFlashing)
                this.StartFlashingPedestrianStopLights(TrafficDirection.NorthSouth);
            else
                this.StopFlashingPedestrianStopLights(TrafficDirection.NorthSouth);
        }
        if (didEastWestPedLightFlashChange)
        {
            if (newState.IsEastWestPedestrianLightFlashing)
                this.StartFlashingPedestrianStopLights(TrafficDirection.EastWest);
            else
                this.StopFlashingPedestrianStopLights(TrafficDirection.EastWest);
        }
    }

    private async void TransitionToOtherDirection()
    {
        this._isChangingTrafficDirection = true;
        TrafficDirection oppositeTrafficDirection = this._currentTrafficDirection == TrafficDirection.NorthSouth ? TrafficDirection.EastWest : TrafficDirection.NorthSouth;

        this.ChangeLights(this._currentTrafficDirection, LightColor.Yellow);
        await UniTask.WaitForSeconds(3f);
        this.StopFlashingPedestrianStopLights(this._currentTrafficDirection);
        this.ChangeLights(this._currentTrafficDirection, LightColor.Red);
        await UniTask.WaitForSeconds(3f);
        this.ChangeLights(oppositeTrafficDirection, LightColor.Green);

        this._currentTrafficDirection = this._currentTrafficDirection == TrafficDirection.NorthSouth ? TrafficDirection.EastWest : TrafficDirection.NorthSouth;
        this._isChangingTrafficDirection = false;
        this._timeSinceLastChange = 0f;
        this.RandomizeTrafficChangeTimeout();
    }

    private void ChangeLights(TrafficDirection trafficDirection, LightColor color)
    {
        TrafficLightController[] vehicleTrafficLightsToChange = trafficDirection == TrafficDirection.NorthSouth ? this._northSouthTrafficLights : this._eastWestTrafficLights;
        TrafficLightController[] pedestrianTrafficLightsToChange = trafficDirection == TrafficDirection.NorthSouth ? this._eastWestTrafficLights : this._northSouthTrafficLights;

        foreach (var vehicleTrafficLightToChange in vehicleTrafficLightsToChange)
        {
            vehicleTrafficLightToChange.ChangeVehicleLights(color);

            if (color == LightColor.Green)
                vehicleTrafficLightToChange.ChangePedestrianLight(LightColor.Green, true);
            else if (color == LightColor.Red)
                vehicleTrafficLightToChange.ChangePedestrianLight(LightColor.Red, true);
        }

        foreach (var pedestrianTrafficLightToChange in pedestrianTrafficLightsToChange)
        {
            if (color == LightColor.Green)
                pedestrianTrafficLightToChange.ChangePedestrianLight(LightColor.Green, false);
            else if (color == LightColor.Red)
                pedestrianTrafficLightToChange.ChangePedestrianLight(LightColor.Red, false);
        }

        OnTrafficLightColorChange?.Invoke(trafficDirection, color == LightColor.Green);

        if (!this.IsHost) { return; }

        IntersectionState updatedState = this._state.Value;

        if (trafficDirection == TrafficDirection.NorthSouth)
            updatedState.NorthSouthLightColor = color;
        else
            updatedState.EastWestLightColor = color;

        this._state.Value = updatedState;
    }

    private void StartFlashingPedestrianStopLights(TrafficDirection trafficDirection)
    {
        TrafficLightController[] mainTrafficLightsToChange = trafficDirection == TrafficDirection.NorthSouth ? this._northSouthTrafficLights : this._eastWestTrafficLights;
        TrafficLightController[] otherTrafficLightsToChange = trafficDirection == TrafficDirection.NorthSouth ? this._eastWestTrafficLights : this._northSouthTrafficLights;

        foreach (var trafficLightToChange in mainTrafficLightsToChange)
        {
            trafficLightToChange.StartFlashingPedestrianStopLight(true);
        }

        foreach (var trafficLightToChange in otherTrafficLightsToChange)
        {
            trafficLightToChange.StartFlashingPedestrianStopLight(false);
        }

        if (!this.IsHost) { return; }

        IntersectionState updatedState = this._state.Value;

        if (trafficDirection == TrafficDirection.NorthSouth)
            updatedState.IsNorthSouthPedestrianLightFlashing = true;
        else
            updatedState.IsEastWestPedestrianLightFlashing = true;

        this._state.Value = updatedState;
    }

    private void StopFlashingPedestrianStopLights(TrafficDirection trafficDirection)
    {
        TrafficLightController[] mainTrafficLightsToChange = trafficDirection == TrafficDirection.NorthSouth ? this._northSouthTrafficLights : this._eastWestTrafficLights;
        TrafficLightController[] otherTrafficLightsToChange = trafficDirection == TrafficDirection.NorthSouth ? this._eastWestTrafficLights : this._northSouthTrafficLights;

        foreach (var trafficLightToChange in mainTrafficLightsToChange)
        {
            trafficLightToChange.StopFlashingPedestrianStopLight(true);
        }

        foreach (var trafficLightToChange in otherTrafficLightsToChange)
        {
            trafficLightToChange.StopFlashingPedestrianStopLight(false);
        }

        if (!this.IsHost) { return; }

        IntersectionState updatedState = this._state.Value;

        if (trafficDirection == TrafficDirection.NorthSouth)
            updatedState.IsNorthSouthPedestrianLightFlashing = false;
        else
            updatedState.IsEastWestPedestrianLightFlashing = false;

        this._state.Value = updatedState;
    }

    private void RandomizeTrafficChangeTimeout() => this._timeoutToChangeDirection = UnityEngine.Random.Range(_MIN_CHANGE_DIRECTION_TIMEOUT, _MAX_CHANGE_DIRECTION_TIMEOUT);
}

public enum TrafficDirection
{
    NorthSouth,
    EastWest,
}

[Serializable]
public struct IntersectionState : INetworkSerializable, System.IEquatable<IntersectionState>
{
    public LightColor NorthSouthLightColor;
    public LightColor EastWestLightColor;
    public bool IsNorthSouthPedestrianLightFlashing;
    public bool IsEastWestPedestrianLightFlashing;

    public IntersectionState(LightColor northSouthLightColor, LightColor eastWestLightColor, bool isNorthSouthFlashin, bool isEastWestFlashing)
    {
        this.NorthSouthLightColor = northSouthLightColor;
        this.EastWestLightColor = eastWestLightColor;
        this.IsNorthSouthPedestrianLightFlashing = isNorthSouthFlashin;
        this.IsEastWestPedestrianLightFlashing = isEastWestFlashing;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out NorthSouthLightColor);
            reader.ReadValueSafe(out EastWestLightColor);
            reader.ReadValueSafe(out IsNorthSouthPedestrianLightFlashing);
            reader.ReadValueSafe(out IsEastWestPedestrianLightFlashing);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(NorthSouthLightColor);
            writer.WriteValueSafe(EastWestLightColor);
            writer.WriteValueSafe(IsNorthSouthPedestrianLightFlashing);
            writer.WriteValueSafe(IsEastWestPedestrianLightFlashing);
        }
    }

    public readonly bool Equals(IntersectionState other) => NorthSouthLightColor == other.NorthSouthLightColor && EastWestLightColor == other.EastWestLightColor && IsNorthSouthPedestrianLightFlashing == other.IsNorthSouthPedestrianLightFlashing && IsEastWestPedestrianLightFlashing == other.IsEastWestPedestrianLightFlashing;
}
