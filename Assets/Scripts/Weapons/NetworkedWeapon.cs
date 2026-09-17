using Fusion;
using UnityEngine;


// Base clss for all networked weapons
public abstract class NetworkedWeapon : NetworkBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────

    [Header("Weapon Stats")]
    [SerializeField] protected float fireRate  = 0.5f;
    [SerializeField] protected int   maxAmmo   = 12;

    // ── Networked State ────────────────────────────────────────────────────

    // Remaining ammo in the magazine
    [Networked] public int Ammo { get; protected set; }

    [Networked] public NetworkBool IsReloading { get; protected set; }

    // ── Local State (not networked — just rate limiting) ───────────────────

    // Tick at which the weapon may fire again.
    protected float NextFireTime;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Ammo        = maxAmmo;
            IsReloading = false;
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    // Called every tick from the Player Cotroller when the fire button is held
    // only does the logic on the server/host and the clients see the effects via networked state
    public void NetworkFire(NetworkRunner runner, PlayerRef shooter, Vector3 aimPos, Vector3 aimDir)
    {
        // Rate limit
        if (runner.SimulationTime < NextFireTime) return;
        if (Ammo <= 0 || IsReloading)             return;
        if (!HasStateAuthority)                   return; // server runs the authoritative logic

        NextFireTime = runner.SimulationTime + fireRate;
        Ammo--;

        PerformFire(runner, shooter, aimPos, aimDir);

        if (Ammo <= 0)
            StartReload();
    }

    // ── Abstract / Virtual ─────────────────────────────────────────────────

    // weapon fire logic
    protected abstract void PerformFire(NetworkRunner runner, PlayerRef shooter, Vector3 aimPos, Vector3 aimDir);

    protected virtual void StartReload()
    {
        Ammo        = maxAmmo;
        IsReloading = false;
    }
}
