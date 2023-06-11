using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkedStaticInstanceWithLogger<PlayerManager>
{
    [SerializeField] Transform _playerPrefab;
    [SerializeField] Transform _playerSpawnArea;
    [SerializeField] float _playerSpawnMaxDistance = 9f;

    private IDictionary<ulong, PlayerController> _playersMap = new Dictionary<ulong, PlayerController>();

    protected override void Awake()
    {
        base.Awake();
        GameManager.OnStateChange += this.OnGameStateChange;
    }

    public override void OnDestroy()
    {
        base.Awake();
        GameManager.OnStateChange -= this.OnGameStateChange;
    }

    private void OnGameStateChange(GameState state)
    {
        switch (state)
        {
            case GameState.GameStarted:
                const int hostId = 0;
                this.SpawnPlayer(hostId);
                break;
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (!this.IsHost) { return; }
        if (this._playersMap.ContainsKey(clientId))
        {
            this._logger.Log($"This player is still alive. Cannot spawn them again.", Logger.LogLevel.Error);
            return;
        }

        Vector3 randomSpawnPoint = new(UnityEngine.Random.Range(this._playerSpawnArea.position.x - this._playerSpawnMaxDistance, this._playerSpawnArea.position.x + this._playerSpawnMaxDistance), 0, UnityEngine.Random.Range(this._playerSpawnArea.position.z - this._playerSpawnMaxDistance, this._playerSpawnArea.position.z + this._playerSpawnMaxDistance));
        Transform playerTransform = Instantiate(this._playerPrefab, randomSpawnPoint, this._playerSpawnArea.rotation);
        playerTransform.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
        this._logger.Log($"Spawned player for {MultiplayerSystem.Instance.GetPlayerUsername(clientId)}");
    }
}
