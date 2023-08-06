using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class IntersectionTrafficLightsController : MonoBehaviour
{
    [SerializeField] private TrafficLightController[] _northSouthTrafficLights;
    [SerializeField] private TrafficLightController[] _eastWestTrafficLights;

    private TrafficDirection _currentTrafficDirection;
    private bool _isChangingTrafficDirection = false;
    private float _timeSinceLastChange = 0f;
    private float _timeoutToChangeDirection;

    private const float _MIN_CHANGE_DIRECTION_TIMEOUT = 10f;
    private const float _MAX_CHANGE_DIRECTION_TIMEOUT = 17f;
    private const float _TIMEOUT_TO_STOP_PEDESTRIAN_TRAFFIC = 3f;

    public event Action<TrafficDirection, bool> OnTrafficLightColorChange;

    private void Awake()
    {
        this._currentTrafficDirection = UnityEngine.Random.value < 0.5f ? TrafficDirection.NorthSouth : TrafficDirection.EastWest;
        this.RandomizeTrafficChangeTimeout();
    }

    private void Start()
    {
        TrafficDirection redTrafficDirection = this._currentTrafficDirection == TrafficDirection.NorthSouth ? TrafficDirection.EastWest : TrafficDirection.NorthSouth;
        this.ChangeLights(this._currentTrafficDirection, LightColor.Green);
        this.ChangeLights(redTrafficDirection, LightColor.Red);
    }

    private void Update()
    {
        if (this._isChangingTrafficDirection) { return; }

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
    }

    private void RandomizeTrafficChangeTimeout() => this._timeoutToChangeDirection = UnityEngine.Random.Range(_MIN_CHANGE_DIRECTION_TIMEOUT, _MAX_CHANGE_DIRECTION_TIMEOUT);
}

public enum TrafficDirection
{
    NorthSouth,
    EastWest,
}
