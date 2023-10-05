/**
 * Luigi Rapetta (2022-2023)
 * Special thanks to:
 *      Toyful Game's "Making A Physics Based Character Controller In Unity (for Very Very Valet)" --- https://www.youtube.com/watch?v=qdskE8PJy6Q&list=PLq6yZbnlz9u3oQbLWwWOuUekpY2_5WbSM&index=11
 *      Unity Technologies's FPS Microgame
 *      Coberto's "How to climb stairs as a Rigidbody (in Unity3D)" --- https://cobertos.com/blog/post/how-to-climb-stairs-unity3d/
 *      Catlike Coding's movement tutorials --- https://catlikecoding.com/unity/tutorials/movement/
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour {
    // --- Public members
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
    public bool jumpAlongGroundNormal = true;

    [Header("Crouch")]
    [Range(0.5f, 1f)] public float crouchHeight = 0.75f;
    [Range(0f, 1f)] public float crouchSpeed = 0.5f;
    [Min(0f)] public float crouchTransitionSpeed = 10f;

    [Header("Thrust")]
    public bool enableThrust = true;
    public bool thrustCancelsVelocity = true;
    public bool airbornThrust = false;
    public bool capVelocityAtThrustEnd = false;
    public bool alignThrustDirectionToGround = false;
    [Min(0f)] public float thrustLength = 6f;
    [Min(0f)] public float thrustVerticalBias = 0.25f;
    [Min(0f)] public float noGravityTime = 0f;
    [Min(0f)] public float velocityCapAfterNoGravityTime = 5f;
    [Min(1)] public int maxThrusts = 1;
    [Min(0f)] public float thrustRechargeTime = 0.5f;

    [Header("Animation and Sound")]
    [Min(0f)] public float walkCycleFrequency = 1.43f;
    [Min(0f)] public float thrustAnimationAmount = 0.25f;
    [Min(0f)] public float landingAnimationAmount = 0.25f;
    [Min(0f)] public float landingShakeAnimationAmount = 0.125f;
    [Min(0f)] public float landingVelocityUpperLimit = 10f;

    [Header("Debug")]
    public bool printDebugInfo = false;

    // --- Public properties
    public bool isCrouching { get { return m_isCrouching; } }
    public Vector2 moveInput { get { return m_moveInput; } }
    public Vector2 lookInput { get { return m_lookInput; } }
    public Vector3 velocity { get { return m_rigidbody.velocity; } }

    // --- Private members
    // Player references
    private CapsuleCollider m_collider;
    private Rigidbody m_rigidbody;
    private PlayerCameraController m_cameraHolder;
    private PlayerWeaponInventoryController m_inventoryController;
    // Capsule height
    private float m_standingCapsuleHeight;
    private float m_targetCapsuleHeight;
    // Precomputed
    private float m_minGroundDotProduct;
    private float m_jumpMagnitude;
    private float m_thrustMagnitude;
    // Horizontal and vertical movement
    private bool m_isGrounded = false;
    private bool m_isSteeped = false;
    private bool m_isCrouching = false;
    private bool m_isBufferingJump = false;
    // NOTE: We need the following bool since thrustPhase does not necessarily reset while landing.
    //       Instead, isJumpingOrThrusting always resets when touching ground, and this allows for
    //       a more consistent handling of movement features (e.g.: coyote time).
    private bool m_isJumpingOrThrusting = false;
    private float m_coyoteTimeCounter = 0f;
    private float m_noGravityThrustCounter;
    private float m_thrustChargeCounter;
    private float m_walkCyclePeriod;
    private float m_walkCyclePeriodCounter = 0f;
    private float m_verticalRelativeVelocityOnLanding = 0f;
    private int m_jumpPhase = 0;
    private int m_thrustPhase = 0;
    private int m_stepsSinceLastGrounded = 0;
    private int m_stepsSinceLastJumpOrThrust = 0;
    private Rigidbody m_connectedBody;
    private Rigidbody m_previousConnectedBody;
    private List<ContactPoint> m_contactPoints = new List<ContactPoint>();
    private List<ContactPoint> m_contactPointsBuildingList = new List<ContactPoint>(); // Do NOT use this one! This is used for intermediate operations.
    private Vector3 m_groundNormal;
    private Vector3 m_steepNormal;
    private Vector3 m_connectionWorldPosition = Vector3.zero;
    private Vector3 m_connectionLocalPosition = Vector3.zero;
    private Vector3 m_connectionVelocity = Vector3.zero;
    // Player input
    private Vector2 m_lookInput = Vector2.zero;
    private Vector3 m_moveInput = Vector3.zero;
    // Object pools


    // --- Private constants
    private const float k_dotBias = 1.414214e-6f; // It mitigates floating point imprecision in UnderSlopeLimit
    private const float k_doublePi = 2f * Mathf.PI;
    private const float k_minSteepDotProduct = -0.01f;
    private const float k_nearZero = 0.0001f;
    private const float k_sphereCastRadiusScale = 0.99f;
    private const int k_groundStepsThreshold = 1;
    private const int k_jumpOrThrustStepsThreshold = 5;
    private const QueryTriggerInteraction k_triggerInteraction = QueryTriggerInteraction.Ignore;


    // --- MonoBehaviour methods
    void Awake() {
        // Retrieve references
        m_collider = GetComponent<CapsuleCollider>();
        m_rigidbody = GetComponent<Rigidbody>();
        m_cameraHolder = GetComponentInChildren<PlayerCameraController>();
        m_inventoryController = GetComponent<PlayerWeaponInventoryController>();

        // Setup state
        m_standingCapsuleHeight = m_collider.height;
        m_noGravityThrustCounter = noGravityTime;
        m_thrustChargeCounter = thrustRechargeTime;
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
        m_thrustMagnitude = Mathf.Sqrt(2f * customGravity * thrustLength);

        // Pre-calculate walk cycle period from frequency
        m_walkCyclePeriod = 1f / walkCycleFrequency;
    }

    void FixedUpdate() {
        // Forcely awake the agent
        if (m_rigidbody.IsSleeping()) m_rigidbody.WakeUp();

        // Keep movement coherent
        ContactCheck();
        UpdateThrustStatus();
        UpdateConnectionState();
        ApplyGravity();
        ApplyMovement();

        // Reset contact data
        ResetContactData();
    }

    void Update() {
        // Update casule height here to make camera movement smoother while crouching
        UpdateCapsuleHeight();

        // Jump if a jump has been buffered
        HandleBufferedJump();
    }

    void OnCollisionEnter(Collision col) {
        // Get landing vertical relative velocity
        m_verticalRelativeVelocityOnLanding = Mathf.Max(col.relativeVelocity.y, m_verticalRelativeVelocityOnLanding);

        // Add contacts to contact points' list
        col.GetContacts(m_contactPointsBuildingList);
        m_contactPoints.AddRange(m_contactPointsBuildingList);

        // Store connected body if below the player
        if (m_connectedBody == null)
            foreach (ContactPoint cp in m_contactPointsBuildingList)
                if (UnderSlopeLimit(cp.normal)) {
                    m_connectedBody = col.rigidbody;
                    break;
                }
    }

    void OnCollisionStay(Collision col) {
        // Get landing vertical relative velocity
        m_verticalRelativeVelocityOnLanding = Mathf.Max(col.relativeVelocity.y, m_verticalRelativeVelocityOnLanding);

        // Add contacts to contact points' list
        col.GetContacts(m_contactPointsBuildingList);
        m_contactPoints.AddRange(m_contactPointsBuildingList);

        // Store connected body if below the player
        if (m_connectedBody == null)
            foreach (ContactPoint cp in m_contactPointsBuildingList)
                if (UnderSlopeLimit(cp.normal)) {
                    m_connectedBody = col.rigidbody;
                    break;
                }
    }

    private void OnDrawGizmos() {
        if (m_rigidbody == null) return;

        // Draw target direction
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(
            transform.position,
            transform.position + (m_moveInput.x * transform.right + m_moveInput.y * transform.forward)
        );

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
        if (!printDebugInfo) return;

        string state = "isGrounded: " + m_isGrounded +
                       "\nisSteeped: " + m_isSteeped +
                       "\nconnectedBody: " + m_connectedBody +
                       "\nisBufferingJump: " + m_isBufferingJump +
                       "\nisJumpingOrThrusting: " + m_isJumpingOrThrusting +
                       "\njumpPhase: " + m_jumpPhase +
                       "\nthrustPhase: " + m_thrustPhase +
                       "\nthrustChargeCounter: " + m_thrustChargeCounter +
                       "\nnoGravityThrustCounter: " + m_noGravityThrustCounter;
        GUILayout.Label($"<color='black'><size=14>{state}</size></color>");
    }


    // --- PlayerController methods
    public void DoMove(Vector2 input) {
        // Just set move input
        m_moveInput = input;

        // If player has a weapon, handle movement sway
        if (m_inventoryController && m_inventoryController.activeWeapon)
            m_inventoryController.activeWeapon.weaponAnimationController.SetMovementTarget(input);
    }

    public void DoLook(Vector2 delta) {
        // Rotate camera
        m_cameraHolder.RotateCamera(delta);

        // Apply rotations
        m_rigidbody.MoveRotation(Quaternion.Euler(0f, m_cameraHolder.yaw, 0f));

        // If player has a weapon, handle view sway
        if (m_inventoryController && m_inventoryController.activeWeapon)
            m_inventoryController.activeWeapon.weaponAnimationController.SetViewSwayTarget(delta.x, Mathf.Abs(m_cameraHolder.pitch) < 90f ? delta.y : 0f);

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
        if (!m_isGrounded || m_rigidbody.velocity.y < 0f)
            m_rigidbody.velocity = new Vector3(m_rigidbody.velocity.x, 0f, m_rigidbody.velocity.z);

        // Abort zero-gravity thrust
        if (m_noGravityThrustCounter < noGravityTime)
            AbortNoGravityThrustCounter();

        // Apply jump
        m_rigidbody.AddForce(jumpDirection * m_jumpMagnitude, ForceMode.VelocityChange);

        // Update jump status
        m_jumpPhase++;
        m_stepsSinceLastJumpOrThrust = 0;
        m_isBufferingJump = false;
        m_isJumpingOrThrusting = true;

        // Trigger jump animation
        if (m_inventoryController && m_inventoryController.activeWeapon)
            m_inventoryController.activeWeapon.weaponAnimationController.TriggerJump(jumpDirection, m_cameraHolder.transform);
    }

    public void DoThrust() {
        // Abort if...
        if (!enableThrust || m_thrustPhase >= maxThrusts || !m_isGrounded && !airbornThrust) return;

        // Elaborate input
        Vector3 actualInput;
        if (m_moveInput.sqrMagnitude > k_nearZero)
            actualInput = new Vector3(m_moveInput.x, m_moveInput.y);
        else actualInput = Vector3.up;

        // Compute thrust direction
        Vector3 thrustDirection;
        bool resetStepsSinceThrust = true;
        if (m_moveInput.sqrMagnitude > k_nearZero)
            thrustDirection = (transform.right * m_moveInput.x + transform.forward * m_moveInput.y).normalized;
        else thrustDirection = transform.forward;

        // If requested, align thrust to ground plane
        if (alignThrustDirectionToGround) {
            thrustDirection = ProjectOnPlane(thrustDirection, m_groundNormal).normalized;
        }

        // Add vertical bias
        thrustDirection = (thrustDirection + transform.up * thrustVerticalBias).normalized;

        // Does it have to reset the counter of steps since last thrust?
        resetStepsSinceThrust = !m_isGrounded || thrustVerticalBias > 0f;

        // Apply thrust
        if (thrustCancelsVelocity) m_rigidbody.velocity = Vector3.zero;
        m_rigidbody.velocity += (thrustDirection * m_thrustMagnitude);
        // NOTE: Here I add the new velocity directly instead of using AddForce since the latter
        //       updates the velocity vector in the next physics update. For cancelling the thrust
        //       properly after a jump, I need this new velocity to be applied in the actual update
        //       where it's been computed. This whole thing is particularly useful when Thrust and
        //       Jump are performed on the very same update cycle.

        // Displace camera
        m_cameraHolder.PushCameraGlobal(thrustDirection * thrustAnimationAmount);

        // Trigger animation
        if (m_inventoryController && m_inventoryController.activeWeapon)
            m_inventoryController.activeWeapon.weaponAnimationController.TriggerThrust(thrustDirection, m_cameraHolder.transform);

        // Update thrust status and charge
        m_thrustPhase += 1;
        if (resetStepsSinceThrust)
            // Do NOT reset if thrusting along ground and there's no vertical bias
            // NOTE: In such case, the player should be considered still on ground for more reactive movement;
            //       you might want to do a jump to interrupt the thrust, for instance.
            m_stepsSinceLastJumpOrThrust = 0;
        m_isJumpingOrThrusting = true;
        m_noGravityThrustCounter = 0f;
        m_thrustChargeCounter = (maxThrusts - m_thrustPhase) / maxThrusts * thrustRechargeTime;
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
                    ~layersToIgnore,
                    k_triggerInteraction
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
        // Increment counters
        m_stepsSinceLastGrounded++;
        m_stepsSinceLastJumpOrThrust++;

        // Store previous contact state and reset it
        bool previousGrounded = m_isGrounded;
        m_isGrounded = false;
        m_isSteeped = false;

        // Return if the player just jumped
        if (m_stepsSinceLastJumpOrThrust < k_jumpOrThrustStepsThreshold) {
            m_groundNormal = Vector3.up;
            m_steepNormal = Vector3.up;
            return;
        }

        // Check if grounded or steeped, building the respective normals
        m_groundNormal = Vector3.zero;
        m_steepNormal = Vector3.zero;
        foreach (ContactPoint cp in m_contactPoints) {
            Vector3 normal = cp.normal;
            if (UnderSlopeLimit(normal)) {
                m_isGrounded = true;
                m_isJumpingOrThrusting = false;
                m_jumpPhase = 0;
                m_coyoteTimeCounter = 0f;
                m_stepsSinceLastGrounded = 0;
                m_groundNormal += normal;
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
        if (m_isGrounded)
            m_groundNormal.Normalize();
        else {
            // Try to snap on ground or ckeck a crevasse
            m_groundNormal = Vector3.up;  // Set a default value
            m_isGrounded = SnapToGround() || CheckCrevasse(); // If true, then m_groundNormal gets updated inisde either SnapToGround or CheckCrevasse
        }

        // If even snapping failed, check if within coyote time
        m_coyoteTimeCounter += Time.deltaTime;
        Vector3 currentHorizontalVelocity = new Vector3(m_rigidbody.velocity.x, 0f, m_rigidbody.velocity.z);
        float coyoteTimeMultipier = Mathf.Clamp01(-2f * currentHorizontalVelocity.magnitude / maxMovementSpeed + 3f);
        if (!m_isGrounded &&
            (!m_isSteeped || UnderSlopeLimit(m_steepNormal)) && // You don't want to allow jumps on too steep slopes...
            m_coyoteTimeCounter <= coyoteTime * coyoteTimeMultipier && // Multiplier prevents coyote jump when the player is too fast
            !m_isJumpingOrThrusting)
            m_isGrounded = true;

        // Is the player landing on ground?
        if (m_isGrounded && previousGrounded == false) {
            // Trigger camera landing
            float relativeAmount = Mathf.Clamp01(m_verticalRelativeVelocityOnLanding / landingVelocityUpperLimit);
            m_cameraHolder.PushCameraGlobal(landingAnimationAmount * relativeAmount * Vector3.down);
            m_cameraHolder.ShakeCamera(landingShakeAnimationAmount * relativeAmount);

            // Inform weapon animator about the ground state and whether the player has landed or not
            if (m_inventoryController && m_inventoryController.activeWeapon) {
                WeaponController weapon = m_inventoryController.activeWeapon;
                weapon.weaponAnimationController.TriggerLand(relativeAmount, m_cameraHolder.transform);
            }
        }
    }

    private bool SnapToGround() {
        // Return if the player didn't immediately loose contact, or when they just jumped
        // NOTE: The exact moment in which the player looses contact is when m_stepsSinceLastGrounded
        //       equals 1. It'll never be 0 inside this method.
        if (m_stepsSinceLastGrounded > k_groundStepsThreshold || m_stepsSinceLastJumpOrThrust < k_jumpOrThrustStepsThreshold)
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
            ~layersToIgnore,
            k_triggerInteraction
        )) return false;

        // Return if the hit normal is not below the slope limit
        if (!UnderSlopeLimit(sphereHitInfo.normal)) return false;

        // Return if the player is falling off a step or platform
        // NOTE: The hit normal of the sphere cast may come from the edge of this platform,
        //       which may be under the slope limit. This check is very similar to the one
        //       of step detection.
        Vector3 rayOrigin = new Vector3(transform.position.x, sphereHitInfo.point.y - k_nearZero, transform.position.z);
        Vector3 rayDistance = ProjectOnPlane(sphereHitInfo.point - transform.position, Vector3.up);
        if (Vector3.Dot(m_rigidbody.velocity, rayDistance) < 0f &&
            !Physics.Raycast(transform.position, Vector3.down, transform.position.y - sphereHitInfo.point.y + k_nearZero, ~layersToIgnore, k_triggerInteraction) && // The next raycast has to have nothing above its origin
            (!Physics.Raycast(rayOrigin, rayDistance.normalized, out RaycastHit rayHitInfo, rayDistance.magnitude, ~layersToIgnore, k_triggerInteraction) ||
             !UnderSlopeLimit(rayHitInfo.normal))) return false;

        // Snap to ground
        // NOTE: At this point, the player just lost contact to the ground and they're above it.
        m_groundNormal = sphereHitInfo.normal;
        m_connectedBody = sphereHitInfo.rigidbody;
        if (m_stepsSinceLastJumpOrThrust >= k_jumpOrThrustStepsThreshold)
            m_jumpPhase = 0;
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
            if (m_stepsSinceLastJumpOrThrust >= k_jumpOrThrustStepsThreshold)
                m_jumpPhase = 0;
            m_stepsSinceLastGrounded = 0;
            return true;
        }

        return false;
    }

    private void ApplyGravity() {
        if (m_noGravityThrustCounter >= noGravityTime) {
            // NOTE: Applying the gravity along the ground normal fixes the issue of slowly sliding
            //       down a slope while not providing any movement input.
            m_rigidbody.AddForce(-m_groundNormal * customGravity, ForceMode.Force);
        }
    }

    private void ApplyMovement() {
        // Compute horizontal velocity and update walk cycle
        Vector3 currentHorizontalVelocity = new Vector3(m_rigidbody.velocity.x, 0f, m_rigidbody.velocity.z); // Consider horizontal velocity in this case

        // Is the player actually moving on ground (and not thrusting)? If so, update walk cycle.
        bool isNoGravityThrusting = m_noGravityThrustCounter < noGravityTime;
        if (m_isGrounded && (Mathf.Abs(m_moveInput.x) > k_nearZero || Mathf.Abs(m_moveInput.y) > k_nearZero) && !isNoGravityThrusting) {
            Vector3 relativeHorizontalVelocity = currentHorizontalVelocity - m_connectionVelocity;
            relativeHorizontalVelocity.y = 0f;
            UpdateWalkCycle(relativeHorizontalVelocity.magnitude / maxMovementSpeed);
        } else ResetWalkCycle();

        // Abort if...
        if ((!m_isGrounded && m_moveInput.sqrMagnitude == 0) || isNoGravityThrusting) return;

        // Calculate target velocity
        float targetMagnitude = maxMovementSpeed * (m_isCrouching && m_isGrounded ? crouchSpeed : 1f);
        Vector3 targetDirection = transform.right * m_moveInput.x + transform.forward * m_moveInput.y;
        Vector3 targetVelocity = targetDirection * targetMagnitude;

        // Calculate dot product between the current horizontal velocity and the target
        float dot = Vector3.Dot(currentHorizontalVelocity.normalized, targetDirection);

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

            // Adjust target velocity if there's a body underneath
            if (m_connectedBody != null) {
                targetVelocity.x += m_connectionVelocity.x;
                targetVelocity.z += m_connectionVelocity.z;
            }
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

    private void UpdateWalkCycle(float relativeHorizontalVelocity) {
        // Compute target and inform weapon controller
        float sin = Mathf.Sin(m_walkCyclePeriodCounter / m_walkCyclePeriod * k_doublePi);
        if (m_inventoryController && m_inventoryController.activeWeapon)
            m_inventoryController.activeWeapon.weaponAnimationController.SetMovementBobTarget(sin);

        // Update counter
        m_walkCyclePeriodCounter += Time.deltaTime * relativeHorizontalVelocity;
        if (m_walkCyclePeriodCounter >= m_walkCyclePeriod)
            m_walkCyclePeriodCounter %= m_walkCyclePeriod;
    }

    private void UpdateThrustStatus() {
        // Increment thrust charge
        if (m_thrustChargeCounter < thrustRechargeTime) {
            m_thrustChargeCounter += Time.deltaTime;
            if (m_thrustChargeCounter >= thrustRechargeTime)
                m_thrustChargeCounter = thrustRechargeTime;
        }

        // Reset thrust phase if charge allows it
        if (m_isGrounded && m_thrustChargeCounter >= thrustRechargeTime)
            m_thrustPhase = 0;

        // Increment zero-G thrust counter
        if (m_noGravityThrustCounter < noGravityTime) {
            m_noGravityThrustCounter += Time.deltaTime;
            if (m_noGravityThrustCounter >= noGravityTime)
                AbortNoGravityThrustCounter();
        }
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
                    ~layersToIgnore,
                    k_triggerInteraction
                );
                foreach (Collider c in obstructions)
                    if (c != m_collider) return;
            }

            // Modify capsule height
            m_collider.height = nextHeight;
        }
        m_collider.center = Vector3.up * m_collider.height * 0.5f;
    }

    private void UpdateConnectionState() {
        if (m_connectedBody == null || m_connectedBody != m_previousConnectedBody || m_connectedBody.mass < m_rigidbody.mass)
            return;

        m_connectionVelocity = (m_connectedBody.transform.TransformPoint(m_connectionLocalPosition) - m_connectionWorldPosition) / Time.deltaTime;
        m_connectionWorldPosition = m_rigidbody.position;
        m_connectionLocalPosition = m_connectedBody.transform.InverseTransformPoint(m_connectionWorldPosition);
    }

    private void ResetContactData() {
        m_verticalRelativeVelocityOnLanding = 0f;
        m_contactPoints.Clear();
        m_previousConnectedBody = m_connectedBody;
        m_connectedBody = null;
        m_connectionVelocity = Vector3.zero;
    }

    private void ResetWalkCycle() {
        // Reset counter and reset target
        m_walkCyclePeriodCounter = 0f;
        if (m_inventoryController && m_inventoryController.activeWeapon)
            m_inventoryController.activeWeapon.weaponAnimationController.ResetMovementBobTarget();
    }

    private void HandleBufferedJump() {
        // Jump if buffering
        if (m_isBufferingJump && m_isGrounded)
            DoJump();
    }

    private void AbortNoGravityThrustCounter() {
        m_noGravityThrustCounter = noGravityTime;
        if (capVelocityAtThrustEnd)
            m_rigidbody.velocity = Vector3.ClampMagnitude(m_rigidbody.velocity, velocityCapAfterNoGravityTime);
    }

    private bool UnderSlopeLimit(Vector3 normal) {
        // Remember that the dot product between a vector and a versor is
        // the length of the projection of the vector along the versor.
        // Since the up vector is along the Y axis, the following check
        // will suffice.
        return normal.y >= m_minGroundDotProduct;
    }

    private bool UnderSteepLimit(Vector3 normal) {
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