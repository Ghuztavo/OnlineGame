using System.Collections.Generic;
using Fusion;
using UnityEngine;

// Holds every connected player's authoritative state and provides the server-side damage entry-point.
// One instance is spawned per PlayerRef by PlayerSpawner.
public class NetworkPlayer : NetworkBehaviour
{
    // ── Networked State ────────────────────────────────────────────────────

    // Display name chosen in the lobby
    [Networked] public NetworkString<_32> Nickname { get; set; }

    // Current health is managed only by the server/host
    [Networked] public int Health { get; set; }

    // Kill count for the match scoreboard
    [Networked] public int Score { get; set; }

    // True once health reaches zero
    [Networked] public NetworkBool IsDead { get; set; }

    // ── Constants ──────────────────────────────────────────────────────────
    public const int MaxHealth = 100;

    // ── Change Detectors ──────────────────────────────────────────────────
    private ChangeDetector _changeDetector;

    // ── Events (local callbacks, not networked) ───────────────────────────
    // Raised on every client when this player's health changes
    public static event System.Action<NetworkPlayer, int> OnHealthChanged;

    // Raised on every client when this player dies
    public static event System.Action<NetworkPlayer>      OnPlayerDied;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasStateAuthority)
        {
            Health = MaxHealth;
            IsDead = false;
            Score  = 0;
        }

        // Assign the name
        if (HasInputAuthority)
        {
            //
        }

        // Register with the player registry
        PlayerRegistry.Register(Object.InputAuthority, this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        PlayerRegistry.Unregister(Object.InputAuthority);
    }

    public override void Render()
    {
        // Detect and broadcast state changes to local listeners every render frame
        foreach (var change in _changeDetector.DetectChanges(this, out var prev, out var current))
        {
            switch (change)
            {
                case nameof(Health):
                    OnHealthChanged?.Invoke(this, Health);
                    break;

                case nameof(IsDead) when IsDead:
                    OnPlayerDied?.Invoke(this);
                    break;
            }
        }
    }

    // ── RPCs ───────────────────────────────────────────────────────────────

    // Requested by the shooter's client, only the server/host applies the damage and updates the authoritative Health value
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float damage, PlayerRef shooter)
    {
        if (IsDead) return;

        Health -= Mathf.RoundToInt(damage);
        Health  = Mathf.Max(0, Health);

        if (Health == 0)
        {
            IsDead = true;
            HandleDeath(shooter);
        }
    }

    // let the local player set their name after spawning
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNickname(NetworkString<_32> nickname)
    {
        Nickname = nickname;
    }

    // ── Private Helpers ────────────────────────────────────────────────────

    private void HandleDeath(PlayerRef killer)
    {
        // Award kill to the shooter
        if (PlayerRegistry.TryGet(killer, out NetworkPlayer killerPlayer))
        {
            killerPlayer.Score++;
        }

        // if there is time im adding a respawn timer and a death animation or something
        Debug.Log($"[NetworkPlayer] {Nickname} was eliminated by {killer}.");
    }
}

// static registry of all connected players, keyed by PlayerRef
public static class PlayerRegistry
{
    private static readonly Dictionary<PlayerRef, NetworkPlayer> _players = new();

    public static void Register(PlayerRef playerRef, NetworkPlayer player)
    {
        _players[playerRef] = player;
    }

    public static void Unregister(PlayerRef playerRef)
    {
        _players.Remove(playerRef);
    }

    public static bool TryGet(PlayerRef playerRef, out NetworkPlayer player)
    {
        return _players.TryGetValue(playerRef, out player);
    }

    public static IEnumerable<NetworkPlayer> All => _players.Values;
}
