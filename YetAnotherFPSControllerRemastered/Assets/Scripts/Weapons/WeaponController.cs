using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour {
    // --- Public members
    [Header("View Sway")]
    [Min(0f)] public Vector3 viewSwayAmount = Vector3.one * 1f;
    [Min(0f)] public Vector3 viewSwayClamp = Vector3.one * 2f;
    [Min(0f)] public float viewSwaySmoothing = 0.1f;
    [Min(0f)] public float viewSwayResetSmoothing = 0.1f;
    public bool invertViewSwayX = false;
    public bool invertViewSwayY = false;
    public bool invertViewSwayZ = false;

    [Header("Movement Sway")]
    [Min(0f)] public Vector2 movementSwayAmount = Vector2.one * 2f;
    [Min(0f)] public float movementSwaySmoothing = 0.1f;
    [Min(0f)] public float movementSwayResetSmoothing = 0.1f;
    public bool invertMovementSwayX = true;
    public bool invertMovementSwayY = false;

    [Header("Movement Pan")]
    [Min(0f)] public Vector2 movementPanAmount = Vector2.one * 0.05f;
    [Min(0f)] public float movementPanSmoothing = 0.1f;
    [Min(0f)] public float movementPanResetSmoothing = 0.1f;
    public bool invertMovementPanX = true;
    public bool invertMovementPanY = true;

    // --- Private members
    Animator m_animator;
    int m_isGroundedID;
    int m_relativeHorizontalVelocityID;
    int m_jumpID;
    int m_landID;
    Vector2 m_moveInput;
    Vector3 m_originalPosition;
    Vector3 m_newViewSwayRotation;
    Vector3 m_newViewSwayRotationVelocity;
    Vector3 m_targetViewSwayRotation;
    Vector3 m_targetViewSwayRotationVelocity;
    Vector3 m_newMovementSwayRotation;
    Vector3 m_newMovementSwayRotationVelocity;
    Vector3 m_targetMovementSwayRotation;
    Vector3 m_targetMovementSwayRotationVelocity;
    Vector3 m_newMovementPanTranslation;
    Vector3 m_newMovementPanTranslationVelocity;
    Vector3 m_targetMovementPanTranslation;
    Vector3 m_targetMovementPanTranslationVelocity;


    // --- MonoBehaviour methods
    private void Awake() {
        // Retrieve references
        m_animator = GetComponentInChildren<Animator>();

        // Fetch the IDs of the animator's parameters
        m_isGroundedID = Animator.StringToHash(ConstantManager.ANIMPARAM_ISGROUNDED);
        m_relativeHorizontalVelocityID = Animator.StringToHash(ConstantManager.ANIMPARAM_RELATIVEHORIZONTALVELOCITY);
        m_jumpID = Animator.StringToHash(ConstantManager.ANIMPARAM_JUMP);
        m_landID = Animator.StringToHash(ConstantManager.ANIMPARAM_LAND);

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
        m_targetViewSwayRotation.x += viewSwayAmount.y * (invertViewSwayY ? deltaY : -deltaY) * Time.deltaTime;
        m_targetViewSwayRotation.y += viewSwayAmount.x * (invertViewSwayX ? -deltaX : deltaX) * Time.deltaTime;
        m_targetViewSwayRotation.z += viewSwayAmount.z * (invertViewSwayZ ? deltaX : -deltaX) * Time.deltaTime;
        m_targetViewSwayRotation.x = Mathf.Clamp(m_targetViewSwayRotation.x, -viewSwayClamp.x, viewSwayClamp.x);
        m_targetViewSwayRotation.y = Mathf.Clamp(m_targetViewSwayRotation.y, -viewSwayClamp.y, viewSwayClamp.y);
        m_targetViewSwayRotation.z = Mathf.Clamp(m_targetViewSwayRotation.z, -viewSwayClamp.z, viewSwayClamp.z);
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
        m_targetViewSwayRotation = Vector3.SmoothDamp(m_targetViewSwayRotation, Vector3.zero, ref m_targetViewSwayRotationVelocity, viewSwayResetSmoothing);
        m_newViewSwayRotation = Vector3.SmoothDamp(m_newViewSwayRotation, m_targetViewSwayRotation, ref m_newViewSwayRotationVelocity, viewSwaySmoothing);

        // Movement sway
        // NOTE: The target is always set here, since PlayerController executes DoMove only once the move input *changes*
        m_targetMovementSwayRotation.z = movementSwayAmount.x * (invertMovementSwayX ? -m_moveInput.x : m_moveInput.x);
        m_targetMovementSwayRotation.x = movementSwayAmount.y * (invertMovementSwayY ? -m_moveInput.y : m_moveInput.y);
        m_targetMovementSwayRotation = Vector3.SmoothDamp(m_targetMovementSwayRotation, Vector3.zero, ref m_targetMovementSwayRotationVelocity, movementSwayResetSmoothing);
        m_newMovementSwayRotation = Vector3.SmoothDamp(m_newMovementSwayRotation, m_targetMovementSwayRotation, ref m_newMovementSwayRotationVelocity, movementSwaySmoothing);

        // Movement panning
        m_targetMovementPanTranslation.x = m_originalPosition.x + movementPanAmount.x * (invertMovementPanX ? -m_moveInput.x : m_moveInput.x);
        m_targetMovementPanTranslation.z = m_originalPosition.z + movementPanAmount.y * (invertMovementPanY ? -m_moveInput.y : m_moveInput.y);
        m_targetMovementPanTranslation = Vector3.SmoothDamp(m_targetMovementPanTranslation, m_originalPosition, ref m_targetMovementPanTranslationVelocity, movementPanResetSmoothing);
        m_newMovementPanTranslation = Vector3.SmoothDamp(m_newMovementPanTranslation, m_targetMovementPanTranslation, ref m_newMovementPanTranslationVelocity, movementPanSmoothing);

        // Apply sway and pan
        transform.localRotation = Quaternion.Euler(m_newViewSwayRotation + m_newMovementSwayRotation);
        transform.localPosition = m_newMovementPanTranslation;
    }
}
