using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    [Header("Vehicle Lights")]
    [SerializeField] private MeshRenderer[] _vehicleRedLightsMeshRenderer;
    [SerializeField] private MeshRenderer[] _vehicleYellowLightsMeshRenderer;
    [SerializeField] private MeshRenderer[] _vehicleGreenLightsMeshRenderer;

    [Header("Pedestrian Lights")]
    [SerializeField] private GameObject _pedestrianRedLight;
    [SerializeField] private GameObject _pedestrianGreenLight;

    [Header("Light Materials")]
    [SerializeField] private Material _normalRedLightMaterial;
    [SerializeField] private Material _normalYellowLightMaterial;
    [SerializeField] private Material _normalGreenLightMaterial;
    [SerializeField] private Material _glowRedLightMaterial;
    [SerializeField] private Material _glowYellowLightMaterial;
    [SerializeField] private Material _glowGreenLightMaterial;

    private IDictionary<LightColor, MeshRenderer[]> _lightColorToVehicleLightsMeshRendererMap;
    private IDictionary<LightColor, Material> _lightColorToNormalLightMaterialMap;
    private IDictionary<LightColor, Material> _lightColorToGlowLightMaterialMap;

    private bool _isFlashingPedestrianStopLight = false;
    private const float _FLASHING_PEDESTRIAN_LIGHT_TIME = 0.7f;

    private enum LightMaterialType
    {
        Normal,
        Glow,
    }

    private void Awake()
    {
        this.InitializeMaps();
    }

    public void ChangeLight(LightColor color)
    {
        this.TurnOffAllLights();
        this.TurnOnLights(color);
    }

    public async void StartFlashingPedestrianStopLight()
    {
        if (this._isFlashingPedestrianStopLight) { return; }

        this._isFlashingPedestrianStopLight = true;
        this._pedestrianGreenLight.SetActive(false);

        while (this._isFlashingPedestrianStopLight)
        {
            this._pedestrianRedLight.SetActive(true);
            await UniTask.WaitForSeconds(_FLASHING_PEDESTRIAN_LIGHT_TIME);

            if (!this._isFlashingPedestrianStopLight)
                break;

            this._pedestrianRedLight.SetActive(false);
            await UniTask.WaitForSeconds(_FLASHING_PEDESTRIAN_LIGHT_TIME);
        }
    }

    public void StopFlashingPedestrianStopLight() => this._isFlashingPedestrianStopLight = false;

    private Material GetLightMaterial(LightColor color, LightMaterialType type)
    {
        IDictionary<LightColor, Material> materialMap = type == LightMaterialType.Normal ? this._lightColorToNormalLightMaterialMap : this._lightColorToGlowLightMaterialMap;
        return materialMap[color];
    }

    private void TurnOffAllLights()
    {
        this.TurnOffLights(LightColor.Red);
        this.TurnOffLights(LightColor.Yellow);
        this.TurnOffLights(LightColor.Green);
    }

    private void TurnOnLights(LightColor color)
    {
        MeshRenderer[] lightMeshRenderers = this._lightColorToVehicleLightsMeshRendererMap[color];
        foreach (MeshRenderer meshRenderers in lightMeshRenderers)
        {
            meshRenderers.material = this.GetLightMaterial(color, LightMaterialType.Glow);
        }

        switch (color)
        {
            case LightColor.Red:
                this._pedestrianRedLight.SetActive(true);
                break;
            case LightColor.Yellow:
                this._pedestrianRedLight.SetActive(true);
                break;
            case LightColor.Green:
                this._pedestrianGreenLight.SetActive(true);
                break;
            default:
                break;
        }
    }

    private void TurnOffLights(LightColor color)
    {
        MeshRenderer[] lightMeshRenderers = this._lightColorToVehicleLightsMeshRendererMap[color];
        foreach (MeshRenderer meshRenderers in lightMeshRenderers)
        {
            meshRenderers.material = this.GetLightMaterial(color, LightMaterialType.Normal);
        }

        switch (color)
        {
            case LightColor.Red:
                this._pedestrianRedLight.SetActive(false);
                break;
            case LightColor.Green:
                this._pedestrianGreenLight.SetActive(false);
                break;
            default:
                break;
        }
    }

    private void InitializeMaps()
    {
        this._lightColorToVehicleLightsMeshRendererMap = new Dictionary<LightColor, MeshRenderer[]>()
        {
            { LightColor.Red, this._vehicleRedLightsMeshRenderer },
            { LightColor.Yellow, this._vehicleYellowLightsMeshRenderer },
            { LightColor.Green, this._vehicleGreenLightsMeshRenderer },
        };
        this._lightColorToNormalLightMaterialMap = new Dictionary<LightColor, Material>()
        {
            { LightColor.Red, this._normalRedLightMaterial },
            { LightColor.Yellow, this._normalYellowLightMaterial },
            { LightColor.Green, this._normalGreenLightMaterial },
        };
        this._lightColorToGlowLightMaterialMap = new Dictionary<LightColor, Material>()
        {
            { LightColor.Red, this._glowRedLightMaterial },
            { LightColor.Yellow, this._glowYellowLightMaterial },
            { LightColor.Green, this._glowGreenLightMaterial },
        };
    }
}

public enum LightColor
{
    Red,
    Yellow,
    Green,
}
