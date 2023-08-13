using Unity.Netcode;
using UnityEngine;

public class NPCController : NetworkBehaviorAutoDisable<NPCController>
{
    private SkinnedMeshRenderer[] _skins;

    private void Awake()
    {
        this._skins = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        this.RandomizeSkin();
    }

    protected override void OnOwnerNetworkSpawn()
    {
        this.RandomizeSkin();
    }

    private void RandomizeSkin()
    {
        foreach (SkinnedMeshRenderer skin in this._skins)
        {
            skin.gameObject.SetActive(false);
        }

        int randomSkinIndex = Random.Range(0, this._skins.Length - 1);
        this._skins[randomSkinIndex].gameObject.SetActive(true);
    }
}
