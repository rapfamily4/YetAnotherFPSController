using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerInventoryController : MonoBehaviour {
    // --- Public members
    [Header("Weapons")]
    public WeaponInventory weaponInventory;
    public bool giveAllAtStart;

    // --- Private members
    private Camera m_camera;
    private WeaponController[] m_weaponInstances;


    // --- MonoBehaviour methods
    private void Awake() {
        // Retrieve references
        m_camera = GetComponentInChildren<Camera>();
    }

    private void Start() {
        // Initialize weapon instances array
        m_weaponInstances = new WeaponController[weaponInventory.weaponSlots.Length];

        // Give all weapons at startup, if requested
        if (giveAllAtStart)
            for (int i = 0; i < weaponInventory.weaponSlots.Length; i++)
                weaponInventory.SetWeaponAvailability(i, true);

        // Instantiate weapons
        for (int i = 0; i < weaponInventory.weaponSlots.Length; i++)
            InstantiateWeapon(i);
    }

    // --- PlayerInventoryController methods
    public void InstantiateWeapon(int index) {
        WeaponController newInstance = Instantiate(weaponInventory.weaponSlots[index], m_camera.transform);
        newInstance.gameObject.SetActive(false);
        m_weaponInstances[index] = newInstance;
    }
}
