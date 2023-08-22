using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class NpcManager : NetworkedStaticInstanceWithLogger<NpcManager>
{
    [SerializeField] Transform _npcPrefab;
    [SerializeField] Transform _npcContainer;
    [SerializeField] int _npcSpawnCount = 20;

    private const string _PEDESTRIAN_GRAPH_NAME = "Pedestrian Graph";
    private static Vector3[] _ALL_PEDESTRIAN_POINTS;

    protected override void Awake()
    {
        base.Awake();
        GameManager.OnStateChange += this.OnGameStateChange;

        if (_ALL_PEDESTRIAN_POINTS == null)
            _ALL_PEDESTRIAN_POINTS = GameObject.FindGameObjectsWithTag(Constants.TagNames.PedestrianPoints).Select(obj => obj.transform.position).ToArray();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        GameManager.OnStateChange -= this.OnGameStateChange;
    }

    private void OnGameStateChange(GameState state)
    {
        if (!this.IsHost) { return; }

        switch (state)
        {
            case GameState.GameStarted:
                this.SpawnNpcs(this._npcSpawnCount);
                break;
            default:
                break;
        }
    }

    private void SpawnNpcs(int amount)
    {
        if (!this.IsHost) { return; }

        for (int i = 0; i < amount; i++)
        {
            int randomSpawnPointIndex = UnityEngine.Random.Range(0, _ALL_PEDESTRIAN_POINTS.Length);
            Vector3 randomSpawnPoint = _ALL_PEDESTRIAN_POINTS[randomSpawnPointIndex];

            Transform npcTransform = Instantiate(this._npcPrefab, randomSpawnPoint, Quaternion.identity);
            NetworkObject npcNetworkObject = npcTransform.GetComponent<NetworkObject>();
            npcNetworkObject.SpawnWithOwnership(0);
            npcNetworkObject.TrySetParent(this._npcContainer);
        }

        this._logger.Log($"Spawned {amount} NPCs");
    }
}
