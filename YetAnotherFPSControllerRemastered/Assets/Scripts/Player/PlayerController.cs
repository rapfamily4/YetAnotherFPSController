/**
 * Luigi Rapetta (2022)
 * Special thanks to:
 *      Toyful Game's "Making A Physics Based Character Controller In Unity (for Very Very Valet)" --- https://www.youtube.com/watch?v=qdskE8PJy6Q&list=PLq6yZbnlz9u3oQbLWwWOuUekpY2_5WbSM&index=11
 *      Unity Technologies's FPS Microgame
 *      Coberto's "How to climb stairs as a Rigidbody (in Unity3D)" --- https://cobertos.com/blog/post/how-to-climb-stairs-unity3d/
 *      Catlike Coding's movement tutorials --- https://catlikecoding.com/unity/tutorials/movement/
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour {
    // --- Public members (!!! PLEASE, REMEMBER TO PUT TOOLTIPS !!!)
    [Header("Camera")]
    [Range(0f, 1f)] public float cameraHeight = 0.85f;
    [Range(0f, 1f)] public float cameraHeightOnLanding = 0.75f;
    [Min(0f)] public float landingSmoothing = 0.25f;
    [Min(0f)] public float landingSmoothingReset = 0.25f;
    [Min(0f)] public float landingVelocityUpperLimit = 5;

    [Header("Movement")]
    [Min(0f)] public float maxMovementSpeed = 5f;
    [Min(0f)] public float acceleration = 30f;
    [Min(0f)] public float maxSharpTurnMultiplier = 2f;
    [Min(0f)] public float coyoteTime = 0.2f;
    [Range(0f, 1f)] public float airControl = 0.25f;
    [Range(0f, 90f)] public float maxGroundAngle = 45f;
    public bool airborneSharpTurn = false;

    [Header("Ground Snap")]
    [Min(0f)] public float maxSnapSpeed = 7.5f;
    [Min(0f)] public float sweepDistance = 0.5f;

    [Header("Collision Detection")]
    public LayerMask layersToIgnore;

    [Header("Gravity")]
    [Min(0f)] public float customGravity = 9.81f;

    [Header("Jump")]
    [Min(0f)] public float jumpHeight = 2f;
    [Min(0)] public int maxAirJumps = 0;
    public bool enableWallJump = true;
    public bool jumpCancelsVerticalVelocity = true;
    public bool jumpAlongGroundNormal = true;

    [Header("Crouch")]
    [Range(0.5f, 1f)] public float crouchHeight = 0.75f;
    [Range(0f, 1f)] public float crouchSpeed = 0.5f;
    [Min(0f)] public float crouchTransitionSpeed = 10f;

    [Header("Thrust")]
    public bool enableThrust = true;
    [Min(0f)] public float thrustHeight = 6f;
    [Range(0f, 1f)] public float thrustVerticalBias = 0.25f;

    // --- Public properties
    public bool isCrouching { get { return m_isCrouching; } }
    public Vector2 moveInput { get { return m_moveInput; } }
    public Vector2 lookInput { get { return m_lookInput; } }
    public Vector3 velocity { get { return m_rigidbody.velocity; } }

    // --- Private members
    private CapsuleCollider m_collider;
    private Rigidbody m_rigidbody;
    private Camera m_cameraHolder;
    private WeaponController m_weaponController;
    private float m_cameraPitch;
    private float m_cameraYaw;
    private float m_landingSmoothingElapsed;
    private float m_landingSmoothingResetElapsed;
    private float m_cameraHeightCurrent;
    private float m_cameraHeightTarget;
    private float m_standingCapsuleHeight;
    private float m_targetCapsuleHeight;
    private float m_minGroundDotProduct;
    private float m_jumpMagnitude;
    private float m_thrustMagnitude;
    private float m_coyoteTimeCounter;
    private int m_stepsSinceLastGrounded;
    private int m_stepsSinceLastJump;
    private int m_jumpPhase;
    private int m_thrustPhase;
    private bool m_isGrounded = false;
    private bool m_isSteeped= false;
    private bool m_isCrouching = false;
    private bool m_isBufferingJump = false;
    private List<ContactPointWrapper> m_contactPoints;
    private Vector2 m_lookInput;
    private Vector3 m_moveInput;
    private Vector3 m_groundNormal;
    private Vector3 m_groundRelativeVelocity;
    private Vector3 m_steepNormal;

    // --- Private constants
    private const float k_sphereCastRadiusScale = 0.99f;
    private const float k_dotBias = 1.414214e-6f; // It mitigates floating point imprecision in UnderSlopeLimit
    private const float k_nearZeroThreshold = 0.0001f; // Useful for defining the immediate surrounding of a point.
    private const float k_minSteepDotProduct = -0.01f;
    private const int k_groundStepsThreshold = 1;
    private const int k_jumpStepsThreshold = 5;


    // --- MonoBehaviour methods
    void Awake() {
        // Retrieve references
        m_collider = GetComponent<CapsuleCollider>();
        m_rigidbody = GetComponent<Rigidbody>();
        m_cameraHolder = GetComponentInChildren<Camera>();
        m_weaponController = GetComponentInChildren<WeaponController>();

        // Setup state
        m_moveInput = Vector3.zero;
        m_contactPoints = new List<ContactPointWrapper>();
        m_standingCapsuleHeight = m_collider.height;
        m_coyoteTimeCounter = 0f;
        m_stepsSinceLastGrounded = 0;
        m_stepsSinceLastJump = 0;
        m_jumpPhase = 0;
        m_thrustPhase = 0;
        m_cameraHeightTarget = m_cameraHeightCurrent = cameraHeight;
        m_landingSmoothingElapsed = landingSmoothing;
        m_landingSmoothingResetElapsed = landingSmoothingReset;
        OnValidate();

        // Freeze rigidbody's rotation and disable implicit gravity
        m_rigidbody.freezeRotation = true;
        m_rigidbody.useGravity = false;

        // Force player's height and crouch state
        DoCrouch(false, true);
        UpdateCapsuleHeight(true);
    }

    void OnValidate() {
        // Validate minimum dot product for contact normals
        m_minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        // Make the bias weighted with the cosine of the angle
        m_minGroundDotProduct -= k_dotBias * m_minGroundDotProduct;

        // Pre-calculate jump and thrust magnitudes; -2 * g * h
        m_jumpMagnitude = Mathf.Sqrt(2f * customGravity * jumpHeight);
        m_thrustMagnitude = Mathf.Sqrt(2f * customGravity * thrustHeight); 
    }

    void FixedUpdate() {
        // Forcely awake the agent
        if (m_rigidbody.IsSleeping()) m_rigidbody.WakeUp();

        // Keep movement coherent
        ContactCheck();
        ApplyGravity();
        ApplyMovement();

        // Reset contact points' list
        m_contactPoints.Clear();
    }

    void Update() {
        // Update casule height here to make camera movement smoother while crouching
        UpdateCapsuleHeight();

        // Jump if a jump has been buffered
        HandleBufferedJump();
    }

    void LateUpdate() {
        // Update camera's height
        if (m_landingSmoothingElapsed < landingSmoothing) {
            m_landingSmoothingElapsed += Time.deltaTime;
            if (m_landingSmoothingElapsed >= landingSmoothing) 
                m_landingSmoothingElapsed = landingSmoothing;
        }
        if (m_landingSmoothingResetElapsed < landingSmoothingReset) {
            m_landingSmoothingResetElapsed += Time.deltaTime;
            if (m_landingSmoothingResetElapsed >= landingSmoothingReset)
                m_landingSmoothingResetElapsed = landingSmoothingReset;
        }
        m_cameraHeightCurrent = Mathf.Lerp(m_cameraHeightCurrent, m_cameraHeightTarget, m_landingSmoothingElapsed / landingSmoothing);
        m_cameraHeightTarget = Mathf.Lerp(m_cameraHeightTarget, cameraHeight, m_landingSmoothingResetElapsed / landingSmoothingReset);

        // Update camera's transform here
        m_cameraHolder.transform.SetPositionAndRotation(
            transform.position + transform.up * m_collider.height * m_cameraHeightCurrent,
            Quaternion.Euler(Vector3.up * m_cameraYaw + Vector3.right * m_cameraPitch)
        );
    }

    void OnCollisionEnter(Collision col) {
        // Add contacts to contact points' list
        ContactPoint[] contacts = new ContactPoint[col.contactCount];
        col.GetContacts(contacts);
        foreach (ContactPoint contact in contacts)
            m_contactPoints.Add(new ContactPointWrapper(contact, col.relativeVelocity));
    }

    void OnCollisionStay(Collision col) {
        // Add contacts to contact points' list
        ContactPoint[] contacts = new ContactPoint[col.contactCount];
        col.GetContacts(contacts);
        foreach (ContactPoint contact in contacts)
            m_contactPoints.Add(new ContactPointWrapper(contact, col.relativeVelocity));
    }

    private void OnDrawGizmos() {
        if (m_rigidbody == null) return;

        // Draw target direction
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + m_moveInput);

        // Draw velocity
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + m_rigidbody.velocity);

        // Draw contact points
        Gizmos.color = Color.red;
        foreach (ContactPointWrapper cp in m_contactPoints) {
            Gizmos.DrawSphere(cp.point, 0.05f);
            Gizmos.DrawLine(cp.point, cp.point + cp.normal * 0.5f);
        }
    }

    void OnGUI() {
        string state = "isGrounded: " + m_isGrounded +
                       "\nisSteeped: " + m_isSteeped +
                       "\nisBufferingJump: " + m_isBufferingJump +
                       "\njumpPhase: " + m_jumpPhase +
                       "\nthrustPhase: " + m_thrustPhase;
        GUILayout.Label($"<color='black'><size=14>{state}</size></color>");
    }


    // --- PlayerController methods
    public void DoMove(Vector2 input) {
        // Just set move input
        m_moveInput = input;

        // If player has a weapon, handle movement sway
        if (m_weaponController)
            m_weaponController.SetMovementTarget(input);
    }

    public void DoLook(Vector2 delta) {
        // Update camera angles
        m_cameraPitch += delta.y * Time.deltaTime;
        m_cameraPitch = Mathf.Clamp(m_cameraPitch, -90f, 90f);
        m_cameraYaw += delta.x * Time.deltaTime;
        m_cameraYaw %= 360f;

        // Apply rotations
        m_rigidbody.MoveRotation(Quaternion.Euler(0f, m_cameraYaw, 0f));

        // If player has a weapon, handle view sway
        if (m_weaponController)
            m_weaponController.SetViewSwayTarget(delta.x, Mathf.Abs(m_cameraPitch) < 90f ? delta.y : 0f);

        // Store delta
        m_lookInput = delta;
    }

    public void DoJump() {
        // Define jump direction, or return if anything is invalid
        Vector3 jumpDirection;
        if (m_isGrounded)
            jumpDirection = jumpAlongGroundNormal ? m_groundNormal : Vector3.up;
        else if (enableWallJump && m_isSteeped) {
            m_jumpPhase = 0; // Reset jump phase only if it's a walljump (while ground check always resets)
            jumpDirection = m_steepNormal;
        } else if (maxAirJumps > 0 && m_jumpPhase <= maxAirJumps) {
            if (m_jumpPhase == 0) m_jumpPhase = 1; // This prevents air jumping one extra time after falling off a surface without jumping
            jumpDirection = jumpAlongGroundNormal ? m_groundNormal : Vector3.up;
        } else return;

        // Apply vertical bias to jump direction
        jumpDirection = (jumpDirection + Vector3.up).normalized;

        // Cancel vertical velocity if needed
        if (jumpCancelsVerticalVelocity || m_rigidbody.velocity.y < 0f)
            m_rigidbody.velocity = new Vector3(m_rigidbody.velocity.x, 0f, m_rigidbody.velocity.z);

        // Apply jump
        m_rigidbody.AddForce(jumpDirection * m_jumpMagnitude, ForceMode.VelocityChange);

        // Update jump status
        m_jumpPhase++;
        m_stepsSinceLastJump = 0;
        m_isBufferingJump = false;

        // Trigger jump animation
        if (m_weaponController)
            m_weaponController.TriggerJump();
    }

    public void DoThrust() {
        // Return if thrust is disabled
        if (!enableThrust) return;

        // Compute thrust direction
        Vector3 thrustDirection = (m_cameraHolder.transform.forward + transform.right * m_moveInput.x).normalized;
        float dot = Mathf.Clamp01(Vector3.Dot(transform.forward, m_cameraHolder.transform.forward));
        thrustDirection = (thrustDirection + (transform.up * thrustVerticalBias * dot)).normalized;

        // Apply thrust
        m_rigidbody.velocity = Vector3.zero;
        m_rigidbody.AddForce(thrustDirection * m_thrustMagnitude, ForceMode.VelocityChange);

        // Update thrust status
        m_thrustPhase = 1;
        m_stepsSinceLastJump = 0;
    }

    public bool DoCrouch(bool crouched, bool ignoreObstructions = false) {
        if (crouched) m_targetCapsuleHeight = m_standingCapsuleHeight * crouchHeight;
        else {
            if (!ignoreObstructions) {
                // Check for any obstructions above the player
                // NOTE: The capsule cast doesn't reflect agent's metrics accurately
                //       in order to prevent intersecting with the floor or walls.
                Collider[] obstructions = Physics.OverlapCapsule(
                    transform.position + transform.up * m_collider.radius,
                    transform.position + transform.up * (m_standingCapsuleHeight - m_collider.radius * k_sphereCastRadiusScale),
                    m_collider.radius * k_sphereCastRadiusScale,
                    ~layersToIgnore
                );
                foreach (Collider c in obstructions)
                    if (c != m_collider) return false;
            }
            m_targetCapsuleHeight = m_standingCapsuleHeight;
        }
        m_isCrouching = crouched;
        return true;
    }

    public void SetJumpBuffer(bool bufferState) {
        if (m_isGrounded) return;
        m_isBufferingJump = bufferState;
    }

    private void ContactCheck() {
        // Increment step counters
        m_stepsSinceLastGrounded++;
        m_stepsSinceLastJump++;

        // Store previous contact state and reset it
        bool previousGrounded = m_isGrounded;
        m_isGrounded = false;
        m_isSteeped = false;

        // Return if the player just jumped
        if (m_stepsSinceLastJump < k_jumpStepsThreshold) {
            m_groundNormal = Vector3.up;
            m_groundRelativeVelocity = Vector3.zero;
            m_steepNormal = Vector3.up;
            return;
        }

        // Check if grounded or steeped, building the respective normals
        m_groundNormal = Vector3.zero;
        m_groundRelativeVelocity = Vector3.zero;
        m_steepNormal = Vector3.zero;
        float groundedCounter = 0;
        foreach (ContactPointWrapper cp in m_contactPoints) {
            Vector3 normal = cp.normal;
            if (UnderSlopeLimit(normal)) {
                m_isGrounded = true;
                m_jumpPhase = 0;
                m_thrustPhase = 0;
                m_coyoteTimeCounter = 0f;
                m_stepsSinceLastGrounded = 0;
                m_groundNormal += normal;
                m_groundRelativeVelocity += cp.relativeVelocity;
                groundedCounter++;
            }
            if (UnderSteepLimit(normal)) {
                m_isSteeped = true;
                m_steepNormal += normal;
            }
        }

        // Consolidate steep normal
        if (m_isSteeped) m_steepNormal.Normalize();
        else m_steepNormal = Vector3.up; // Set a default value

        // Consolidate ground normal and relative velocity
        if (m_isGrounded) {
            m_groundNormal.Normalize();
            m_groundRelativeVelocity /= groundedCounter;
        } else {
            // Try to snap on ground or ckeck a crevasse
            m_groundNormal = Vector3.up;  // Set a default value
            m_isGrounded = SnapToGround() || CheckCrevasse(); // If true, then m_groundNormal gets updated inisde either SnapToGround or CheckCrevasse
        }

        // If even snapping failed, check if within coyote time
        m_coyoteTimeCounter += Time.deltaTime;
        if (!m_isGrounded &&
            (!m_isSteeped || UnderSlopeLimit(m_steepNormal)) && // You don't want to allow jumps on too steep slopes...
            m_coyoteTimeCounter <= coyoteTime &&
            m_jumpPhase == 0 &&
            m_thrustPhase == 0)
            m_isGrounded = true;

        // Inform weapon animator about the ground state and whether the player has landed or not
        if (m_weaponController) {
            m_weaponController.SetGrounded(m_isGrounded);
            // Is the player landing on ground?
            if (previousGrounded != m_isGrounded) {
                m_weaponController.TriggerLand();
                m_landingSmoothingElapsed = m_landingSmoothingResetElapsed = 0f;
                float landingForce = 1f - Mathf.Clamp01(Mathf.Abs(m_groundRelativeVelocity.y) / landingVelocityUpperLimit);
                m_cameraHeightTarget = Mathf.Lerp(cameraHeightOnLanding, cameraHeight, landingForce);
            }
        }
    }

    private bool SnapToGround() {
        // Return if the player didn't immediately loose contact, or when they just jumped
        // NOTE: The exact moment in which the player looses contact is when m_stepsSinceLastGrounded
        //       equals 1. It'll never be 0 inside this method.
        if (m_stepsSinceLastGrounded > k_groundStepsThreshold || m_stepsSinceLastJump < k_jumpStepsThreshold)
            return false;

        // Return if speed exceeds max ground snap speed
        // NOTE: At high speeds we want the player to get launched in the air anyways...
        float speed = m_rigidbody.velocity.magnitude;
        if (speed >= maxSnapSpeed) return false;

        // Return if there's nothing below to snap to
        if (!Physics.SphereCast(
            transform.position + Vector3.up * m_collider.radius,
            m_collider.radius,
            Vector3.down,
            out RaycastHit sphereHitInfo,
            sweepDistance,
            ~layersToIgnore
        )) return false;

        // Return if the hit normal is not below the slope limit
        if (!UnderSlopeLimit(sphereHitInfo.normal)) return false;

        // Return if the player is falling off a step or platform
        // NOTE: The hit normal of the sphere cast may come from the edge of this platform,
        //       which may be under the slope limit. This check is very similar to the one
        //       of step detection.
        Vector3 rayOrigin = new Vector3(transform.position.x, sphereHitInfo.point.y - k_nearZeroThreshold, transform.position.z);
        Vector3 rayDistance = ProjectOnPlane(sphereHitInfo.point - transform.position, Vector3.up);
        if (Vector3.Dot(m_rigidbody.velocity, rayDistance) < 0f &&
            !Physics.Raycast(transform.position, Vector3.down, transform.position.y - sphereHitInfo.point.y + k_nearZeroThreshold, ~layersToIgnore) && // The next raycast has to have nothing above its origin
            (!Physics.Raycast(rayOrigin, rayDistance.normalized, out RaycastHit rayHitInfo, rayDistance.magnitude, ~layersToIgnore) ||
             !UnderSlopeLimit(rayHitInfo.normal))) return false;

        // Snap to ground
        // NOTE: At this point, the player just lost contact to the ground and they're above it.
        m_groundNormal = sphereHitInfo.normal;
        if (m_stepsSinceLastJump >= k_jumpStepsThreshold) {
            m_jumpPhase = 0;
            m_thrustPhase = 0;
        }
        m_stepsSinceLastGrounded = 0;
        float dot = Vector3.Dot(m_rigidbody.velocity, sphereHitInfo.normal);
        if (dot > 0f)
            // NOTE: Rotate onto plane only if the velocity aims along the direction of the hit
            //       normal (dot > 0). Velocity aiming behind the direction of the hit normal is
            //       already useful for realigning the player to the ground: we would lose this
            //       advantage if we rotated the vector along the plane.
            m_rigidbody.velocity = (m_rigidbody.velocity - sphereHitInfo.normal * dot).normalized * speed;
        return true;
    }

    private bool CheckCrevasse() {
        if (!m_isSteeped) return false;

        // Force ground detection if the steep normal is a valuable ground normal
        if (UnderSlopeLimit(m_steepNormal)) {
            m_groundNormal = m_steepNormal;
            if (m_stepsSinceLastJump >= k_jumpStepsThreshold) {
                m_jumpPhase = 0;
                m_thrustPhase = 0;
            }
            m_stepsSinceLastGrounded = 0;
            return true;
        }

        return false;
    }

    private void ApplyGravity() {
        // NOTE: Applying the gravity along the ground normal fixes the issue of slowly sliding
        //       down a slope while not providing any movement input.
        m_rigidbody.AddForce(-m_groundNormal * customGravity, ForceMode.Force);
    }

    public void ApplyMovement() {
        // Abort if...
        if (!m_isGrounded && m_moveInput.sqrMagnitude == 0) return;

        // Calculate target velocity
        float targetMagnitude = maxMovementSpeed * (m_isCrouching && m_isGrounded ? crouchSpeed : 1f);
        Vector3 targetDirection = transform.right * m_moveInput.x + transform.forward * m_moveInput.y;
        Vector3 targetVelocity = targetDirection * targetMagnitude;

        // Compute horizontal velocity and inform weapon animator about it
        Vector3 currentHorizontalVelocity = new Vector3(m_rigidbody.velocity.x, 0f, m_rigidbody.velocity.z); // Consider horizontal velocity in this case
        Vector3 currentHorizontalDirection = currentHorizontalVelocity.normalized;
        if (m_weaponController)
            m_weaponController.SetRelativeHorizontalVelocity(currentHorizontalVelocity.magnitude / maxMovementSpeed);

        // Calculate dot product between the current horizontal velocity and the target
        float dot = Vector3.Dot(currentHorizontalDirection, targetDirection);

        // Calculate max acceleration
        float sharpTurnMultiplier = ((airborneSharpTurn || m_isGrounded) && dot < 0) ? Mathf.Lerp(1f, maxSharpTurnMultiplier, -dot) : 1f;
        float maxAcceleration = acceleration * (m_isGrounded ? 1f : airControl) * sharpTurnMultiplier;

        // Process target velocity
        if (m_isGrounded) {
            // Project the target onto the plane
            targetVelocity = targetMagnitude * ProjectOnPlane(targetVelocity, m_groundNormal).normalized;

            // Use velocities parallel to the contact plane (slope handling)
            // NOTE: This is crucial for getting a correct velocity difference (dv).
            currentHorizontalVelocity = ProjectOnPlane(m_rigidbody.velocity, m_groundNormal);
        } else {
            // Modify target velocity while airborne in order to keep horizontal speed
            float currentHorizontalMagnitude = currentHorizontalVelocity.magnitude;
            if (currentHorizontalMagnitude > maxMovementSpeed)
                targetVelocity = Vector3.ClampMagnitude(currentHorizontalVelocity * Mathf.Clamp01(dot) + targetVelocity, currentHorizontalMagnitude);
        }

        // Calculate acceleration to apply
        Vector3 accelerationToApply = (targetVelocity - currentHorizontalVelocity) / Time.deltaTime; // (dv / dt) = a
        accelerationToApply = Vector3.ClampMagnitude(accelerationToApply, maxAcceleration);

        // If steeped and not grounded, remove the component of the acceleration towards the steep
        if (!m_isGrounded && m_isSteeped && Vector3.Dot(m_steepNormal, targetDirection) < 0f) {
            Vector3 right = Vector3.Cross(-m_steepNormal, Vector3.up).normalized;
            accelerationToApply = right * Vector3.Dot(right, accelerationToApply);
            // also project velocity on steep (like slope handling)?
        }

        // Apply force
        m_rigidbody.AddForce(accelerationToApply, ForceMode.Force);
    }

    private void UpdateCapsuleHeight(bool force = false) {
        if (force) m_collider.height = m_targetCapsuleHeight;
        else if (m_collider.height != m_targetCapsuleHeight) {
            // Calculate the next height
            float nextHeight = Mathf.Lerp(m_collider.height, m_targetCapsuleHeight, crouchTransitionSpeed * Time.deltaTime);

            if (!m_isCrouching) {
                // Check for any obstructions above the player
                // NOTE: The capsule cast doesn't reflect agent's metrics accurately
                //       in order to prevent intersecting with the floor or walls.
                Collider[] obstructions = Physics.OverlapCapsule(
                    transform.position + transform.up * m_collider.radius,
                    transform.position + transform.up * (nextHeight - m_collider.radius * k_sphereCastRadiusScale),
                    m_collider.radius * k_sphereCastRadiusScale,
                    ~layersToIgnore
                );
                foreach (Collider c in obstructions)
                    if (c != m_collider) return;
            }

            // Modify capsule height
            m_collider.height = nextHeight;
        }
        m_collider.center = Vector3.up * m_collider.height * 0.5f;
    }

    private void HandleBufferedJump() {
        // Jump if buffering
        if (m_isBufferingJump && m_isGrounded)
            DoJump();
    }

    private bool UnderSlopeLimit(Vector3 normal) {
        // Remember that the dot product between a vector and a versor is
        // the length of the projection of the vector along the versor.
        // Since the up vector is along the Y axis, the following check
        // will suffice.
        return normal.y >= m_minGroundDotProduct;
    }

    private bool UnderSteepLimit(Vector3 normal)
    {
        // Remember that the dot product between a vector and a versor is
        // the length of the projection of the vector along the versor.
        // Since the up vector is along the Y axis, the following check
        // will suffice.
        return normal.y > k_minSteepDotProduct;
    }

    private Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal) {
        // planeNormal assumed to be already normalized
        return vector - planeNormal * Vector3.Dot(vector, planeNormal);
    }
}