using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PhysicalProjectile : ProjectileBase {
    // --- Inspector parameters
    public PhysicalProjectileSettings settings;

    // --- Private members
    private Vector3 m_velocity;
    private Vector3 m_initialPosition;
    private Vector3 m_lastRootPosition;
    private Vector3 m_trajectoryCorrectionVector;
    private Vector3 m_remainingTrajectoryCorrectionVector;
    private Vector3 m_rootOffset;
    private Vector3 m_tipOffset;
    private float m_timeElapsed;
    private int m_bouncesLeft;
    private bool m_hasTrajectoryCorrection;
    private TrailController m_trail;
    private RaycastHit[] m_bufferedHits = new RaycastHit[Constants.PROJECTILES_BUFFERED_HITS];


    // --- MonoBehaviour methods
#if UNITY_EDITOR
    private void Awake() {
        // Subscribe to events
        settings.ProjectileSettingsChanged.AddListener(OnValidate);
    }
#endif

    private void Update() {
        // Time to live
        m_timeElapsed += Time.deltaTime;
        if (m_timeElapsed >= settings.timeToLive) {
            if (settings.hitWhenTimeExpires) OnHit(transform.position, -transform.forward, null);
            else {
                ReleaseTrail();
                ReleaseProjectile();
                return;
            }
        }

        // Update velocity and transform
        if (settings.drag > 0f)
            m_velocity = m_velocity * Mathf.Pow(1 - settings.drag, Time.deltaTime);
        transform.position += m_velocity * Time.deltaTime;
        if (settings.gravityAcceleration > 0f) {
            transform.forward = m_velocity.normalized;
            m_velocity += Vector3.down * settings.gravityAcceleration * Time.deltaTime;
            UpdateRootTipOffsets();
        }

        // Correct trajectory
        if (m_hasTrajectoryCorrection) {
            // Compute correction
            float distanceThisFrame = (transform.position + m_rootOffset - m_lastRootPosition).magnitude;
            Vector3 correctionThisFrame = (distanceThisFrame / settings.trajectoryCorrectionDistance) * m_trajectoryCorrectionVector;
            correctionThisFrame = Vector3.ClampMagnitude(correctionThisFrame, m_remainingTrajectoryCorrectionVector.magnitude);
            m_remainingTrajectoryCorrectionVector -= correctionThisFrame;
            
            // Apply correction
            transform.position += correctionThisFrame;

            // Detect end of correction
            m_hasTrajectoryCorrection = m_remainingTrajectoryCorrectionVector.sqrMagnitude > k_nearZero;
        }

        // Hit detection
        RaycastHit closestHit = new RaycastHit { distance = Mathf.Infinity };
        bool foundHit = false;
        Vector3 displacementSinceLastFrame = transform.position + m_tipOffset - m_lastRootPosition;
        int hitsNumber;
        if (settings.radius > 0f) hitsNumber = Physics.SphereCastNonAlloc(
                m_lastRootPosition,
                settings.radius,
                displacementSinceLastFrame.normalized,
                m_bufferedHits,
                displacementSinceLastFrame.magnitude,
                settings.hittableLayers,
                QueryTriggerInteraction.Collide
            );
        else hitsNumber = Physics.RaycastNonAlloc(
                m_lastRootPosition,
                displacementSinceLastFrame.normalized,
                m_bufferedHits,
                displacementSinceLastFrame.magnitude,
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
        Vector3 newRootPosition = transform.position + m_rootOffset;
        if (foundHit) {
            // Handle case of casting while already inside a collider
            if (closestHit.distance <= 0f) {
                closestHit.point = newRootPosition;
                //closestHit.normal = -transform.forward;
            }
            OnHit(closestHit.point, closestHit.normal, closestHit.collider); // Last root position will be stored here if bouncing is enabled
        } else m_lastRootPosition = newRootPosition; // Just store last root position

        // Update trail position
        UpdateTrail();
    }

    private void OnValidate() {
        // This should allow correct gizmos drawing when tip and root are concerned
        UpdateRootTipOffsets();
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        float gizmoRadius = settings.radius > 0f ? settings.radius : 0.01f;
        if (!Application.isPlaying)
            UpdateRootTipOffsets();
        Gizmos.DrawSphere(transform.position, gizmoRadius);
        Gizmos.DrawSphere(transform.position + m_rootOffset, gizmoRadius);
        Gizmos.DrawSphere(transform.position + m_tipOffset, gizmoRadius);
    }

#if UNITY_EDITOR
    private void OnDestroy() {
        // Unsubscribe to events
        settings.ProjectileSettingsChanged.RemoveListener(OnValidate);
    }
#endif

    // --- PhysicalProjectile methods
    // !!! ENABLE OFFSET FOR ADJUSTABLE CROSSHAIR!!!
    override public void OnShoot(Vector3 muzzlePosition, Quaternion projectileRotation, GameObject sourceObject, Transform cameraTransform = null /*, float crosshairOffset */) {
        // Set source and gather its colliders to ignore
        source = sourceObject;
        ignoredColliders.Clear();
        ignoredColliders.AddRange(source.GetComponentsInChildren<Collider>());

        // Reset projectile's state
        transform.SetPositionAndRotation(muzzlePosition, projectileRotation);
        UpdateRootTipOffsets();
        m_velocity = transform.forward * settings.speed;
        m_timeElapsed = 0f;
        m_bouncesLeft = settings.numberOfBounces;

        // Handle trajectory correction
        if (cameraTransform && settings.trajectoryCorrectionDistance >= 0f) {
            m_hasTrajectoryCorrection = true;
            Vector3 muzzleToCamera = cameraTransform.position - muzzlePosition;
            m_trajectoryCorrectionVector = Vector3.ProjectOnPlane(muzzleToCamera, cameraTransform.forward);
            if (settings.trajectoryCorrectionDistance == 0) {
                m_remainingTrajectoryCorrectionVector = Vector3.zero;
                transform.position += m_trajectoryCorrectionVector;
                m_hasTrajectoryCorrection = false;
            } else m_remainingTrajectoryCorrectionVector = m_trajectoryCorrectionVector;
        } else m_hasTrajectoryCorrection = false;

        // Set initial positions here, since these could have been changed above
        m_initialPosition = transform.position;
        m_lastRootPosition = transform.position + m_rootOffset;

        // Prevent projectile from going through walls
        // NOTE: We're recalculating the distance of the projectile from the camera since it could
        //       instantly change if trajectoryCorrectionDistance == 0, as written above
        Vector3 cameraToProjectile = m_initialPosition - cameraTransform.position;
        RaycastHit closestHit = new RaycastHit { distance = Mathf.Infinity };
        bool foundHit = false;
        int hitsNumber;
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
        if (foundHit)
            OnHit(closestHit.point, closestHit.normal, closestHit.collider);

        // Initiate trail
        if (settings.trailPool != null) {
            m_trail = settings.trailPool.Get();
            m_trail.ResetToPosition(transform.position);
        }
    }

    override protected void OnHit(Vector3 point, Vector3 normal, Collider collider) {
        if ((settings.numberOfBounces < 0 || m_bouncesLeft > 0) && m_timeElapsed < settings.timeToLive) {
            // Mirror velocity vector onto plane
            Vector3 orthoVelocity = Vector3.Dot(m_velocity, normal) * normal;
            Vector3 paralVelocity = m_velocity - orthoVelocity;
            m_velocity = paralVelocity - orthoVelocity;

            // Apply elasticity factor on velocity
            m_velocity *= settings.impactElasticity;

            // Displace the root first; we'll deduct from there the actual position of the projectile.
            Vector3 normalizedVelocity = m_velocity.normalized;
            m_lastRootPosition = point + normal * settings.radius + normalizedVelocity * k_nearZero;

            // Displace projectile
            transform.forward = normalizedVelocity;
            UpdateRootTipOffsets();
            transform.position = m_lastRootPosition - m_rootOffset;

            // Decrease bounces counter and stop trajectory correction
            m_bouncesLeft--;
            m_hasTrajectoryCorrection = false;

            // NOTE: This code approximates bouncing: I'm forcing the dispacement of the root right
            //       next to the collision point, basically neglecting any previous velocity that
            //       would displace it way further. This is way simpler to compute and prevents me
            //       to perform additional tunneling checks.
        } else {
            if (collider != null) {
                Damageable damageable = collider.GetComponent<Damageable>();
                if (damageable)
                    damageable.InflictDamage(settings.damage, source);
            }
            // Add particle effects here
            UpdateTrail(point);
            ReleaseTrail();
            ReleaseProjectile();
        }
    }

    private void ReleaseTrail() {
        if (m_trail != null) {
            m_trail.BeginRelease();
            m_trail = null;
        }
    }

    private void UpdateRootTipOffsets() {
        // might be good to check if doing some caching on transform vectors speeds things up
        m_rootOffset = transform.forward * settings.root;
        m_tipOffset = transform.forward * settings.tip;
    }

    private void UpdateTrail() {
        UpdateTrail(transform.position);
    }

    private void UpdateTrail(Vector3 position) {
        if (m_trail != null)
            m_trail.transform.position = position;
    }
}
