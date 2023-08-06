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
    [SerializeField] private GameObject _pedestrianRedLightWithSameDirection;
    [SerializeField] private GameObject _pedestrianGreenLightWithSameDirection;
    [SerializeField] private GameObject _pedestrianRedLightWithDiffDirection;
    [SerializeField] private GameObject _pedestrianGreenLightWithDiffDirection;

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

    private bool _isFlashingPedestrianStopLightWithSameDirection = false;
    private bool _isFlashingPedestrianStopLightWithDiffDirection = false;
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

    public void StopFlashingPedestrianStopLight(bool hasSameDirectionAsVehicleLights)
    {
        if (hasSameDirectionAsVehicleLights)
            this._isFlashingPedestrianStopLightWithSameDirection = false;
        else
            this._isFlashingPedestrianStopLightWithDiffDirection = false;
    }

    public async void StartFlashingPedestrianStopLight(bool hasSameDirectionAsVehicleLights)
    {
        if (this.GetIsFlashingPedestrianStopLight(hasSameDirectionAsVehicleLights)) { return; }
        GameObject pedestrianGreenLight = hasSameDirectionAsVehicleLights ? this._pedestrianGreenLightWithSameDirection : this._pedestrianGreenLightWithDiffDirection;
        GameObject pedestrianRedLight = hasSameDirectionAsVehicleLights ? this._pedestrianRedLightWithSameDirection : this._pedestrianRedLightWithDiffDirection;

        if (hasSameDirectionAsVehicleLights)
            this._isFlashingPedestrianStopLightWithSameDirection = true;
        else
            this._isFlashingPedestrianStopLightWithDiffDirection = true;

        pedestrianGreenLight.SetActive(false);

        while (this.GetIsFlashingPedestrianStopLight(hasSameDirectionAsVehicleLights))
        {
            pedestrianRedLight.SetActive(true);
            await UniTask.WaitForSeconds(_FLASHING_PEDESTRIAN_LIGHT_TIME);

            if (!this.GetIsFlashingPedestrianStopLight(hasSameDirectionAsVehicleLights))
                break;

            pedestrianRedLight.SetActive(false);
            await UniTask.WaitForSeconds(_FLASHING_PEDESTRIAN_LIGHT_TIME);
        }
    }

    public void ChangePedestrianLight(LightColor color, bool hasSameDirectionAsVehicleLights)
    {
        GameObject greenPedestrianLight = hasSameDirectionAsVehicleLights ? this._pedestrianGreenLightWithSameDirection : this._pedestrianGreenLightWithDiffDirection;
        GameObject redPedestrianLight = hasSameDirectionAsVehicleLights ? this._pedestrianRedLightWithSameDirection : this._pedestrianRedLightWithDiffDirection;

        if (color == LightColor.Green)
        {
            greenPedestrianLight.SetActive(true);
            redPedestrianLight.SetActive(false);
        }
        else if (color == LightColor.Red)
        {
            greenPedestrianLight.SetActive(false);
            redPedestrianLight.SetActive(true);
        }
    }

    public void ChangeVehicleLights(LightColor color)
    {
        this.TurnOffAllVehicleLights();

        foreach (MeshRenderer meshRenderers in this._lightColorToVehicleLightsMeshRendererMap[color])
        {
            meshRenderers.material = this.GetLightMaterial(color, LightMaterialType.Glow);
        }
    }

    private void TurnOffAllVehicleLights()
    {
        foreach (MeshRenderer meshRenderers in this._lightColorToVehicleLightsMeshRendererMap[LightColor.Red])
        {
            meshRenderers.material = this.GetLightMaterial(LightColor.Red, LightMaterialType.Normal);
        }
        foreach (MeshRenderer meshRenderers in this._lightColorToVehicleLightsMeshRendererMap[LightColor.Yellow])
        {
            meshRenderers.material = this.GetLightMaterial(LightColor.Yellow, LightMaterialType.Normal);
        }
        foreach (MeshRenderer meshRenderers in this._lightColorToVehicleLightsMeshRendererMap[LightColor.Green])
        {
            meshRenderers.material = this.GetLightMaterial(LightColor.Green, LightMaterialType.Normal);
        }
    }

    private bool GetIsFlashingPedestrianStopLight(bool hasSameDirectionAsVehicleLights) => hasSameDirectionAsVehicleLights ? this._isFlashingPedestrianStopLightWithSameDirection : this._isFlashingPedestrianStopLightWithDiffDirection;

    private Material GetLightMaterial(LightColor color, LightMaterialType type)
    {
        IDictionary<LightColor, Material> materialMap = type == LightMaterialType.Normal ? this._lightColorToNormalLightMaterialMap : this._lightColorToGlowLightMaterialMap;
        return materialMap[color];
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
