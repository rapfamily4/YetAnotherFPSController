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
    public bool invertMovementSwayX = false;
    public bool invertMovementSwayY = false;


    // --- Hidden public members
    [HideInInspector] public PlayerController playerController;

    // --- Private members
    Vector3 m_newViewSwayRotation;
    Vector3 m_newViewSwayRotationVelocity;
    Vector3 m_targetViewSwayRotation;
    Vector3 m_targetViewSwayRotationVelocity;
    Vector3 m_newMovementSwayRotation;
    Vector3 m_newMovementSwayRotationVelocity;
    Vector3 m_targetMovementSwayRotation;
    Vector3 m_targetMovementSwayRotationVelocity;


    // --- MonoBehaviour methods
    private void Start() {
        // Initialize rotations
        m_newViewSwayRotation = transform.localRotation.eulerAngles;
        m_newMovementSwayRotation = transform.localRotation.eulerAngles;
    }

    private void Update() {
        // Do nothing if there's no player reference
        if (!playerController) return;

        // View sway
        Vector2 lookInput = playerController.lookInput;
        m_targetViewSwayRotation.x += viewSwayAmount.y * (invertViewSwayY ? lookInput.y : -lookInput.y) * Time.deltaTime;
        m_targetViewSwayRotation.y += viewSwayAmount.x * (invertViewSwayX ? -lookInput.x : lookInput.x) * Time.deltaTime;
        m_targetViewSwayRotation.z += viewSwayAmount.z * (invertViewSwayZ ? lookInput.x : -lookInput.x) * Time.deltaTime;
        m_targetViewSwayRotation.x = Mathf.Clamp(m_targetViewSwayRotation.x, -viewSwayClamp.x, viewSwayClamp.x);
        m_targetViewSwayRotation.y = Mathf.Clamp(m_targetViewSwayRotation.y, -viewSwayClamp.y, viewSwayClamp.y);
        m_targetViewSwayRotation.z = Mathf.Clamp(m_targetViewSwayRotation.z, -viewSwayClamp.z, viewSwayClamp.z);
        m_targetViewSwayRotation = Vector3.SmoothDamp(m_targetViewSwayRotation, Vector3.zero, ref m_targetViewSwayRotationVelocity, viewSwayResetSmoothing);
        m_newViewSwayRotation = Vector3.SmoothDamp(m_newViewSwayRotation, m_targetViewSwayRotation, ref m_newViewSwayRotationVelocity, viewSwaySmoothing);

        // Movement sway
        Vector2 moveInput = playerController.moveInput;
        m_targetMovementSwayRotation.z = movementSwayAmount.x * (invertMovementSwayX ? -moveInput.x : moveInput.x);
        m_targetMovementSwayRotation.x = movementSwayAmount.y * (invertMovementSwayY ? -moveInput.y : moveInput.y);
        m_targetMovementSwayRotation = Vector3.SmoothDamp(m_targetMovementSwayRotation, Vector3.zero, ref m_targetMovementSwayRotationVelocity, movementSwayResetSmoothing);
        m_newMovementSwayRotation = Vector3.SmoothDamp(m_newMovementSwayRotation, m_targetMovementSwayRotation, ref m_newMovementSwayRotationVelocity, movementSwaySmoothing);

        // Apply sway
        transform.localRotation = Quaternion.Euler(m_newViewSwayRotation + m_newMovementSwayRotation);
    }
}
