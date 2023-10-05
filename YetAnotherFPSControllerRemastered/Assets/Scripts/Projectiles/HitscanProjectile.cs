using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class HitscanProjectile : ProjectileBase {
    // --- Inspector parameters
    public HitscanProjectileSettings settings;
    
    // --- Private members
    private RaycastHit[] m_bufferedHits = new RaycastHit[Constants.PROJECTILES_BUFFERED_HITS];

    // --- MonoBehaviour methods
#if UNITY_EDITOR
    private void Awake() {
        // Subscribe to events
        settings.ProjectileSettingsChanged.AddListener(OnValidate);
    }
#endif

    private void OnValidate() { /* no_op */ }

#if UNITY_EDITOR
    private void OnDestroy() {
        // Unsubscribe to events
        settings.ProjectileSettingsChanged.RemoveListener(OnValidate);
    }
#endif

    // --- HitscanProjectile methods
    // !!! ENABLE OFFSET FOR ADJUSTABLE CROSSHAIR!!!
    override public void OnShoot(Vector3 muzzlePosition, Quaternion projectileRotation, GameObject sourceObject, Transform cameraTransform = null /*, float crosshairOffset */) {
        // Set source and gather its colliders to ignore
        source = sourceObject;
        ignoredColliders.Clear();
        ignoredColliders.AddRange(source.GetComponentsInChildren<Collider>());

        // Reset projectile's state
        transform.SetPositionAndRotation(muzzlePosition, projectileRotation);

        // Prevent projectile from going through walls
        int hitsNumber;
        Vector3 cameraToProjectile = muzzlePosition - cameraTransform.position;
        RaycastHit closestHit = new RaycastHit { distance = Mathf.Infinity };
        bool foundHit = false;
        hitsNumber = Physics.RaycastNonAlloc(
            cameraTransform.position,
            cameraToProjectile.normalized,
            m_bufferedHits,
            cameraToProjectile.magnitude,
            settings.hittableLayers,
            QueryTriggerInteraction.Collide
        );
        for (int i = 0; i < hitsNumber; i++) {
            RaycastHit hit = m_bufferedHits[i];
            if (IsHitValid(hit) && hit.distance < closestHit.distance) {
                foundHit = true;
                closestHit = hit;
            }
        }
        if (foundHit) {
            OnHit(closestHit.point, closestHit.normal, closestHit.collider);
            return;
        }

        // Trajectory correction
        Vector3 elapsedTrajectory = Vector3.zero;
        if (cameraTransform && settings.trajectoryCorrectionDistance >= 0f) {
            Vector3 correctionVector = Vector3.ProjectOnPlane(cameraTransform.position - muzzlePosition, cameraTransform.forward);
            elapsedTrajectory = transform.forward * settings.trajectoryCorrectionDistance + correctionVector;
            hitsNumber = Physics.RaycastNonAlloc(
                muzzlePosition,
                elapsedTrajectory.normalized,
                m_bufferedHits,
                elapsedTrajectory.magnitude,
                settings.hittableLayers,
                QueryTriggerInteraction.Collide
            );
            foundHit = false;
            for (int i = 0; i < hitsNumber; i++) {
                RaycastHit hit = m_bufferedHits[i];
                if (IsHitValid(hit) && hit.distance < closestHit.distance) {
                    foundHit = true;
                    closestHit = hit;
                }
            }
            if (foundHit) {
                ShowLine(muzzlePosition, closestHit.point);
                OnHit(closestHit.point, closestHit.normal, closestHit.collider);
                return;
            }
        }

        // Cast past correction
        hitsNumber = Physics.RaycastNonAlloc(
            muzzlePosition + elapsedTrajectory,
            transform.forward,
            m_bufferedHits,
            settings.maxDistance - settings.trajectoryCorrectionDistance,
            settings.hittableLayers,
            QueryTriggerInteraction.Collide
        );
        foundHit = false;
        for (int i = 0; i < hitsNumber; i++) {
            RaycastHit hit = m_bufferedHits[i];
            if (IsHitValid(hit) && hit.distance < closestHit.distance) {
                foundHit = true;
                closestHit = hit;
            }
        }
        if (foundHit) {
            ShowLine(muzzlePosition, closestHit.point);
            OnHit(closestHit.point, closestHit.normal, closestHit.collider);
            return;
        }

        // No hit found
        ShowLine(muzzlePosition, muzzlePosition + transform.forward * settings.maxDistance);
        ReleaseProjectile();
    }

    override protected void OnHit(Vector3 point, Vector3 normal, Collider collider) {
        if (collider != null) {
            Damageable damageable = collider.GetComponent<Damageable>();
            if (damageable)
                damageable.InflictDamage(settings.damage, source);
        }
        // Add particle effects here
        ReleaseProjectile();
    }

    private void ShowLine(Vector3 startPosition, Vector3 endPosition) {
        if (settings.linePool != null) {
            settings.linePool.Get().AnimateLine(startPosition, endPosition);
        }
    }
}