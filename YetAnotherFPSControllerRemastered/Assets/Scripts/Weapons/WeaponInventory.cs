using System;
using UnityEditor.UIElements;
using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable Object/WeaponInventory")]
public class WeaponInventory : ScriptableObject {
    // --- Inspector parameters
    [Tooltip("The list of weapons the player can carry and select, alongside their availability. " +
             "The prefabs of the weapons must be manually placed here in advance.")]
    [SerializeField] private WeaponSlotEntry[] weaponSlots;

    // --- Public properties
    public int currentWeapon { get; set; } = -1;
    public int weaponSlotsNumber { get { return weaponSlots.Length; } }


    // --- WeaponInventory methods
    public WeaponController GetWeaponPrefab(int weaponIndex) {
        if (weaponIndex < 0 || weaponIndex >= weaponSlotsNumber) {
            Debug.LogError("WeaponInventory.GetWeaponPrefab() has invalid index (" + weaponIndex + ")");
            return null;
        } else return weaponSlots[weaponIndex].weaponPrefab;
    }

    public bool GetWeaponAvailability(int weaponIndex) {
        if (weaponIndex < 0 || weaponIndex >= weaponSlotsNumber) {
            Debug.LogError("WeaponInventory.GetWeaponAvailability() has invalid index (" + weaponIndex + ")");
            return false;
        } else return weaponSlots[weaponIndex].available;
    }

    public bool GetWeaponAvailability(WeaponController weaponPrefab) {
        for (int i = 0; i < weaponSlots.Length; i++) {
            // If the provided weapon prefab equals the one in the weapon slots...
            if (weaponPrefab == weaponSlots[i].weaponPrefab)
                return true;
        }
        return false;
    }

    public bool SetWeaponAvailability(int weaponIndex, bool available) {
        if (weaponIndex < 0 || weaponIndex >= weaponSlotsNumber) {
            Debug.LogError("WeaponInventory.SetWeaponAvailability() has invalid index (" + weaponIndex + ")");
            return false;
        } else {
            weaponSlots[weaponIndex].available = available;
            return true;
        }
    }

    public bool SetWeaponAvailability(WeaponController weaponPrefab, bool available, bool returnOnFirstMatch = true) {
        bool wasSet = false;
        for (int i = 0; i < weaponSlots.Length; i++) {
            // If the provided weapon prefab equals the one in the weapon slots...
            if (weaponPrefab == weaponSlots[i].weaponPrefab) {
                weaponSlots[i].available = available;
                wasSet = true;
                if (returnOnFirstMatch) return wasSet;
            }
        }
        return wasSet;
    }
}
