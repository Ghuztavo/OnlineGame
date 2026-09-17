using Fusion;
using UnityEngine;

// the raycast runs only if is the state authority
// when a hit is confirmed it calls for the RPC_TakeDamage on the victim's script which updates the health value
public class NetworkedPistol : NetworkedWeapon
{
    // ── Inspector ──────────────────────────────────────────────────────────

    [Header("Pistol")]
    [Tooltip("The local camera used to build the aim ray")]
    [SerializeField] private Camera playerCamera;

    [SerializeField] private float damage = 25f;
    [SerializeField] private float range  = 100f;

    [Tooltip("Layers the bullet can hit.")]
    [SerializeField] private LayerMask hitMask;

    // ── Initialization ─────────────────────────────────────────────────────

    // called by the player after spawning the weapon, assigning the local player's camera so aim raycasts work
    public void Init(Camera cam)
    {
        playerCamera = cam;
    }

    // ── NetworkedWeapon Implementation ─────────────────────────────────────

    public override void Spawned()
    {
        base.Spawned();

        // if Init() wasn't called yet, try Camera.main.
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    protected override void PerformFire(NetworkRunner runner, PlayerRef shooter, Vector3 aimPos, Vector3 aimDir)
    {
        Ray ray = new Ray(aimPos, aimDir);

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask))
        {
            Debug.Log($"[NetworkedPistol] Hit '{hit.collider.name}' for {damage} dmg.");

            // Check if we hit a NetworkedPlayerController.
            if (hit.collider.TryGetComponent(out NetworkedPlayerController victim))
            {
                victim.ApplyDamage((int)damage);
            }
        }
    }

    protected override void StartReload()
    {
        // instant reload
        base.StartReload();
        Debug.Log("[NetworkedPistol] Reloading…");
    }
}
