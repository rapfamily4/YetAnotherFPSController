using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WeaponController : MonoBehaviour {
    // --- Public members
    public WeaponAnimationSettings animationSettings;

    // --- Private members
    // Animation
    private Animator m_animator;
    private int m_isGroundedID;
    private int m_relativeHorizontalVelocityID;
    private int m_jumpID;
    private int m_landID;
    private Vector2 m_moveInput;
    private Vector3 m_originalPosition;
    private Vector3 m_newViewSwayRotation;
    private Vector3 m_newViewSwayRotationVelocity;
    private Vector3 m_targetViewSwayRotation;
    private Vector3 m_targetViewSwayRotationVelocity;
    private Vector3 m_newMovementSwayRotation;
    private Vector3 m_newMovementSwayRotationVelocity;
    private Vector3 m_targetMovementSwayRotation;
    private Vector3 m_targetMovementSwayRotationVelocity;
    private Vector3 m_newMovementPanTranslation;
    private Vector3 m_newMovementPanTranslationVelocity;
    private Vector3 m_targetMovementPanTranslation;
    private Vector3 m_targetMovementPanTranslationVelocity;


    // --- MonoBehaviour methods
    private void Awake() {
        // Retrieve references
        m_animator = GetComponentInChildren<Animator>();

        // Fetch the IDs of the animator's parameters
        m_isGroundedID = Animator.StringToHash(Constants.ANIMPARAM_ISGROUNDED);
        m_relativeHorizontalVelocityID = Animator.StringToHash(Constants.ANIMPARAM_RELATIVEHORIZONTALVELOCITY);
        m_jumpID = Animator.StringToHash(Constants.ANIMPARAM_JUMP);
        m_landID = Animator.StringToHash(Constants.ANIMPARAM_LAND);

        // Store original position
        m_originalPosition = transform.localPosition;

        // Initialize rotations and position
        m_newViewSwayRotation = m_newMovementSwayRotation = transform.localRotation.eulerAngles;
        m_newMovementPanTranslation = m_originalPosition;
    }

    private void Update() {
        // Weapon sway and pan
        ApplyWeaponSwayAndPan();
    }

    // --- WeaponController methods
    public void SetViewSwayTarget(float deltaX, float deltaY) {
        m_targetViewSwayRotation.x += animationSettings.viewSwayAmount.y * (animationSettings.invertViewSwayY ? deltaY : -deltaY) * Time.deltaTime;
        m_targetViewSwayRotation.y += animationSettings.viewSwayAmount.x * (animationSettings.invertViewSwayX ? -deltaX : deltaX) * Time.deltaTime;
        m_targetViewSwayRotation.z += animationSettings.viewSwayAmount.z * (animationSettings.invertViewSwayZ ? deltaX : -deltaX) * Time.deltaTime;
        m_targetViewSwayRotation.x = Mathf.Clamp(m_targetViewSwayRotation.x, -animationSettings.viewSwayClamp.x, animationSettings.viewSwayClamp.x);
        m_targetViewSwayRotation.y = Mathf.Clamp(m_targetViewSwayRotation.y, -animationSettings.viewSwayClamp.y, animationSettings.viewSwayClamp.y);
        m_targetViewSwayRotation.z = Mathf.Clamp(m_targetViewSwayRotation.z, -animationSettings.viewSwayClamp.z, animationSettings.viewSwayClamp.z);
    }

    public void SetMovementTarget(Vector2 moveInput) {
        // Store player's movement input
        // NOTE: Since PlayerController executes DoMove only once the move input *changes*, and since it wouldn't
        //       be nice to set m_targetMovementSwayRotation in each PlayerController's Update, the new movement
        //       is stored here and then used to influence m_targetMovementSwayRotation.
        m_moveInput = moveInput;
    }

    public void SetGrounded(bool grounded) {
        // Inform the animator that the player is grounded
        m_animator.SetBool(m_isGroundedID, grounded);
    }

    public void SetRelativeHorizontalVelocity(float relativeHorizontalVelocity) {
        // Inform the animator player's horizontal velocity relative to their max movement speed
        m_animator.SetFloat(m_relativeHorizontalVelocityID, relativeHorizontalVelocity);
    }

    public void TriggerJump() {
        // Trigger jump animation
        m_animator.ResetTrigger(m_landID);
        m_animator.SetTrigger(m_jumpID);
    }

    public void TriggerLand() {
        // Trigger land animation
        m_animator.ResetTrigger(m_jumpID);
        m_animator.SetTrigger(m_landID);
    }

    private void ApplyWeaponSwayAndPan() {
        // View sway
        m_targetViewSwayRotation = Vector3.SmoothDamp(m_targetViewSwayRotation, Vector3.zero, ref m_targetViewSwayRotationVelocity, animationSettings.viewSwayResetSmoothing);
        m_newViewSwayRotation = Vector3.SmoothDamp(m_newViewSwayRotation, m_targetViewSwayRotation, ref m_newViewSwayRotationVelocity, animationSettings.viewSwaySmoothing);

        // Movement sway
        // NOTE: The target is always set here, since PlayerController executes DoMove only once the move input *changes*
        m_targetMovementSwayRotation.z = animationSettings.movementSwayAmount.x * (animationSettings.invertMovementSwayX ? -m_moveInput.x : m_moveInput.x);
        m_targetMovementSwayRotation.x = animationSettings.movementSwayAmount.y * (animationSettings.invertMovementSwayY ? -m_moveInput.y : m_moveInput.y);
        m_targetMovementSwayRotation = Vector3.SmoothDamp(m_targetMovementSwayRotation, Vector3.zero, ref m_targetMovementSwayRotationVelocity, animationSettings.movementSwayResetSmoothing);
        m_newMovementSwayRotation = Vector3.SmoothDamp(m_newMovementSwayRotation, m_targetMovementSwayRotation, ref m_newMovementSwayRotationVelocity, animationSettings.movementSwaySmoothing);

        // Movement panning
        m_targetMovementPanTranslation.x = m_originalPosition.x + animationSettings.movementPanAmount.x * (animationSettings.invertMovementPanX ? -m_moveInput.x : m_moveInput.x);
        m_targetMovementPanTranslation.z = m_originalPosition.z + animationSettings.movementPanAmount.y * (animationSettings.invertMovementPanY ? -m_moveInput.y : m_moveInput.y);
        m_targetMovementPanTranslation = Vector3.SmoothDamp(m_targetMovementPanTranslation, m_originalPosition, ref m_targetMovementPanTranslationVelocity, animationSettings.movementPanResetSmoothing);
        m_newMovementPanTranslation = Vector3.SmoothDamp(m_newMovementPanTranslation, m_targetMovementPanTranslation, ref m_newMovementPanTranslationVelocity, animationSettings.movementPanSmoothing);

        // Apply sway and pan
        transform.localRotation = Quaternion.Euler(m_newViewSwayRotation + m_newMovementSwayRotation);
        transform.localPosition = m_newMovementPanTranslation;
    }
}
