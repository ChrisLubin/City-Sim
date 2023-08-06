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

    private enum TrafficDirection
    {
        NorthSouth,
        EastWest,
    }

    private void Awake()
    {
        this._currentTrafficDirection = Random.value < 0.5f ? TrafficDirection.NorthSouth : TrafficDirection.EastWest;
        this.RandomizeTrafficChangeTimeout();
    }

    private void Start()
    {
        TrafficLightController[] greenTrafficLights = this._currentTrafficDirection == TrafficDirection.NorthSouth ? this._northSouthTrafficLights : this._eastWestTrafficLights;
        TrafficLightController[] redTrafficLights = this._currentTrafficDirection == TrafficDirection.NorthSouth ? this._eastWestTrafficLights : this._northSouthTrafficLights;

        foreach (var greenTrafficLight in greenTrafficLights)
        {
            greenTrafficLight.ChangeLight(LightColor.Green);
        }
        foreach (var redTrafficLight in redTrafficLights)
        {
            redTrafficLight.ChangeLight(LightColor.Red);
        }
    }

    private void Update()
    {
        if (this._isChangingTrafficDirection) { return; }

        this._timeSinceLastChange += Time.deltaTime;

        // Turn on pedestrian stop light before light turns yellow
        if (this._timeoutToChangeDirection - this._timeSinceLastChange <= _TIMEOUT_TO_STOP_PEDESTRIAN_TRAFFIC)
        {
            TrafficLightController[] greenTrafficLights = this._currentTrafficDirection == TrafficDirection.NorthSouth ? this._northSouthTrafficLights : this._eastWestTrafficLights;
            foreach (var greenTrafficLight in greenTrafficLights)
            {
                greenTrafficLight.TurnOnPedestrianStopLight();
            }
        }
        if (this._timeSinceLastChange >= this._timeoutToChangeDirection)
        {
            this.TransitionToOtherDirection();
        }
    }

    private async void TransitionToOtherDirection()
    {
        this._isChangingTrafficDirection = true;
        TrafficLightController[] greenTrafficLights = this._currentTrafficDirection == TrafficDirection.NorthSouth ? this._northSouthTrafficLights : this._eastWestTrafficLights;
        TrafficLightController[] redTrafficLights = this._currentTrafficDirection == TrafficDirection.NorthSouth ? this._eastWestTrafficLights : this._northSouthTrafficLights;

        foreach (var greenTrafficLight in greenTrafficLights)
        {
            greenTrafficLight.ChangeLight(LightColor.Yellow);
        }
        await UniTask.WaitForSeconds(3f);
        foreach (var greenTrafficLight in greenTrafficLights)
        {
            greenTrafficLight.ChangeLight(LightColor.Red);
        }
        await UniTask.WaitForSeconds(3f);
        foreach (var redTrafficLight in redTrafficLights)
        {
            redTrafficLight.ChangeLight(LightColor.Green);
        }

        this._currentTrafficDirection = this._currentTrafficDirection == TrafficDirection.NorthSouth ? TrafficDirection.EastWest : TrafficDirection.NorthSouth;
        this._isChangingTrafficDirection = false;
        this._timeSinceLastChange = 0f;
        this.RandomizeTrafficChangeTimeout();
    }

    private void RandomizeTrafficChangeTimeout() => this._timeoutToChangeDirection = Random.Range(_MIN_CHANGE_DIRECTION_TIMEOUT, _MAX_CHANGE_DIRECTION_TIMEOUT);
}
