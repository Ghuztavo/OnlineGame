using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] protected float fireRate;

    protected float nextTimeToFire;

    public virtual void Fire()
    {
        if (Time.time < nextTimeToFire)
        {
            return;
        }

        nextTimeToFire = Time.time + fireRate;
    }
}
