using UnityEngine;

public class Pistol : Weapon
{
    [Header("Pistol")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float range = 100f;
    [SerializeField] private LayerMask hitMask;

    public override void Fire()
    {
        if (Time.time < nextTimeToFire) { return; }

        nextTimeToFire = Time.time + fireRate;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask))
        {
            Debug.Log($"Hit {hit.collider.name} for {damage} damage.");
            // TODO: Damage logic
        }
    }
}
