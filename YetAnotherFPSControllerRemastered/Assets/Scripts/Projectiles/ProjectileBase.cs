using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public abstract class ProjectileBase : MonoBehaviour {
    // --- Public interface
    [HideInInspector] public List<Collider> ignoredColliders;
    [HideInInspector] public LinkedPool<ProjectileBase> projectilePool;
    public GameObject source { get; protected set; } = null;

    // --- Constants
    protected const float k_nearZero = 0.0001f;


    // --- MonoBehaviour methods

    // --- ProjectileBase methods
    public abstract void OnShoot(Vector3 muzzlePosition, Quaternion projectileRotation, GameObject sourceObject, Transform cameraTransform = null /*, float crosshairOffset */);
    
    protected abstract void OnHit(Vector3 point, Vector3 normal, Collider collider);
    
    protected void ReleaseProjectile() {
        // Return projectile to respective pool
        projectilePool.Release(this);
    }

    protected bool IsHitValid(RaycastHit hit) {
        // Ignore if the trigger is not damageable
        if (hit.collider.isTrigger && hit.collider.GetComponent<Damageable>() == null)
            return false;

        // Ignore source's colliders
        if (ignoredColliders != null && ignoredColliders.Contains(hit.collider))
            return false;

        // The hit is valid
        return true;
    }
}
