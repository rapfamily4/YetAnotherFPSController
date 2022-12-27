using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerInventoryController : MonoBehaviour {
    // --- StartupBehaviour definition
    private enum StartupBehaviour {
        KeepAll,
        ResetAll,
        GiveAll
    }
    
    // --- Inspector parameters
    [Header("Weapons")]
    public WeaponInventory weaponInventory;
    [SerializeField] private StartupBehaviour startupBehaviour;

    // --- Private members
    private Camera m_cameraHolder;
    private WeaponController[] m_weaponInstances = null;


    // --- MonoBehaviour methods
    private void Awake() {
        // Retrieve references
        m_cameraHolder = GetComponentInChildren<Camera>();
    }

    private void OnEnable() {
        EventManager.StartListening(Constants.EVENT_WEAPONSLOTSMODIFIED, InstantiateWeaponPrefabs);
    }

    private void OnDisable() {
        EventManager.StopListening(Constants.EVENT_WEAPONSLOTSMODIFIED, InstantiateWeaponPrefabs);
    }

    private void Start() {
        // Execute startup behaviour
        if (startupBehaviour != StartupBehaviour.KeepAll)
            for (int i = 0; i < weaponInventory.weaponSlots.Length; i++)
                weaponInventory.weaponSlots[i].available = (startupBehaviour == StartupBehaviour.GiveAll ? true : false);

        // Instantiate weapons
        InstantiateWeaponPrefabs();
    }

    // --- PlayerInventoryController methods
    public void InstantiateWeaponPrefabs() {
        // Destroy previously instantiated weapons
        if (m_weaponInstances != null)
            foreach (WeaponController weaponInstance in m_weaponInstances)
                if (weaponInstance) Destroy(weaponInstance.gameObject);

        // Initialize new weapon instances array
        m_weaponInstances = new WeaponController[weaponInventory.weaponSlots.Length];

        // Instantiate new weapons
        for (int i = 0; i < weaponInventory.weaponSlots.Length; i++) {
            WeaponController weaponPrefab = weaponInventory.weaponSlots[i].weaponPrefab;
            if (weaponPrefab) {
                WeaponController newInstance = Instantiate(weaponPrefab, m_cameraHolder.transform);
                newInstance.gameObject.SetActive(false);
                m_weaponInstances[i] = newInstance;
            }
        }
    }
}
