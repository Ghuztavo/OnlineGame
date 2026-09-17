using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;


// Listens to Fusion join/leave callbacks and spawns/despawns the player prefab
// for each connected peer.
public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Prefabs")]
    [Tooltip("Your Player prefab.")] // the prefab needs to have a NEtworkObject component
    [SerializeField] private NetworkObject playerPrefab;

    // Tracks which NetworkObject belongs to which PlayerRef to despawn
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

    // ── INetworkRunnerCallbacks ─────────────────────────────────────────────

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Only the host/server spawns objects — clients receive the spawn from state sync
        if (!runner.IsServer) return;

        // Pick a spawn position
        Vector3 spawnPos = GetSpawnPosition();

        NetworkObject networkPlayerObject = runner.Spawn(
            playerPrefab,
            spawnPos,
            Quaternion.identity,
            player  // InputAuthority
        );

        _spawnedPlayers[player] = networkPlayerObject;

        Debug.Log($"[PlayerSpawner] Spawned player for {player} at {spawnPos}.");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedPlayers.TryGetValue(player, out NetworkObject obj))
        {
            runner.Despawn(obj);
            _spawnedPlayers.Remove(player);
            Debug.Log($"[PlayerSpawner] Despawned player for {player}.");
        }
    }

    // ── Spawn Point Helper ─────────────────────────────────────────────────

    private int _spawnIndex = 0;

    // returns a spawn position, it will go through all the objects that are tagged as "SpawnPoint" and get one of them
    public Vector3 GetSpawnPosition()
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");

        if (spawnPoints.Length == 0)
            return Vector3.zero;

        Vector3 pos = spawnPoints[_spawnIndex % spawnPoints.Length].transform.position;
        _spawnIndex++;
        return pos;
    }

    // ── Unused INetworkRunnerCallbacks (required by interface) ─────────────
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ReadOnlySpan<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
}
