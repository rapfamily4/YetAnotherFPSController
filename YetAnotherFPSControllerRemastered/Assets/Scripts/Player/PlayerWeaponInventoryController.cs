using UnityEngine;


public class PlayerWeaponInventoryController : MonoBehaviour {
    // --- StartupBehaviour definition
    private enum StartupBehaviour {
        KeepAll,
        ResetAll,
        TakeAll,
        GiveAll
    }
    
    // --- Inspector parameters
    [Header("Weapons")]
    [SerializeField] private WeaponInventory weaponInventory;
    [SerializeField] private StartupBehaviour startupBehaviour;

    [Header("Event Channels")]
    public VoidEventChannel weaponUnequippedEventChannel;

    [Header("Debug")]
    public bool printDebugInfo = false;

    // --- Public properties
    public WeaponController activeWeapon {
        get {
            if (m_activeWeaponIndex >= 0)
                return m_weaponInstances[m_activeWeaponIndex];
            return null;
        }
    }
    public WeaponController selectedWeapon {
        get {
            if (weaponInventory.selectedWeapon >= 0)
                return m_weaponInstances[weaponInventory.selectedWeapon];
            return null;
        }
    }

    // --- Private members
    private PlayerCameraController m_cameraHolder;
    private WeaponController[] m_weaponInstances = null;
    private int m_activeWeaponIndex = -1;


    // --- MonoBehaviour methods
    private void Awake() {
        // Retrieve references
        m_cameraHolder = GetComponentInChildren<PlayerCameraController>();
    }

    private void OnEnable() {
#if UNITY_EDITOR
        weaponInventory.WeaponSlotsChanged.AddListener(InstantiateWeaponPrefabs);
#endif
        weaponUnequippedEventChannel.VoidEvent.AddListener(ActivateSelectedWeapon);
    }

    private void OnDisable() {
#if UNITY_EDITOR
        weaponInventory.WeaponSlotsChanged.RemoveListener(InstantiateWeaponPrefabs);
#endif
        weaponUnequippedEventChannel.VoidEvent.RemoveListener(ActivateSelectedWeapon);
    }

    private void Start() {
        // Execute startup behaviour
        switch (startupBehaviour) {
            case StartupBehaviour.KeepAll:
                break;

            case StartupBehaviour.ResetAll:
                weaponInventory.ResetAllAvailability();
                break;

            case StartupBehaviour.TakeAll:
                weaponInventory.TakeAllAvailability();
                break;

            case StartupBehaviour.GiveAll:
                weaponInventory.GiveAllAvailability();
                break;
        }

        // Instantiate weapons
        InstantiateWeaponPrefabs();
    }

        void OnGUI() {
        if (!printDebugInfo) return;

        string state = "### INVENTORY ###" +
                       "\nselectedWeapon: " + weaponInventory.selectedWeapon +
                       "\n\n### INVENTORY CONTROLLER ###" +
                       "\nselectedWeapon: " + selectedWeapon +
                       "\nactiveWeapon:   " + activeWeapon;
        GUILayout.Label($"<color='black'><size=14>{state}</size></color>");
    }

    // --- PlayerInventoryController methods
    public void SelectWeapon(int index) {
        // If selection was successful, show up the according weapon instance
        // NOTE: If the selected weapon is the one which is currently lowering, revert the unequip animation.
        if (weaponInventory.SetSelectedWeapon(index)) {
            if (m_activeWeaponIndex >= 0)
                m_weaponInstances[m_activeWeaponIndex].DoWeaponSwitch(m_activeWeaponIndex == index ? false : true);
            else
                ActivateSelectedWeapon();
        }
    }

    public void PickUpWeapon(WeaponController weaponPrefab) {
        int index = weaponInventory.GetWeaponIndex(weaponPrefab);
        if (index < 0)
            return;
        bool wasAvailable = weaponInventory.GetWeaponAvailability(index);
        if (!wasAvailable) {
            weaponInventory.SetWeaponAvailability(index, true);
            SelectWeapon(index);
        }
    }

    private void InstantiateWeaponPrefabs() {
        // Destroy previously instantiated weapons
        if (m_weaponInstances != null)
            foreach (WeaponController weaponInstance in m_weaponInstances)
                if (weaponInstance) Destroy(weaponInstance.gameObject);

        // Initialize new weapon instances array
        int weaponSlotsNumber = weaponInventory.weaponSlotsNumber;
        m_weaponInstances = new WeaponController[weaponSlotsNumber];

        // Instantiate new weapons
        m_activeWeaponIndex = -1;
        int currentlySelected = weaponInventory.selectedWeapon;
        for (int i = 0; i < weaponSlotsNumber; i++) {
            WeaponController weaponPrefab = weaponInventory.GetWeaponPrefab(i);
            if (weaponPrefab) {
                WeaponController newInstance = Instantiate(weaponPrefab, m_cameraHolder.baseCamera);
                bool active = currentlySelected == i;
                newInstance.gameObject.SetActive(active);
                newInstance.owner = gameObject;
                if (active) m_activeWeaponIndex = i;
                m_weaponInstances[i] = newInstance;
            }
        }
    }

    private void ActivateSelectedWeapon() {
        // Set unequipped weapon as inactive
        if (m_activeWeaponIndex >= 0)
            m_weaponInstances[m_activeWeaponIndex].gameObject.SetActive(false);

        // Activate selected weapon
        int selected = weaponInventory.selectedWeapon;
        m_weaponInstances[selected].gameObject.SetActive(true);
        m_activeWeaponIndex = selected;
    }
}
