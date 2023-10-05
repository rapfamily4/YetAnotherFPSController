/** Luigi Rapetta (2022) */

using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(PlayerController))]
public class PlayerInputHandler : MonoBehaviour {
    // --- Public members
    [Header("Mouse control")]
    [Min(0f)] public float mouseSensitivity = 10f;
    [Range(0f, 1f)] public float mouseSmoothness = 0.03f;
    public bool invertXAxis = false;
    public bool invertYAxis = false;

    [Header("Crouch behaviour")]
    public bool toggleCrouch = false;

    [Header("Cursor")]
    public bool lockCursor = true;

    [Header("Channels")]
    public PlayerControlsChannel playerControlsChannel;
    public VoidEventChannel weaponUnequippedEventChannel;

    [Header("Debug")]
    public bool printDebugInfo = false;

    // --- Private members
    private PlayerControls m_inputActions;
    private PlayerController m_playerController;
    private PlayerWeaponInventoryController m_playerInventory;
    private InputAction m_moveAction;
    private InputAction m_lookAction;
    private InputAction m_jumpAction;
    private InputAction m_thrustAction;
    private InputAction m_crouchAction;
    private InputAction m_primaryFireAction;
    private InputAction m_secondaryFireAction;
    private InputAction m_selectWeapon1Action;
    private InputAction m_selectWeapon2Action;
    private InputAction m_selectWeapon3Action;
    private InputAction m_selectWeapon4Action;
    private InputAction m_selectWeapon5Action;
    private InputAction m_selectWeapon6Action;
    private InputAction m_selectWeapon7Action;
    private InputAction m_selectWeapon8Action;
    private InputAction m_selectWeapon9Action;
    private InputAction m_selectWeapon10Action;
    private Vector2 m_lastMove = Vector2.zero;
    private Vector2 m_mouseDelta = Vector2.zero;
    private Vector2 m_mouseDeltaRaw = Vector2.zero;
    private Vector2 m_mouseDeltaDamp = Vector2.zero;
    private Vector2 m_mouseOld = Vector2.zero;

    // --- Private constants
    private const float k_nearZero = 0.0001f;


    // --- MonoBehaviour methods
    private void Awake() {
        // Retrieve references
        m_playerController = GetComponent<PlayerController>();
        m_playerInventory = GetComponent<PlayerWeaponInventoryController>();
    }

    private void OnEnable() {
        // NOTE: I REALLY DON'T LIKE THIS
        weaponUnequippedEventChannel.VoidEvent.AddListener(OnWeaponUnequipped);

        if (m_inputActions == null) {
            // Retrieve PlayerControls reference
            m_inputActions = playerControlsChannel.inputActions;

            // Cache input actions
            m_moveAction = m_inputActions.Player.Move;
            m_lookAction = m_inputActions.Player.Look;
            m_jumpAction = m_inputActions.Player.Jump;
            m_thrustAction = m_inputActions.Player.Thrust;
            m_crouchAction = m_inputActions.Player.Crouch;
            if (m_playerInventory) {
                m_primaryFireAction = m_inputActions.Player.PrimaryFire;
                m_secondaryFireAction = m_inputActions.Player.SecondaryFire;
                m_selectWeapon1Action = m_inputActions.Player.SelectWeapon1;
                m_selectWeapon2Action = m_inputActions.Player.SelectWeapon2;
                m_selectWeapon3Action = m_inputActions.Player.SelectWeapon3;
                m_selectWeapon4Action = m_inputActions.Player.SelectWeapon4;
                m_selectWeapon5Action = m_inputActions.Player.SelectWeapon5;
                m_selectWeapon6Action = m_inputActions.Player.SelectWeapon6;
                m_selectWeapon7Action = m_inputActions.Player.SelectWeapon7;
                m_selectWeapon8Action = m_inputActions.Player.SelectWeapon8;
                m_selectWeapon9Action = m_inputActions.Player.SelectWeapon9;
                m_selectWeapon10Action = m_inputActions.Player.SelectWeapon10;
            }
        }

        // Enable input actions
        m_moveAction.Enable();
        m_lookAction.Enable();
        m_jumpAction.Enable();
        m_thrustAction.Enable();
        m_crouchAction.Enable();
        if (m_playerInventory) {
            m_primaryFireAction.Enable();
            m_secondaryFireAction.Enable();
            m_selectWeapon1Action.Enable();
            m_selectWeapon2Action.Enable();
            m_selectWeapon3Action.Enable();
            m_selectWeapon4Action.Enable();
            m_selectWeapon5Action.Enable();
            m_selectWeapon6Action.Enable();
            m_selectWeapon7Action.Enable();
            m_selectWeapon8Action.Enable();
            m_selectWeapon9Action.Enable();
            m_selectWeapon10Action.Enable();
        }

        // Subscribe callbacks
        // Move callbacks
        m_moveAction.performed += OnMovePerformed;
        m_moveAction.canceled += OnMoveCanceled;
        // Look callbacks
        m_lookAction.performed += OnLookPerformed;
        m_lookAction.canceled += OnLookCanceled;
        // Jump callbacks
        m_jumpAction.started += OnJumpStarted;
        m_jumpAction.canceled += OnJumpCanceled;
        // Thrust callbacks
        m_thrustAction.started += OnThrustStarted;
        // Crouch callbacks
        m_crouchAction.started += OnCrouchStarted;
        m_crouchAction.canceled += OnCrouchCanceled;
        // Inventory callbacks
        if (m_playerInventory) {
            // Weapon fire
            m_primaryFireAction.started   += OnPrimaryFireStarted;
            m_primaryFireAction.canceled  += OnPrimaryFireCanceled;
            m_secondaryFireAction.started   += OnSecondaryFireStarted;
            m_secondaryFireAction.canceled  += OnSecondaryFireCanceled;
            // Weapon selection callbacks
            m_selectWeapon1Action.started  += OnSelectWeapon1Started;
            m_selectWeapon2Action.started  += OnSelectWeapon2Started;
            m_selectWeapon3Action.started  += OnSelectWeapon3Started;
            m_selectWeapon4Action.started  += OnSelectWeapon4Started;
            m_selectWeapon5Action.started  += OnSelectWeapon5Started;
            m_selectWeapon6Action.started  += OnSelectWeapon6Started;
            m_selectWeapon7Action.started  += OnSelectWeapon7Started;
            m_selectWeapon8Action.started  += OnSelectWeapon8Started;
            m_selectWeapon9Action.started  += OnSelectWeapon9Started;
            m_selectWeapon10Action.started += OnSelectWeapon10Started;
        }

        // Setup state
        OnValidate();
    }

    void OnDisable() {
        // NOTE: I REALLY DON'T LIKE THIS
        weaponUnequippedEventChannel.VoidEvent.RemoveListener(OnWeaponUnequipped);

        // Disable input actions
        m_moveAction.Disable();
        m_lookAction.Disable();
        m_jumpAction.Disable();
        m_thrustAction.Disable();
        m_crouchAction.Disable();
        if (m_playerInventory) {
            m_primaryFireAction.Disable();
            m_secondaryFireAction.Disable();
            m_selectWeapon1Action.Disable();
            m_selectWeapon2Action.Disable();
            m_selectWeapon3Action.Disable();
            m_selectWeapon4Action.Disable();
            m_selectWeapon5Action.Disable();
            m_selectWeapon6Action.Disable();
            m_selectWeapon7Action.Disable();
            m_selectWeapon8Action.Disable();
            m_selectWeapon9Action.Disable();
            m_selectWeapon10Action.Disable();
        }

        // Unsubscribe callbacks
        // Move callbacks
        m_moveAction.performed -= OnMovePerformed;
        m_moveAction.canceled -= OnMoveCanceled;
        // Look callbacks
        m_lookAction.performed -= OnLookPerformed;
        m_lookAction.canceled -= OnLookCanceled;
        // Jump callbacks
        m_jumpAction.started -= OnJumpStarted;
        m_jumpAction.canceled -= OnJumpCanceled;
        // Thrust callbacks
        m_thrustAction.started -= OnThrustStarted;
        // Crouch callbacks
        m_crouchAction.started -= OnCrouchStarted;
        m_crouchAction.canceled -= OnCrouchCanceled;
        // Inventory callbacks
        if (m_playerInventory) {
            // Weapon fire
            m_primaryFireAction.started   -= OnPrimaryFireStarted;
            m_primaryFireAction.canceled  -= OnPrimaryFireCanceled;
            m_secondaryFireAction.started   -= OnSecondaryFireStarted;
            m_secondaryFireAction.canceled  -= OnSecondaryFireCanceled;
            // Weapon selection callbacks
            m_selectWeapon1Action.started  -= OnSelectWeapon1Started;
            m_selectWeapon2Action.started  -= OnSelectWeapon2Started;
            m_selectWeapon3Action.started  -= OnSelectWeapon3Started;
            m_selectWeapon4Action.started  -= OnSelectWeapon4Started;
            m_selectWeapon5Action.started  -= OnSelectWeapon5Started;
            m_selectWeapon6Action.started  -= OnSelectWeapon6Started;
            m_selectWeapon7Action.started  -= OnSelectWeapon7Started;
            m_selectWeapon8Action.started  -= OnSelectWeapon8Started;
            m_selectWeapon9Action.started  -= OnSelectWeapon9Started;
            m_selectWeapon10Action.started -= OnSelectWeapon10Started;
        }
    }

    private void OnValidate() {
        // Lock cursor
        UpdateCursorState();
    }

    void Update() {
        // Explicitely update mouse delta: this is required for handling smoothness correctly
        UpdateMouseDelta();
    }

    void OnGUI() {
        if (!printDebugInfo) return;

        string state = "mouseDelta: " + m_mouseDelta;
        GUILayout.Label($"<color='black'><size=14>{state}</size></color>");
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
            m_playerController.DoLook(m_mouseDelta * mouseSensitivity);
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

    private void OnMovePerformed(InputAction.CallbackContext context) {
        m_lastMove = context.ReadValue<Vector2>();
        m_playerController.DoMove(m_lastMove);
    }

    private void OnMoveCanceled(InputAction.CallbackContext context) {
        m_lastMove = Vector2.zero;
        m_playerController.DoMove(Vector2.zero);
    }

    private void OnLookPerformed(InputAction.CallbackContext context) {
        m_mouseDeltaRaw = context.ReadValue<Vector2>();
    }

    private void OnLookCanceled(InputAction.CallbackContext context) {
        m_mouseDeltaRaw = Vector2.zero;
    }

    private void OnJumpStarted(InputAction.CallbackContext context) {
        m_playerController.SetJumpBuffer(true);
        m_playerController.DoJump();
    }

    private void OnJumpCanceled(InputAction.CallbackContext context) {
        m_playerController.SetJumpBuffer(false);
    }

    private void OnThrustStarted(InputAction.CallbackContext context) {
        m_playerController.DoThrust();
    }

    private void OnCrouchStarted(InputAction.CallbackContext context) {
        if (toggleCrouch) m_playerController.DoCrouch(!m_playerController.isCrouching);
        else m_playerController.DoCrouch(true);
    }

    private void OnCrouchCanceled(InputAction.CallbackContext context) {
        if (!toggleCrouch) m_playerController.DoCrouch(false);
    }

    private void OnPrimaryFireStarted(InputAction.CallbackContext context) {
        if (m_playerInventory.activeWeapon)
            m_playerInventory.activeWeapon.DoFire(true, true, false, false);
    }

    private void OnPrimaryFireCanceled(InputAction.CallbackContext context) {
        if (m_playerInventory.activeWeapon)
            m_playerInventory.activeWeapon.DoFire(true, false, false, true);
    }

    private void OnSecondaryFireStarted(InputAction.CallbackContext context) {
        if (m_playerInventory.activeWeapon)
            m_playerInventory.activeWeapon.DoFire(false, true, false, false);
    }

    private void OnSecondaryFireCanceled(InputAction.CallbackContext context) {
        if (m_playerInventory.activeWeapon)
            m_playerInventory.activeWeapon.DoFire(false, false, false, true);
    }

    private void OnSelectWeaponStarted(int index) {
        m_playerInventory.SelectWeapon(index);
    }

    private void OnSelectWeapon1Started(InputAction.CallbackContext context) {
        OnSelectWeaponStarted(0);
    }

    private void OnSelectWeapon2Started(InputAction.CallbackContext context) {
        OnSelectWeaponStarted(1);
    }

    private void OnSelectWeapon3Started(InputAction.CallbackContext context) {
        OnSelectWeaponStarted(2);
    }

    private void OnSelectWeapon4Started(InputAction.CallbackContext context) {
        OnSelectWeaponStarted(3);
    }

    private void OnSelectWeapon5Started(InputAction.CallbackContext context) {
        OnSelectWeaponStarted(4);
    }

    private void OnSelectWeapon6Started(InputAction.CallbackContext context) {
        OnSelectWeaponStarted(5);
    }

    private void OnSelectWeapon7Started(InputAction.CallbackContext context) {
        OnSelectWeaponStarted(6);
    }

    private void OnSelectWeapon8Started(InputAction.CallbackContext context) {
        OnSelectWeaponStarted(7);
    }

    private void OnSelectWeapon9Started(InputAction.CallbackContext context) {
        OnSelectWeaponStarted(8);
    }

    private void OnSelectWeapon10Started(InputAction.CallbackContext context) {
        OnSelectWeaponStarted(9);
    }

    private void OnWeaponUnequipped() {
        // I REALLY DON'T LIKE THIS
        m_playerInventory.selectedWeapon.weaponAnimationController.SetMovementTarget(m_lastMove);
        m_playerInventory.selectedWeapon.DoFire(true, false, m_primaryFireAction.ReadValue<float>() > k_nearZero, false);
        m_playerInventory.selectedWeapon.DoFire(false, false, m_secondaryFireAction.ReadValue<float>() > k_nearZero, false);
    }
}
