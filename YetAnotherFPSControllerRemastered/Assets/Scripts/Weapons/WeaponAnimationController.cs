using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponAnimationController : MonoBehaviour {
    // --- Inspector parameters
    [Header("Procedural Animations")]
    public Transform modelPivot;
    public WeaponMovementAnimationSettings movementSettings;
    public WeaponGameplayAnimationSettings gameplaySettings;

    // --- Properties
    public PlayerCameraController cameraController {get { return m_cameraController; } set { m_cameraController = value; }}

    // --- Private members
    // References
    private Animator m_animator;
    private PlayerCameraController m_cameraController;
    // Player info
    private Vector2 m_moveInput;
    // Default configuration
    private Vector3 m_defaultPosition;
    private Vector3 m_defaultRotation;
    // Idle
    private float m_idlePeriodCounter;
    private Vector3 m_idleBobTranslation;
    private Vector3 m_idleSwayRotation;
    private Vector3 m_idleNoiseRotation;
    // Movement bob
    private Vector3 m_movementBobTranslation;
    private Vector3 m_movementBobTranslationVelocity;
    private Vector3 m_targetMovementBobTranslation;
    private Vector3 m_targetMovementBobTranslationVelocity;
    private Vector3 m_movementBobRotation;
    private Vector3 m_movementBobRotationVelocity;
    private Vector3 m_targetMovementBobRotation;
    private Vector3 m_targetMovementBobRotationVelocity;
    // Movement pan
    private Vector3 m_movementPanTranslation;
    private Vector3 m_movementPanTranslationVelocity;
    private Vector3 m_targetMovementPanTranslation;
    private Vector3 m_targetMovementPanTranslationVelocity;
    // Movement sway
    private Vector3 m_movementSwayRotation;
    private Vector3 m_movementSwayRotationVelocity;
    private Vector3 m_targetMovementSwayRotation;
    private Vector3 m_targetMovementSwayRotationVelocity;
    // View sway
    private Vector3 m_viewSwayRotation;
    private Vector3 m_viewSwayRotationVelocity;
    private Vector3 m_targetViewSwayRotation;
    private Vector3 m_targetViewSwayRotationVelocity;
    // Physics-based
    private Vector3 m_springDamperTranslation;
    private Vector3 m_springDamperRotation;
    private Vector3 m_springDamperVelocity;
    // Switching
    private float m_switchingPhase;
    private Vector3 m_switchingTranslation;
    private Vector3 m_switchingTranslationVelocity;
    private Vector3 m_switchingRotation;
    private Vector3 m_switchingRotationVelocity;
    // Fire
    private int m_primaryFireID;
    private int m_secondaryFireID;
    private Vector3 m_recoilTranslation;
    private Vector3 m_recoilTranslationVelocity;
    private Vector3 m_targetRecoilTranslation;
    private Vector3 m_targetRecoilTranslationVelocity;
    private Vector3 m_recoilNoiseTranslation;
    private Vector3 m_recoilNoiseRotation;

    // --- Private constants
    private const float k_doublePi = 2f * Mathf.PI;
    private const float k_nearZero = 0.0001f;


    // --- MonoBehaviour methods
    private void Awake() {
        // Animator setup
        m_animator = GetComponentInChildren<Animator>();
        m_primaryFireID = Animator.StringToHash(Constants.ANIM_PRIMARYFIRE);
        m_secondaryFireID = Animator.StringToHash(Constants.ANIM_SECONDARYFIRE);

        // Store default transform of the model's pivot
        m_defaultPosition = modelPivot.transform.localPosition;
        m_defaultRotation = modelPivot.transform.localRotation.eulerAngles;
    }

    private void OnEnable() {
        ResetAnimationState();
    }

    private void OnDisable() {
        // Reset rotations, positions
        modelPivot.transform.SetLocalPositionAndRotation(
            m_defaultPosition + gameplaySettings.switchingUnequipPosition,
            Quaternion.Euler(m_defaultRotation + gameplaySettings.switchingUnequipRotation)
        );
    }

    private void Update() {
        // Procedural animations
        ComputeIdle();
        ComputeMovementBob();
        ComputeSwayAndPan();
        ComputePhysicsBased();
        ComputeSwitching();
        ComputeRecoil();
        ApplyNewPositionAndRotation();
    }

    // --- WeaponAnimationController methods
    public void SetViewSwayTarget(float deltaX, float deltaY) {
        m_targetViewSwayRotation.x += movementSettings.viewSwayAmount.y * (movementSettings.invertViewSwayY ? deltaY : -deltaY) * Time.deltaTime;
        m_targetViewSwayRotation.y += movementSettings.viewSwayAmount.x * (movementSettings.invertViewSwayX ? -deltaX : deltaX) * Time.deltaTime;
        m_targetViewSwayRotation.z += movementSettings.viewSwayAmount.z * (movementSettings.invertViewSwayZ ? deltaX : -deltaX) * Time.deltaTime;
        m_targetViewSwayRotation.x = Mathf.Clamp(m_targetViewSwayRotation.x, -movementSettings.viewSwayClamp.x, movementSettings.viewSwayClamp.x);
        m_targetViewSwayRotation.y = Mathf.Clamp(m_targetViewSwayRotation.y, -movementSettings.viewSwayClamp.y, movementSettings.viewSwayClamp.y);
        m_targetViewSwayRotation.z = Mathf.Clamp(m_targetViewSwayRotation.z, -movementSettings.viewSwayClamp.z, movementSettings.viewSwayClamp.z);
    }

    public void SetMovementTarget(Vector2 moveInput) {
        // Store player's movement input
        // NOTE: Since PlayerController executes DoMove only once the move input *changes*, and since it wouldn't
        //       be nice to set targets for movement translations in each PlayerController's Update, the new movement
        //       is stored here and then used to influence targets for movement-based translations.
        m_moveInput = moveInput;
    }

    public void SetMovementBobTarget(float sin) {
        // Set target
        float absSin = Mathf.Abs(sin);
        m_targetMovementBobTranslation.x = sin * movementSettings.movementBobTranslationAmount.x;
        m_targetMovementBobTranslation.y = (-absSin) * movementSettings.movementBobTranslationAmount.y;
        m_targetMovementBobTranslation.z = (-absSin) * movementSettings.movementBobTranslationAmount.z;
        m_targetMovementBobRotation.x = absSin * movementSettings.movementBobRotationAmount.y;
        m_targetMovementBobRotation.y = sin * movementSettings.movementBobRotationAmount.x;
        m_targetMovementBobRotation.z = sin * movementSettings.movementBobRotationAmount.z;
    }

    public void ResetMovementBobTarget() {
        // Reset target smoothly
        m_targetMovementBobTranslation = Vector3.SmoothDamp(m_targetMovementBobTranslation, Vector3.zero, ref m_targetMovementBobTranslationVelocity, movementSettings.movementBobResetSmoothing);
        m_targetMovementBobRotation = Vector3.SmoothDamp(m_targetMovementBobRotation, Vector3.zero, ref m_targetMovementBobRotationVelocity, movementSettings.movementBobResetSmoothing);
    }

    public void SetSwitchingTarget(float switchingCounter) {
        m_switchingPhase = switchingCounter / gameplaySettings.switchingTime;
    }

    public void TriggerJump(Vector3 jumpDirection, Transform cameraTransform) {
        Vector3 localJumpDirection = cameraTransform.InverseTransformDirection(jumpDirection);
        Vector3 verticalComponent = new Vector3(0f, -Mathf.Abs(cameraTransform.forward.y), 0f);
        m_springDamperVelocity += (verticalComponent - localJumpDirection).normalized * movementSettings.jumpVelocity;
    }

    public void TriggerLand(float landingForce, Transform cameraTransform) {
        Vector3 localDownDirection = cameraTransform.InverseTransformDirection(Vector3.down);
        Vector3 verticalComponent = new Vector3(0f, -Mathf.Abs(cameraTransform.forward.y), 0f);
        m_springDamperVelocity += movementSettings.landingVelocity * landingForce * (verticalComponent + localDownDirection).normalized;
    }

    public void TriggerThrust(Vector3 thrustDirection, Transform cameraTransform) {
        Vector3 localThrustDirection = cameraTransform.InverseTransformDirection(thrustDirection);
        m_springDamperVelocity += -localThrustDirection * movementSettings.thrustVelocity;
    }

    public void TriggerFire(bool isPrimary) {
        // Play animation
        m_animator.SetTrigger(isPrimary ? m_primaryFireID : m_secondaryFireID);

        // Procedural recoil
        m_targetRecoilTranslation.z -= isPrimary ? gameplaySettings.primaryRecoilAmount : gameplaySettings.secondaryRecoilAmount;
        m_targetRecoilTranslation.z = Mathf.Clamp(m_targetRecoilTranslation.z, -gameplaySettings.maxRecoilDisplacement, 0f);

        // Camera pushback
        if (m_cameraController != null) {
            float push = isPrimary ? gameplaySettings.primaryRecoilCameraPushback : gameplaySettings.secondaryRecoilCameraPushback;
            m_cameraController.RecoilCamera(new Vector3(-push, 0f, 0f));
        }
    }

    private void ResetAnimationState() {
        // Idle
        m_idlePeriodCounter = 0f;
        m_idleBobTranslation = Vector3.zero;
        m_idleSwayRotation = Vector3.zero;
        m_idleNoiseRotation = Vector3.zero;
        // Movement bob
        m_movementBobTranslation = Vector3.zero;
        m_movementBobTranslationVelocity = Vector3.zero;
        m_targetMovementBobTranslation = Vector3.zero;
        m_targetMovementBobTranslationVelocity = Vector3.zero;
        m_movementBobRotation = Vector3.zero;
        m_movementBobRotationVelocity = Vector3.zero;
        m_targetMovementBobRotation = Vector3.zero;
        m_targetMovementBobRotationVelocity = Vector3.zero;
        // Movement pan
        m_movementPanTranslation = Vector3.zero;
        m_movementPanTranslationVelocity = Vector3.zero;
        m_targetMovementPanTranslation = Vector3.zero;
        m_targetMovementPanTranslationVelocity = Vector3.zero;
        // Movement sway
        m_movementSwayRotation = Vector3.zero;
        m_movementSwayRotationVelocity = Vector3.zero;
        m_targetMovementSwayRotation = Vector3.zero;
        m_targetMovementSwayRotationVelocity = Vector3.zero;
        // View sway
        m_viewSwayRotation = Vector3.zero;
        m_viewSwayRotationVelocity = Vector3.zero;
        m_targetViewSwayRotation = Vector3.zero;
        m_targetViewSwayRotationVelocity = Vector3.zero;
        // Physics-based
        m_springDamperTranslation = Vector3.zero;
        m_springDamperRotation = Vector3.zero;
        m_springDamperVelocity = Vector3.zero;
        // Switching
        m_switchingPhase = 0f;
        m_switchingTranslation = gameplaySettings.switchingUnequipPosition;
        m_switchingTranslationVelocity = Vector3.zero;
        m_switchingRotation = gameplaySettings.switchingUnequipRotation;
        m_switchingRotationVelocity = Vector3.zero;
        // Fire
        m_recoilTranslation = Vector3.zero;
        m_recoilTranslationVelocity = Vector3.zero;
        m_targetRecoilTranslation = Vector3.zero;
        m_targetRecoilTranslationVelocity = Vector3.zero;
        m_recoilNoiseTranslation = Vector3.zero;
        m_recoilNoiseRotation = Vector3.zero;
    }

    private void ComputeIdle() {
        // Compute translation and rotation
        float frequency = movementSettings.idleFrequency;
        float evaluator = k_doublePi * frequency * m_idlePeriodCounter;
        m_idleBobTranslation.y = Mathf.Sin(evaluator) * movementSettings.idleBobAmount;
        m_idleSwayRotation.x = Mathf.Cos(evaluator) * movementSettings.idleSwayAmount;

        // Also compute idle noise
        float xNoise = (Mathf.Clamp01(Mathf.PerlinNoise(Time.timeSinceLevelLoad * movementSettings.idleNoiseSpeed, 0f)) - 0.5f) * 2f;
        float yNoise = (Mathf.Clamp01(Mathf.PerlinNoise(Time.timeSinceLevelLoad * movementSettings.idleNoiseSpeed, 1f)) - 0.5f) * 2f;
        float zNoise = (Mathf.Clamp01(Mathf.PerlinNoise(Time.timeSinceLevelLoad * movementSettings.idleNoiseSpeed, 2f)) - 0.5f) * 2f;
        m_idleNoiseRotation.y = xNoise * movementSettings.idleNoiseAmount.x;
        m_idleNoiseRotation.x = yNoise * movementSettings.idleNoiseAmount.y;
        m_idleNoiseRotation.z = zNoise * movementSettings.idleNoiseAmount.z;

        // Update counter
        m_idlePeriodCounter += Time.deltaTime;
        if (frequency * m_idlePeriodCounter >= 1f) // m_idlePeriodCounter >= 1 / frequency
            m_idlePeriodCounter %= 1 / frequency;
    }

    private void ComputeMovementBob() {
        // Compute translation and rotation
        m_movementBobTranslation = Vector3.SmoothDamp(m_movementBobTranslation, m_targetMovementBobTranslation, ref m_movementBobTranslationVelocity, movementSettings.movementBobSmoothing);
        m_movementBobRotation = Vector3.SmoothDamp(m_movementBobRotation, m_targetMovementBobRotation, ref m_movementBobRotationVelocity, movementSettings.movementBobSmoothing);
    }

    private void ComputeSwayAndPan() {
        // Movement panning
        m_targetMovementPanTranslation.x = movementSettings.movementPanAmount.x * (movementSettings.invertMovementPanX ? -m_moveInput.x : m_moveInput.x);
        m_targetMovementPanTranslation.z = movementSettings.movementPanAmount.y * (movementSettings.invertMovementPanY ? -m_moveInput.y : m_moveInput.y);
        m_targetMovementPanTranslation = Vector3.SmoothDamp(m_targetMovementPanTranslation, Vector3.zero, ref m_targetMovementPanTranslationVelocity, movementSettings.movementPanResetSmoothing);
        m_movementPanTranslation = Vector3.SmoothDamp(m_movementPanTranslation, m_targetMovementPanTranslation, ref m_movementPanTranslationVelocity, movementSettings.movementPanSmoothing);

        // Movement sway
        // NOTE: The target is always set here, since PlayerController executes DoMove only once the move input *changes*
        m_targetMovementSwayRotation.z = movementSettings.movementSwayAmount.x * (movementSettings.invertMovementSwayX ? -m_moveInput.x : m_moveInput.x);
        m_targetMovementSwayRotation.x = movementSettings.movementSwayAmount.y * (movementSettings.invertMovementSwayY ? -m_moveInput.y : m_moveInput.y);
        m_targetMovementSwayRotation = Vector3.SmoothDamp(m_targetMovementSwayRotation, Vector3.zero, ref m_targetMovementSwayRotationVelocity, movementSettings.movementSwayResetSmoothing);
        m_movementSwayRotation = Vector3.SmoothDamp(m_movementSwayRotation, m_targetMovementSwayRotation, ref m_movementSwayRotationVelocity, movementSettings.movementSwaySmoothing);

        // View sway
        m_targetViewSwayRotation = Vector3.SmoothDamp(m_targetViewSwayRotation, Vector3.zero, ref m_targetViewSwayRotationVelocity, movementSettings.viewSwayResetSmoothing);
        m_viewSwayRotation = Vector3.SmoothDamp(m_viewSwayRotation, m_targetViewSwayRotation, ref m_viewSwayRotationVelocity, movementSettings.viewSwaySmoothing);
    }

    private void ComputePhysicsBased() {
        // Update translation and velocity
        MathUtils.SpringDamperVariableDeltaTime(ref m_springDamperTranslation, ref m_springDamperVelocity, movementSettings.elasticity, movementSettings.damping);

        // Apply rotation amount for displacements on local axes
        m_springDamperRotation.x = -movementSettings.rotationAmountOnHorizontalPlane.x * m_springDamperTranslation.z
                                   - movementSettings.rotationAmountOnVerticalAxis * m_springDamperTranslation.y;
        m_springDamperRotation.y = movementSettings.rotationAmountOnHorizontalPlane.y * m_springDamperTranslation.x;
        m_springDamperRotation.z = movementSettings.rotationAmountOnHorizontalPlane.z * m_springDamperTranslation.x;
    }

    private void ComputeSwitching() {
        // Compute new targets and displacements
        Vector3 targetSwitchingTranslation = Vector3.Lerp(gameplaySettings.switchingUnequipPosition, Vector3.zero, m_switchingPhase);
        m_switchingTranslation = Vector3.SmoothDamp(m_switchingTranslation, targetSwitchingTranslation, ref m_switchingTranslationVelocity, gameplaySettings.switchingSmoothing);
        Vector3 targetSwitchingRotation = Vector3.Lerp(gameplaySettings.switchingUnequipRotation, Vector3.zero, m_switchingPhase);
        m_switchingRotation = Vector3.SmoothDamp(m_switchingRotation, targetSwitchingRotation, ref m_switchingRotationVelocity, gameplaySettings.switchingSmoothing);
    }

    private void ComputeRecoil() {
        // Backwards displacement
        m_recoilTranslation = Vector3.SmoothDamp(m_recoilTranslation, m_targetRecoilTranslation, ref m_recoilTranslationVelocity, gameplaySettings.recoilSmoothing);
        m_targetRecoilTranslation = Vector3.SmoothDamp(m_targetRecoilTranslation, Vector3.zero, ref m_targetRecoilTranslationVelocity, gameplaySettings.recoilResetSmoothing);

        // Noisy displacement
        if (m_recoilTranslation.z < k_nearZero) {
            float zRatio = m_recoilTranslation.z / gameplaySettings.maxRecoilDisplacement;
            Vector2 noiseAmount = Mathf.Pow(zRatio, gameplaySettings.noiseFalloff) * gameplaySettings.noiseAmount;
            Vector2 noise = Vector3.zero;
            noise.x = (Mathf.Clamp01(Mathf.PerlinNoise(Time.timeSinceLevelLoad * movementSettings.idleNoiseSpeed, 10f)) - 0.5f) * 2f;
            noise.y = (Mathf.Clamp01(Mathf.PerlinNoise(Time.timeSinceLevelLoad * movementSettings.idleNoiseSpeed, 18f)) - 0.5f) * 2f;
            m_recoilNoiseTranslation = noise * noiseAmount;
        } else m_recoilNoiseTranslation = Vector3.zero;
        m_recoilNoiseRotation.x = m_recoilNoiseTranslation.y;
        m_recoilNoiseRotation.y = -m_recoilNoiseTranslation.x;
    }

    private void ApplyNewPositionAndRotation() {
        // Compute new position and rotation
        Vector3 newPosition = m_defaultPosition + m_idleBobTranslation + m_movementBobTranslation + m_movementPanTranslation + m_springDamperTranslation + m_switchingTranslation + m_recoilTranslation + m_recoilNoiseTranslation;
        Vector3 newRotation = m_defaultRotation + m_idleSwayRotation + m_idleNoiseRotation + m_movementBobRotation + m_movementSwayRotation + m_viewSwayRotation + m_springDamperRotation + m_switchingRotation + m_recoilNoiseRotation;

        // Apply
        modelPivot.transform.SetLocalPositionAndRotation(newPosition, Quaternion.Euler(newRotation));
    }
}
