/** Luigi Rapetta (2022) */

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : MonoBehaviour {
    // --- Public members
    [Header("Mouse control")]
    [Range(0f, 10f)] public float mouseSensitivity = 3f;
    [Range(0f, 1f)] public float mouseSmoothness = 0.03f;
    public bool invertXAxis = false;
    public bool invertYAxis = false;

    [Header("Crouch behaviour")]
    public bool toggleCrouch = false;

    [Header("Cursor")]
    public bool lockCursor = true;

    // --- Private members
    private PlayerControls m_controls;
    private PlayerController m_controller;
    private float m_mouseSensitivityMultiplier;
    private Vector2 m_mouseDelta = Vector2.zero;
    private Vector2 m_mouseDeltaRaw = Vector2.zero;
    private Vector2 m_mouseDeltaDamp = Vector2.zero;
    private Vector2 m_mouseOld = Vector2.zero;

    // --- Private constants
    private const float k_mouseSensitivityScale = 0.03f;


    // --- MonoBehaviour methods
    void Awake() {
        // Fetch components
        m_controls = new PlayerControls();
        m_controller = GetComponent<PlayerController>();

        // Move callbacks
        m_controls.Player.Move.performed += ctx => m_controller.DoMove(ctx.ReadValue<Vector2>());
        m_controls.Player.Move.canceled  += ctx => m_controller.DoMove(Vector2.zero);
        // Look callbacks
        m_controls.Player.Look.performed += ctx => m_mouseDeltaRaw = ctx.ReadValue<Vector2>();
        m_controls.Player.Look.canceled  += ctx => m_mouseDeltaRaw = Vector2.zero;
        // Jump callbacks
        m_controls.Player.Jump.started += ctx => m_controller.SetJumpBuffer(true);
        m_controls.Player.Jump.started += ctx => m_controller.DoJump();
        m_controls.Player.Jump.canceled += ctx => m_controller.SetJumpBuffer(false);
        // Thrust callbacks
        m_controls.Player.Thrust.started += ctx => m_controller.DoThrust();
        // Crouch callbacks
        m_controls.Player.Crouch.started   += ctx => { if (!toggleCrouch) m_controller.DoCrouch(true); else m_controller.DoCrouch(!m_controller.isCrouching); };
        m_controls.Player.Crouch.canceled  += ctx => { if (!toggleCrouch) m_controller.DoCrouch(false); };

        // Setup state
        OnValidate();
    }

    private void OnValidate() {
        // Set multipliers
        m_mouseSensitivityMultiplier = mouseSensitivity * k_mouseSensitivityScale;

        // Lock cursor
        UpdateCursorState();
    }

    void Update() {
        // Explicitely update mouse delta: this is required for handling smoothness correctly
        UpdateMouseDelta();
    }

    void OnEnable() {
        m_controls.Enable();
    }

    void OnDisable() {
        m_controls.Disable();
    }

    // --- PlayerInputHandler methods
    private void UpdateMouseDelta() {
        // Store old delta
        m_mouseOld.x = m_mouseDelta.x;
        m_mouseOld.y = m_mouseDelta.y;

        // Update delta
        Vector2 target = new Vector2((invertXAxis ? -1f : 1f) * m_mouseDeltaRaw.x, (invertYAxis ? 1f : -1f) * m_mouseDeltaRaw.y);
        m_mouseDelta = Vector2.SmoothDamp(m_mouseDelta, target, ref m_mouseDeltaDamp, mouseSmoothness);

        // Move player's view
        if (!m_mouseDelta.Equals(m_mouseOld))
            m_controller.DoLook(m_mouseDelta * m_mouseSensitivityMultiplier);
    }

    private void UpdateCursorState() {
        if (lockCursor) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        } else {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
