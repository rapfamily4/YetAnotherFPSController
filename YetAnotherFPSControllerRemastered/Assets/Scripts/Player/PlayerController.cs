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

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour {
    // --- Public members (!!! PLEASE, REMEMBER TO PUT TOOLTIPS !!!)
    [Header("Camera")]
    [Range(0f, 1f)] public float cameraHeight = 0.85f;

    [Header("Movement")]
    [Min(0f)] public float maxMovementSpeed = 5f;
    [Min(0f)] public float acceleration = 30f;
    [Min(0f)] public float maxSharpTurnMultiplier = 2f;
    [Range(0f, 1f)] public float airControl = 0.25f;
    [Range(0f, 90f)] public float maxGroundAngle = 45f;

    [Header("Ground Snap")]
    [Min(0f)] public float maxSnapSpeed = 7.5f;
    [Min(0f)] public float sweepDistance = 0.5f;

    [Header("Collision Detection")]
    public LayerMask layersToIgnore;

    [Header("Gravity")]
    [Min(0f)] public float customGravity = 9.81f;

    [Header("Jump")]
    [Min(0f)] public float jumpHeight = 2f;
    [Min(0f)] public float maxAirJumps = 0;
    public bool enableWallJump = true;
    public bool jumpCancelsVerticalVelocity = true;
    public bool jumpAlongGroundNormal = true;

    [Header("Crouch")]
    [Range(0.5f, 1f)] public float crouchHeight = 0.75f;
    [Range(0f, 1f)] public float crouchSpeed = 0.5f;
    [Min(0f)] public float crouchTransitionSpeed = 10f;
    public bool toggleCrouch = false;

    [Header("Thrust")]
    [Min(0f)] public float thrustHeight = 6f;
    [Range(0f, 1f)] public float thrustVerticalBias = 0.25f;

    // --- Private members
    private PlayerInputHandler m_inputHandler;
    private CapsuleCollider m_collider;
    private Rigidbody m_rigidbody;
    private Camera m_camera;
    private float m_cameraPitch;
    private float m_cameraYaw;
    private float m_standingCapsuleHeight;
    private float m_targetCapsuleHeight;
    private float m_minGroundDotProduct;
    private float m_jumpMagnitude;
    private float m_thrustMagnitude;
    private int m_stepsSinceLastGrounded;
    private int m_stepsSinceLastJump;
    private int m_jumpPhase;
    private bool m_isGrounded = false;
    private bool m_isSteeped= false;
    private bool m_isCrouching = false;
    private List<ContactPoint> m_contactPoints;
    private Vector3 m_groundNormal;
    private Vector3 m_steepNormal;

    // --- Private constants
    private const float k_sphereCastRadiusScale = 0.99f;
    private const float k_dotBias = 1.414214e-6f; // It mitigates floating point imprecision in UnderSlopeLimit
    private const float k_positionBias = 0.0001f; // Useful for defining the immediate surrounding of a point.
    private const float k_minSteepDotProduct = -0.01f;


    // --- MonoBehaviour methods
    void Awake() {
        // Retrieve references
        m_inputHandler = GetComponent<PlayerInputHandler>();
        m_collider = GetComponent<CapsuleCollider>();
        m_rigidbody = GetComponent<Rigidbody>();
        m_camera = GetComponentInChildren<Camera>();
        
        // Setup state
        m_contactPoints = new List<ContactPoint>();
        m_standingCapsuleHeight = m_collider.height;
        m_stepsSinceLastGrounded = 0;
        m_stepsSinceLastJump = 0;
        m_jumpPhase = 0;
        OnValidate();

        // Freeze rigidbody's rotation and disable implicit gravity
        m_rigidbody.freezeRotation = true;
        m_rigidbody.useGravity = false;

        // Force player's height and crouch state
        SetCrouchState(false, true);
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

        // Check for ground
        ContactCheck();

        // Handle frame independent movement
        HandleGravity();
        HandleMove();

        // Reset contact points' list
        m_contactPoints.Clear();
    }

    void Update() {
        // Handle input-sensible movement
        HandleLook();
        HandleJump();
        HandleCrouch();
        HandleThrust();
    }

    void LateUpdate() {
        // Update camera's transform here
        m_camera.transform.position = transform.position + transform.up * m_collider.height * cameraHeight;
        m_camera.transform.eulerAngles = Vector3.up * m_cameraYaw + Vector3.right * m_cameraPitch;
    }

    void OnCollisionEnter(Collision col) {
        // Add contacts to contact points' list
        m_contactPoints.AddRange(col.contacts);
    }

    void OnCollisionStay(Collision col) {
        // Add contacts to contact points' list
        m_contactPoints.AddRange(col.contacts);
    }

    private void OnDrawGizmos() {
        if (m_rigidbody == null) return;

        // Draw target direction
        Gizmos.color = Color.blue;
        Vector3 move3D = (transform.right * m_inputHandler.moveInput.x + transform.forward * m_inputHandler.moveInput.y).normalized;
        Gizmos.DrawLine(transform.position, transform.position + move3D);

        // Draw velocity
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + m_rigidbody.velocity);

        // Draw contact points
        Gizmos.color = Color.red;
        foreach (ContactPoint cp in m_contactPoints) {
            Gizmos.DrawSphere(cp.point, 0.05f);
            Gizmos.DrawLine(cp.point, cp.point + cp.normal * 0.5f);
        }
    }

    void OnGUI() {
        string state = "isGrounded: " + m_isGrounded + "\nisSteeped: " + m_isSteeped + "\njumpPhase: " + m_jumpPhase;
        GUILayout.Label($"<color='black'><size=14>{state}</size></color>");
    }


    // --- PlayerController methods
    private void ContactCheck() {
        // Increment step counters
        m_stepsSinceLastGrounded++;
        m_stepsSinceLastJump++;
        
        // Reset contact state
        m_isGrounded = false;
        m_isSteeped = false;
        m_groundNormal = Vector3.zero;
        m_steepNormal = Vector3.zero;

        // Check if grounded or steeped, building the respective normals
        foreach (ContactPoint cp in m_contactPoints) {
            Vector3 normal = cp.normal;
            if (UnderSlopeLimit(normal)) {
                m_isGrounded = true;
                if (m_stepsSinceLastJump > 5) m_jumpPhase = 0;
                m_stepsSinceLastGrounded = 0;
                m_groundNormal += normal;
            }
            if (UnderSteepLimit(normal)) {
                m_isSteeped = true;
                m_steepNormal += normal;
            }
        }

        if (m_isSteeped) m_steepNormal.Normalize();
        else m_steepNormal = Vector3.up; // Set a default value

        if (m_isGrounded) m_groundNormal.Normalize();
        else {
            // Try to snap on ground or ckeck a crevasse
            m_groundNormal = Vector3.up;  // Set a default value
            m_isGrounded = SnapToGround() || CheckCrevasse(); // If true, then m_groundNormal gets updated inisde either SnapToGround or CheckCrevasse
        }
    }

    private bool SnapToGround() {
        // Return if the player didn't immediately loose contact, or when they just jumped
        // NOTE: The exact moment in which the player looses contact is when m_stepsSinceLastGrounded
        //       equals 1. It'll never be 0 inside this method.
        if (m_stepsSinceLastGrounded > 1 || m_stepsSinceLastJump < 5)
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
        Vector3 rayOrigin = new Vector3(transform.position.x, sphereHitInfo.point.y - k_positionBias, transform.position.z);
        Vector3 rayDistance = ProjectOnPlane(sphereHitInfo.point - transform.position, Vector3.up);
        if (Vector3.Dot(m_rigidbody.velocity, rayDistance) < 0f &&
            !Physics.Raycast(transform.position, Vector3.down, transform.position.y - sphereHitInfo.point.y + k_positionBias, ~layersToIgnore) && // The next raycast has to have nothing above its origin
            (!Physics.Raycast(rayOrigin, rayDistance.normalized, out RaycastHit rayHitInfo, rayDistance.magnitude, ~layersToIgnore) ||
             !UnderSlopeLimit(rayHitInfo.normal))) return false;

        // Snap to ground
        // NOTE: At this point, the player just lost contact to the ground and they're above it.
        m_groundNormal = sphereHitInfo.normal;
        if (m_stepsSinceLastJump > 5) m_jumpPhase = 0;
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
            if (m_stepsSinceLastJump > 5) m_jumpPhase = 0;
            m_stepsSinceLastGrounded = 0;
            return true;
        }

        return false;
    }

    private void HandleGravity() {
        // NOTE: Applying the gravity along the ground normal fixes the issue of slowly sliding
        //       down a slope while not providing any movement input.
        m_rigidbody.AddForce(-m_groundNormal * customGravity, ForceMode.Force);
    }

    private void HandleMove() {
        // Retrieve input 
        Vector2 input = m_inputHandler.moveInput;

        // Abort if...
        if (!m_isGrounded && input.sqrMagnitude == 0) return;

        // Calculate target velocity
        float targetMagnitude = maxMovementSpeed * (m_isCrouching && m_isGrounded ? crouchSpeed : 1f);
        Vector3 targetDirection = (transform.right * input.x + transform.forward * input.y).normalized;
        Vector3 targetVelocity = targetDirection * targetMagnitude;

        // Calculate dot product between the current horizontal velocity and the target
        Vector3 currentVelocity = new Vector3(m_rigidbody.velocity.x, 0f, m_rigidbody.velocity.z); // Consider horizontal velocity in this case
        Vector3 currentDirection = currentVelocity.normalized;
        float dot = Vector3.Dot(currentDirection, targetDirection);

        // Calculate max acceleration
        float sharpTurnMultiplier = (dot >= 0) ? 1f : Mathf.Lerp(1f, maxSharpTurnMultiplier, -dot);
        float maxAcceleration = acceleration * (m_isGrounded ? 1f : airControl) * sharpTurnMultiplier;

        // Process target velocity
        if (m_isGrounded) {
            // Use velocities parallel to the contact plane (slope handling)
            // NOTE: This is crucial for getting a correct velocity difference (dv).
            targetVelocity = targetMagnitude * ProjectOnPlane(targetVelocity, m_groundNormal).normalized;
            currentVelocity = ProjectOnPlane(m_rigidbody.velocity, m_groundNormal);
        } else {
            // Modify target velocity while airborne in order to keep horizontal speed
            float currentMagnitude = currentVelocity.magnitude;
            if (currentMagnitude > maxMovementSpeed)
                targetVelocity = Vector3.ClampMagnitude(currentVelocity * Mathf.Clamp01(dot) + targetVelocity, currentMagnitude);
        }

        // Calculate acceleration to apply
        Vector3 accelerationToApply = (targetVelocity - currentVelocity) / Time.deltaTime; // (dv / dt) = a
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

    private void HandleLook() {
        // Retrieve input
        Vector2 delta = m_inputHandler.lookDelta;

        // Update camera angles
        m_cameraPitch += delta.y;
        m_cameraPitch = Mathf.Clamp(m_cameraPitch, -90f, 90f);
        m_cameraYaw += delta.x;
        m_cameraYaw %= 360f;

        // Apply rotations
        m_rigidbody.MoveRotation(Quaternion.Euler(0f, m_cameraYaw, 0f));
    }

    private void HandleJump() {
        // Detect jump input
        if (!m_inputHandler.jumpStatus.started) return;
        
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

        // Update jump state
        m_jumpPhase++;
        m_stepsSinceLastJump = 0;
    }

    private void HandleCrouch() {
        if (toggleCrouch && m_inputHandler.crouchStatus.started) SetCrouchState(!m_isCrouching);
        else if (!toggleCrouch) SetCrouchState(m_inputHandler.crouchStatus.pressed);
        UpdateCapsuleHeight();
    }

    private void HandleThrust() {
        if (!m_inputHandler.thrustStatus.started) return;

        // Retrieve movement input
        Vector2 input = m_inputHandler.moveInput;

        // Compute thrust direction
        Vector3 thrustDirection = (m_camera.transform.forward + transform.right * input.x).normalized;
        float dot = Mathf.Clamp01(Vector3.Dot(transform.forward, m_camera.transform.forward));
        thrustDirection = (thrustDirection + (transform.up * thrustVerticalBias * dot)).normalized;

        // Apply thrust
        m_rigidbody.velocity = Vector3.zero;
        m_rigidbody.AddForce(thrustDirection * m_thrustMagnitude, ForceMode.VelocityChange);

        // Reset counters
        m_stepsSinceLastJump = 0;
    }

    private bool SetCrouchState(bool crouched, bool ignoreObstructions = false) {
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