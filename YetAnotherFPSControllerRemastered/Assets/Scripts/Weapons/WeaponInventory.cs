using System;
using UnityEditor.UIElements;
using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable Object/WeaponInventory")]
public class WeaponInventory : ScriptableObject {
    // --- Public members
    [Tooltip("The list of weapons the player can carry and select, alongside their availability. " +
             "The prefabs of the weapons must be manually placed here in advance.")]
    public WeaponSlotEntry[] weaponSlots;

    // --- Public hidden members
    [HideInInspector] public int currentWeapon = -1;


    // --- WeaponInventory methods
    public bool GetWeaponAvailability(WeaponController weaponPrefab) {
        for (int i = 0; i < weaponSlots.Length; i++) {
            // If the provided weapon prefab equals the one in the weapon slots...
            if (weaponPrefab == weaponSlots[i].weaponPrefab)
                return true;
        }
        return false;
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
