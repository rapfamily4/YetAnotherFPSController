using System;
using UnityEngine;
using UnityEngine.Events;


[Serializable]
public struct WeaponSlotEntry {
    public WeaponController weaponPrefab;
    public bool giveAtStartup;
    [NonSerialized] public bool available;
}

[CreateAssetMenu(menuName = "Scriptable Object/Weapons/Weapon Inventory")]
public class WeaponInventory : ScriptableObject {
    // --- Inspector parameters
    [Tooltip("The list of weapons the player can carry and select, alongside their availability at startup. " +
             "The prefabs of the weapons must be manually placed here in advance.")]
    [SerializeField] private WeaponSlotEntry[] weaponSlots;

    // --- Public properties
    [field: NonSerialized] public int selectedWeapon { get; private set; } = -1;
    public int weaponSlotsNumber { get { return weaponSlots.Length; } }

    // --- Events
#if UNITY_EDITOR
    [NonSerialized] public UnityEvent WeaponSlotsChanged;
#endif


    // --- ScriptableObject methods
    public void OnEnable() {
        hideFlags = HideFlags.DontUnloadUnusedAsset; // It'll ensure us that the SO won't be unloaded if unused
#if UNITY_EDITOR
        WeaponSlotsChanged = new UnityEvent();
#endif
    }

#if UNITY_EDITOR
    public void OnDisable() {
        WeaponSlotsChanged = null;
    }

    public void OnValidate() {
        if (WeaponSlotsChanged != null) WeaponSlotsChanged.Invoke();
    }
#endif

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
            if (weaponPrefab == weaponSlots[i].weaponPrefab)
                return true;
        }
        return false;
    }

    public int GetWeaponIndex(WeaponController weaponPrefab) {
        for (int i = 0; i < weaponSlots.Length; i++)
            if (weaponPrefab == weaponSlots[i].weaponPrefab)
                return i;
        return -1;
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

    public bool SetSelectedWeapon(int index) {
        if (selectedWeapon != index && GetWeaponAvailability(index)) {
            selectedWeapon = index;
            return true;
        } else return false;
    }

    public void ResetAllAvailability() {
        selectedWeapon = -1;
        bool setSelected = false;
        for (int i = 0; i < weaponSlots.Length; i++) {
            weaponSlots[i].available = weaponSlots[i].giveAtStartup;
            ResetAmmo(weaponSlots[i].weaponPrefab);
            if (!setSelected && weaponSlots[i].giveAtStartup) {
                setSelected = true;
                selectedWeapon = i;
            }
        }
    }

    public void TakeAllAvailability() {
        selectedWeapon = -1;
        for (int i = 0; i < weaponSlots.Length; i++) {
            weaponSlots[i].available = false;
            ResetAmmo(weaponSlots[i].weaponPrefab);
        }
    }

    public void GiveAllAvailability() {
        if (selectedWeapon < 0) selectedWeapon = 0;
        for (int i = 0; i < weaponSlots.Length; i++) {
            weaponSlots[i].available = true;
            MaxOutAmmo(weaponSlots[i].weaponPrefab);
        }
    }

    private void ResetAmmo(WeaponController weapon) {
        if (weapon.primaryFire != null)
            if (weapon.primaryFire.ammoType != null)
                weapon.primaryFire.ammoType.currentAmount = 0;
        if (weapon.secondaryFire != null)
            if (weapon.secondaryFire.ammoType != null)
                weapon.secondaryFire.ammoType.currentAmount = 0;
    }

    private void MaxOutAmmo(WeaponController weapon) {
        if (weapon.primaryFire != null)
            if (weapon.primaryFire.ammoType != null)
                weapon.primaryFire.ammoType.currentAmount = weapon.primaryFire.ammoType.maxAmount;
        if (weapon.secondaryFire != null)
            if (weapon.secondaryFire.ammoType != null)
                weapon.secondaryFire.ammoType.currentAmount = weapon.secondaryFire.ammoType.maxAmount;
    }
}
