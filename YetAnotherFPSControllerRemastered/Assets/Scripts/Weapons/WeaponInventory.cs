using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable Object/WeaponInventory")]
public class WeaponInventory : ScriptableObject {
    // --- Public members
    [Tooltip("The list of weapons the player can carry and select. " +
             "The prefabs of the weapons must be manually placed here in advance.\n\n" +
             "IMPORTANT: the prefabs will be instantiated within the player GameObject only in the Start method: " +
             "any edit to this array during gameplay will not update the instantiated content and may lead to " +
             "undesired behaviour.")]
    public WeaponController[] weaponSlots;

    // --- Public hidden members
    [HideInInspector] public int currentWeapon = -1;

    // --- Private members
    private bool[] m_weaponAvailability;


    // --- ScriptableObject methods
    private void Awake() {
        // Initialize weapon availability array
        m_weaponAvailability = new bool[weaponSlots.Length];
        for (int i = 0; i < m_weaponAvailability.Length; i++)
            m_weaponAvailability[i] = false;
    }


    // --- WeaponInventory methods
    public bool GetWeaponAvailability(int index) {
        if (index < 0 || index >= weaponSlots.Length)
            return false;
        else return m_weaponAvailability[index];
    }

    public bool GetWeaponAvailability(WeaponController weaponPrefab) {
        for (int i = 0; i < weaponSlots.Length; i++) {
            // If the provided weapon prefab equals the one in the weapon slots...
            if (weaponPrefab == weaponSlots[i])
                return true;
        }
        return false;
    }

    public bool SetWeaponAvailability(int index, bool available) {
        if (index < 0 || index >= weaponSlots.Length)
            return false;
        else {
            m_weaponAvailability[index] = available;
            return true;
        }
    }

    public bool SetWeaponAvailability(WeaponController weaponPrefab, bool available, bool returnOnFirstMatch = true) {
        bool wasSet = false;
        for (int i = 0; i < weaponSlots.Length; i++) {
            // If the provided weapon prefab equals the one in the weapon slots...
            if (weaponPrefab == weaponSlots[i]) {
                m_weaponAvailability[i] = available;
                wasSet = true;
                if (returnOnFirstMatch) return wasSet;
            }
        }
        return wasSet;
    }
}
