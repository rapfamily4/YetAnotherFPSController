using UnityEngine;


public class WeaponController : MonoBehaviour {
    // --- Inspector parameters
    [Header("Firing")]
    public Transform muzzlePivot;
    public WeaponFireMode primaryFire;
    public WeaponFireMode secondaryFire;
    public bool weaponSwapAbortsBurst = true;

    [Header("Event Channels")]
    public WeaponControllerEventChannel weaponEnabledEventChannel;
    public WeaponControllerEventChannel weaponDisabledEventChannel;
    public VoidEventChannel weaponUnequippedEventChannel;

    [Header("Debug")]
    public bool printDebugInfo = false;

    // --- Public interface
    [HideInInspector] public GameObject owner = null;
    public WeaponAnimationController weaponAnimationController => m_weaponAnimationController;

    // --- Private members
    private WeaponAnimationController m_weaponAnimationController;
    private PlayerCameraController m_cameraController;
    private bool m_isLowering;
    private bool m_started = false;
    private float m_switchingCounter;

    // --- MonoBehaviour methods
    private void Awake() {
        m_weaponAnimationController = GetComponent<WeaponAnimationController>();
        m_cameraController = transform.parent.parent.GetComponent<PlayerCameraController>(); // CameraHolder -> BaseCamera -> {WeaponCamera, WeaponController(s)...}
    }
    
    private void OnEnable() {
        // Subscribe to firemodes' events
        primaryFire.Fired.AddListener(OnFired);
        primaryFire.PutOtherFireModeToWait.AddListener(OnPutOtherFireModeToWait);
        primaryFire.Finished.AddListener(OnFinished);
        secondaryFire.Fired.AddListener(OnFired);
        secondaryFire.PutOtherFireModeToWait.AddListener(OnPutOtherFireModeToWait);
        secondaryFire.Finished.AddListener(OnFinished);

        // Reset state
        m_isLowering = false;
        m_switchingCounter = 0f;
        ResetFireStates();

        // Alert listeners
        if (m_started)
            weaponEnabledEventChannel.WeaponControllerEvent.Invoke(this);
    }

    private void Start() {
        if (!m_started) {
            weaponEnabledEventChannel.WeaponControllerEvent.Invoke(this);
            weaponAnimationController.cameraController = m_cameraController;
            m_started = true;
        }
    }

    private void OnDisable() {
        // Unsubscribe to firemodes' events
        primaryFire.Fired.RemoveListener(OnFired);
        primaryFire.PutOtherFireModeToWait.RemoveListener(OnPutOtherFireModeToWait);
        primaryFire.Finished.RemoveListener(OnFinished);
        secondaryFire.Fired.RemoveListener(OnFired);
        secondaryFire.PutOtherFireModeToWait.RemoveListener(OnPutOtherFireModeToWait);
        secondaryFire.Finished.RemoveListener(OnFinished);

        // Alert listeners
        if (m_started)
            weaponDisabledEventChannel.WeaponControllerEvent.Invoke(this);
    }

    private void Update() {
        UpdateFireStates();
        UpdateSwitching();
    }

    void OnGUI() {
        if (!printDebugInfo) return;

        string state = "### WEAPON SWITCH ###" +
                       "\nm_isLowering: " + m_isLowering +
                       "\nm_switchingCounter: " + m_switchingCounter;
        if (primaryFire)
            state += "\n\n### PRIMARY FIRE ###" +
                     "\nprimaryFire.state.locked: " + primaryFire.state.locked +
                     "\nprimaryFire.state.waiting: " + primaryFire.state.waiting +
                     "\nprimaryFire.state.buffered: " + primaryFire.state.buffered +
                     "\nprimaryFire.state.delayCounter: " + primaryFire.state.delayCounter +
                     "\nprimaryFire.state.burstDelayCounter: " + primaryFire.state.burstDelayCounter +
                     "\nprimaryFire.state.burstShotsCounter: " + primaryFire.state.burstShotsCounter;
        if (secondaryFire)
            state += "\n\n### SECONDARY FIRE ###" +
                     "\nsecondaryFire.state.locked: " + secondaryFire.state.locked +
                     "\nsecondaryFire.state.waiting: " + secondaryFire.state.waiting +
                     "\nsecondaryFire.state.buffered: " + secondaryFire.state.buffered +
                     "\nsecondaryFire.state.delayCounter: " + secondaryFire.state.delayCounter +
                     "\nsecondaryFire.state.burstDelayCounter: " + secondaryFire.state.burstDelayCounter +
                     "\nsecondaryFire.state.burstShotsCounter: " + secondaryFire.state.burstShotsCounter;
        GUILayout.Label($"<color='black'><size=14>{state}</size></color>");
    }

    // --- WeaponController methods
    public void DoFire(bool primary, bool inputDown, bool inputHeld, bool inputUp) {
        WeaponFireMode fireMode = primary ? primaryFire : secondaryFire;
        fireMode.Trigger(inputDown, inputHeld, inputUp);
    }

    public void DoWeaponSwitch(bool unequip) {
        // Set unequip state
        m_isLowering = unequip;

        // Abort any burst sequence
        if (weaponSwapAbortsBurst) {
            if (primaryFire)
                primaryFire.state.burstShotsCounter = primaryFire.burstShots;
            if (secondaryFire)
                secondaryFire.state.burstShotsCounter = secondaryFire.burstShots;
        }
    }

    private void ResetFireStates() {
        if (primaryFire)
            primaryFire.ResetState();
        if (secondaryFire)
            secondaryFire.ResetState();
    }

    private void UpdateFireStates() {
        if (primaryFire)
            primaryFire.Update();
        if (secondaryFire)
            secondaryFire.Update();
    }

    private void UpdateSwitching() {
        float switchingTime = m_weaponAnimationController.gameplaySettings.switchingTime;
        if (m_switchingCounter < switchingTime || m_isLowering) {
            m_switchingCounter += Time.deltaTime * (m_isLowering ? -1f : 1f);
            if (m_isLowering && m_switchingCounter <= 0f) {      // if weapon is fully unequipped
                m_switchingCounter = 0f;
                m_isLowering = false;
                weaponUnequippedEventChannel.VoidEvent.Invoke();
            } else if (m_switchingCounter >= switchingTime) {    // if weapon is fully equipped
                m_switchingCounter = switchingTime;
                primaryFire.state.locked = false;
                secondaryFire.state.locked = false;
            } else {                                             // if weapon is either raising or lowering
                primaryFire.state.locked = true;
                secondaryFire.state.locked = true;
            }
        }
        m_weaponAnimationController.SetSwitchingTarget(m_switchingCounter);
    }

    private void OnFired(WeaponFireMode fireMode) {
        // Shoot projectiles
        GameObject source = owner == null ? gameObject : owner;
        for (int i = 0; i < fireMode.projectilesFired; i++)
            fireMode.projectilePool.Get().OnShoot(muzzlePivot.position, ComputeProjectileRotation(muzzlePivot, fireMode.spreadAngle), source, m_cameraController.baseCamera);

        // Trigger animations
        m_weaponAnimationController.TriggerFire(primaryFire.Equals(fireMode));
    }

    private void OnPutOtherFireModeToWait(WeaponFireMode fireMode) {
        // Permit other firemodes to be triggered
        if (primaryFire && !primaryFire.Equals(fireMode))
            primaryFire.PutToWait();
        if (secondaryFire && !secondaryFire.Equals(fireMode))
            secondaryFire.PutToWait();
    }

    private void OnFinished(WeaponFireMode fireMode) {
        // Permit other firemodes to be triggered
        if (primaryFire && !primaryFire.Equals(fireMode))
            primaryFire.state.waiting = false;
        if (secondaryFire && !secondaryFire.Equals(fireMode))
            secondaryFire.state.waiting = false;
    }

    private Quaternion ComputeProjectileRotation(Transform muzzle, float spread) {
        float spreadRatio = spread / 180f;
        Vector3 candidate = Vector3.Slerp(muzzle.forward, Random.insideUnitSphere, spreadRatio);
        return Quaternion.LookRotation(candidate);
    }
}
