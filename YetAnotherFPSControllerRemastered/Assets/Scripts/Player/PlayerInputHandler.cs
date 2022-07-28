/** Luigi Rapetta (2022) */

using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : MonoBehaviour {
    // --- Action status definition
    public struct ActionStatus {
        public bool started;
        public bool performed;
        public bool canceled;
    }

    // --- Public members
    [Header("Mouse control")]
    [Range(0f, 10f)] public float mouseSensitivity = 3f;
    [Range(0f, 1f)] public float mouseSmoothness = 0.03f;
    public bool invertXAxis = false;
    public bool invertYAxis = false;

    [Header("Cursor")]
    public bool lockCursor = true;

    // --- Public properties
    public Vector2 lookDelta { get { return m_mouseDelta * m_mouseSensitivityMultiplier; } }
    public Vector2 moveInput { get { return m_horizontalInput; } }
    public ActionStatus jumpStatus { get { UpdateActionStatus(ref m_jumpStatus, m_controls.Player.Jump); return m_jumpStatus; } }
    public ActionStatus crouchStatus { get { UpdateActionStatus(ref m_crouchStatus, m_controls.Player.Crouch); return m_crouchStatus; } }
    public ActionStatus thrustStatus { get { UpdateActionStatus(ref m_thrustStatus, m_controls.Player.Thrust); return m_thrustStatus; } }

    // --- Private members
    private PlayerControls m_controls;
    private float m_mouseSensitivityMultiplier;
    private Vector2 m_mouseDelta = Vector2.zero;
    private Vector2 m_mouseDeltaRaw = Vector2.zero;
    private Vector2 m_mouseDeltaDamp = Vector2.zero;
    private Vector2 m_horizontalInput = Vector2.zero;
    private ActionStatus m_jumpStatus = new ActionStatus();
    private ActionStatus m_crouchStatus = new ActionStatus();
    private ActionStatus m_thrustStatus = new ActionStatus();

    // --- Private constants
    private const float k_mouseSensitivityScale = 0.03f;


    // --- MonoBehaviour methods
    void Awake() {
        // Setup Input System's callbacks
        m_controls = new PlayerControls();
        m_controls.Player.Look.performed  += ctx => m_mouseDeltaRaw = ctx.ReadValue<Vector2>();
        m_controls.Player.Look.canceled   += ctx => m_mouseDeltaRaw = Vector2.zero;
        m_controls.Player.Move.performed  += ctx => m_horizontalInput = ctx.ReadValue<Vector2>();
        m_controls.Player.Move.canceled   += ctx => m_horizontalInput = Vector2.zero;

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
        // Update mouse delta at each update; this should be enough for handling smoothness correctly
        UpdateMouseDelta();
    }

    void OnEnable() {
        m_controls.Enable();
    }

    void OnDisable() {
        m_controls.Disable();
    }

    void OnGUI() {
        string lookDelta = "Look delta (" + (mouseSmoothness > 0f ? "with" : "without") + " smoothness): " + m_mouseDelta.ToString();
        GUILayout.Label($"<color='black'><size=14>{lookDelta}</size></color>");
    }

    // --- PlayerInputHandler methods
    private void UpdateMouseDelta() {
        Vector2 target = new Vector2((invertXAxis ? -1f : 1f) * m_mouseDeltaRaw.x, (invertYAxis ? 1f : -1f) * m_mouseDeltaRaw.y);
        m_mouseDelta = Vector2.SmoothDamp(m_mouseDelta, target, ref m_mouseDeltaDamp, mouseSmoothness);
    }

    private void UpdateActionStatus(ref ActionStatus status, InputAction action) {
        status.started = action.WasPressedThisFrame();
        status.performed = action.WasPerformedThisFrame();
        status.canceled = action.WasReleasedThisFrame();
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
