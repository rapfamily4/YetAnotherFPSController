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
    public LayerMask layersToIgnore;

    [Header("Crouch")]
    [Range(0.5f, 1f)] public float crouchHeight = 0.75f;
    [Range(0f, 1f)] public float crouchSpeed = 0.5f;
    [Min(0f)] public float crouchTransitionSpeed = 10f;
    public bool toggleCrouch = false;

    [Header("Jump")]
    [Min(0f)] public float jumpHeight = 2f;
    [Min(0f)] public float maxAirJumps = 0;
    public bool jumpCancelsVerticalVelocity = true;
    public bool jumpAlongGroundNormal = true;

    [Header("Thrust")]
    [Min(0f)] public float thrustHeight = 6f;
    [Range(0f, 1f)] public float thrustUpImpulseFactor = 1f;
    
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
    private int m_stepsSinceLastGrounded;
    private int m_stepsSinceLastJump;
    private int m_jumpPhase;
    private bool m_isGrounded = false;
    private bool m_isCrouching = false;
    private List<ContactPoint> m_contactPoints;
    private Vector3 m_contactNormal;

    // --- Private constants
    private const float k_sphereCastRadiusScale = 0.99f;
    private const float k_dotBias = 1.414214e-6f; // It mitigates floating point imprecision in UnderSlopeLimit


    // --- MonoBehaviour methods
    void Awake() {
        // Retrieve references
        m_inputHandler = GetComponent<PlayerInputHandler>();
        m_collider = GetComponent<CapsuleCollider>();
        m_rigidbody = GetComponent<Rigidbody>();
        m_camera = GetComponentInChildren<Camera>();

        // Setup state
        m_contactPoints = new List<ContactPoint>();
        m_stepsSinceLastGrounded = 0;
        m_stepsSinceLastJump = 0;
        m_jumpPhase = 0;
        OnValidate();

        // Freeze Rigidbody's rotation
        m_rigidbody.freezeRotation = true;

        // Force player's height and crouch state
        //SetCrouchState(false, true);
        //UpdateCapsuleHeight(true);
    }

    void OnValidate() {
        // Validate minimum dot product for contact normals
        m_minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        // Make the bias weighted with the cosine of the angle
        m_minGroundDotProduct -= k_dotBias * m_minGroundDotProduct;
    }

    void FixedUpdate() {
        // Forcely awake the agent
        if (m_rigidbody.IsSleeping()) m_rigidbody.WakeUp();

        // Check for ground
        ContactCheck();

        // Handle frame independent movement
        HandleMove();

        // Reset contact points' list
        m_contactPoints.Clear();
    }

    void Update() {
        // Handle input-sensible movement
        HandleLook();
        HandleJump();
        //HandleThrust();
        //HandleCrouch();
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

        // Draw move input
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


    // --- PlayerController methods
    private void ContactCheck() {
        // Increment step counters
        m_stepsSinceLastGrounded++;
        m_stepsSinceLastJump++;
        
        // Reset contact state
        m_isGrounded = false;
        m_contactNormal = Vector3.zero;

        // Check if grounded
        foreach (ContactPoint cp in m_contactPoints) {
            Vector3 normal = cp.normal;
            if (UnderSlopeLimit(normal)) {
                m_isGrounded = true;
                m_jumpPhase = 0;
                m_stepsSinceLastGrounded = 0;
                m_contactNormal += normal;
            }
        }

        if (m_isGrounded) m_contactNormal.Normalize();
        else {
            // Try to snap on ground
            m_contactNormal = Vector3.up;  // Set a default value
            m_isGrounded = SnapToGround(); // If true, then m_contactNormal gets updated inisde SnapToGround
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
            out RaycastHit hitInfo,
            sweepDistance,
            ~layersToIgnore
        )) return false;

        // Return if the hit normal is not below the slope limit
        if (!UnderSlopeLimit(hitInfo.normal)) return false;

        // Snap to ground
        // NOTE: At this point, the player just lost contact to the ground and they're above it.
        m_contactNormal = hitInfo.normal;
        m_stepsSinceLastGrounded = 0;
        float dot = Vector3.Dot(m_rigidbody.velocity, hitInfo.normal);
        if (dot > 0f)
            // NOTE: Rotate onto plane only if the velocity aims along the direction of the hit
            //       normal (dot > 0). Velocity aiming behind the direction of the hit normal is
            //       already useful for realigning the player to the ground: rotating it along
            //       the plane would make this advantage lost.
            m_rigidbody.velocity = (m_rigidbody.velocity - hitInfo.normal * dot).normalized * speed;
        return true;
    }

    private void HandleMove() {
        // Retrieve input 
        Vector2 input = m_inputHandler.moveInput;

        // Abort if...
        // *** remember the onSteep handling
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
            targetVelocity = targetMagnitude * ProjectOnContactPlane(targetVelocity).normalized;
            currentVelocity = ProjectOnContactPlane(m_rigidbody.velocity);
        } else {
            // Modify target velocity while airborne in order to keep horizontal speed
            float currentMagnitude = currentVelocity.magnitude;
            if (currentMagnitude > maxMovementSpeed)
                targetVelocity = Vector3.ClampMagnitude(currentVelocity * Mathf.Clamp01(dot) + targetVelocity, currentMagnitude);
        }

        // Calculate acceleration to apply
        Vector3 accelerationToApply = (targetVelocity - currentVelocity) / Time.deltaTime; // (dv / dt) = a
        accelerationToApply = Vector3.ClampMagnitude(accelerationToApply, maxAcceleration);

        // Apply force
        m_rigidbody.AddForce(accelerationToApply, ForceMode.Force);
    }

    private void HandleJump() {
        if (!m_inputHandler.jumpStatus.started || !m_isGrounded && m_jumpPhase >= maxAirJumps) return;
        
        // Cancel vertical velocity if needed
        if (jumpCancelsVerticalVelocity || m_rigidbody.velocity.y < 0f)
            m_rigidbody.velocity = new Vector3(m_rigidbody.velocity.x, 0f, m_rigidbody.velocity.z);
        
        // Apply jump
        float magnitude = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
        Vector3 jumpDirection = jumpAlongGroundNormal ? m_contactNormal : Vector3.up;
        m_rigidbody.AddForce(jumpDirection * magnitude, ForceMode.VelocityChange);

        // Update jump state
        m_jumpPhase++;
        m_stepsSinceLastJump = 0;
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

    private bool UnderSlopeLimit(Vector3 normal) {
        // Remember that the dot product between a vector and a versor is
        // the length of the projection of the vector along the versor.
        // Since the up vector is along the Y axis, the following check
        // will suffice.
        return normal.y >= m_minGroundDotProduct;
    }

    private Vector3 ProjectOnContactPlane(Vector3 vector) {
        return vector - m_contactNormal * Vector3.Dot(vector, m_contactNormal);
    }

}
