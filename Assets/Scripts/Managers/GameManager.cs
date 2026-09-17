using Fusion;
using UnityEngine;


// Handles global game states and events, such as restarting the round when a player dies.
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Called by NetworkedPlayerController when its health drops to 0.
    // Only runs if its the server/host
    public void OnPlayerDied(NetworkedPlayerController deadPlayer)
    {
        if (!HasStateAuthority) return;

        Debug.Log($"[GameManager] Player {deadPlayer.Object.InputAuthority} died. Restarting round for all players.");
        RestartRound();
    }

    private void RestartRound()
    {
        if (!HasStateAuthority) return;

        // Find the PlayerSpawner to get spawn points
        PlayerSpawner spawner = FindAnyObjectByType<PlayerSpawner>();

        // Find all active players in the scene and respawn them
        NetworkedPlayerController[] allPlayers = FindObjectsByType<NetworkedPlayerController>(FindObjectsSortMode.None);
        
        foreach (var player in allPlayers)
        {
            Vector3 spawnPos = Vector3.zero;
            
            if (spawner != null)
            {
                spawnPos = spawner.GetSpawnPosition();
            }

            player.Respawn(spawnPos);
        }
    }
}
