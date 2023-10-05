/** Luigi Rapetta, 2023 **/

using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour {
    // --- Inspector parameters
    [Header("Resources")]
    public SerializableHealthObject serializableHealthObject;
    public WeaponInventory weaponInventory;
    
    [Header("Event Channels")]
    public WeaponControllerEventChannel weaponEnabledEventChannel;
    public WeaponControllerEventChannel weaponDisabledEventChannel;
    
    [Header("HUD References")]
    public TMP_Text healthDisplay;
    public TMP_Text primaryAmmoLabel;
    public TMP_Text primaryAmmoDisplay;
    public TMP_Text secondaryAmmoLabel;
    public TMP_Text secondaryAmmoDisplay;


    // --- MonoBehaviour methods
    private void OnEnable() {
        serializableHealthObject.healthObject.OnHealed.AddListener(UpdateHealth);
        serializableHealthObject.healthObject.OnDamaged.AddListener(UpdateHealth);
        weaponEnabledEventChannel.WeaponControllerEvent.AddListener(InitializeAmmoDisplay);
        weaponDisabledEventChannel.WeaponControllerEvent.AddListener(HideAmmoDisplay);
    }

    private void OnDisable() {
        serializableHealthObject.healthObject.OnHealed.RemoveListener(UpdateHealth);
        serializableHealthObject.healthObject.OnDamaged.RemoveListener(UpdateHealth);
        weaponEnabledEventChannel.WeaponControllerEvent.RemoveListener(InitializeAmmoDisplay);
        weaponDisabledEventChannel.WeaponControllerEvent.RemoveListener(HideAmmoDisplay);
    }

    private void Start() {
        UpdateHealth(-1f);
        if (weaponInventory.selectedWeapon < 0)
            HideAmmoDisplay(null);
    }

    // --- HUDController methods
    private void UpdateHealth(float amount) {
        UpdateHealth(amount, null);
    }

    private void UpdateHealth(float amount, GameObject source) {
        healthDisplay.text = Mathf.CeilToInt(serializableHealthObject.healthObject.currentHealth).ToString();
    }

    private void HideAmmoDisplay(WeaponController weapon) {
        primaryAmmoLabel.enabled = false;
        primaryAmmoDisplay.enabled = false;
        secondaryAmmoLabel.enabled = false;
        secondaryAmmoDisplay.enabled = false;
        if (weapon != null) {
            if (weapon.primaryFire.ammoType != null)
                weapon.primaryFire.ammoType.AmmoAmountChanged.RemoveListener(UpdatePrimaryAmmoDisplay);
            if (weapon.secondaryFire.ammoType != null) {
                if (weapon.secondaryFire.ammoType != weapon.primaryFire.ammoType)
                    weapon.secondaryFire.ammoType.AmmoAmountChanged.RemoveListener(UpdateSecondaryAmmoDisplay);
                else
                    weapon.secondaryFire.ammoType.AmmoAmountChanged.RemoveListener(UpdatePrimaryAmmoDisplay);
            }
        }
    }

    private void InitializeAmmoDisplay(WeaponController weapon) {
        if (weapon == null)
            return;
        else {
            // Reset to default state
            primaryAmmoLabel.enabled = false;
            primaryAmmoDisplay.enabled = false;
            secondaryAmmoLabel.enabled = false;
            secondaryAmmoDisplay.enabled = false;
        }
        if (weapon.primaryFire.ammoType != null) {
            primaryAmmoLabel.enabled = true;
            primaryAmmoDisplay.enabled = true;
            weapon.primaryFire.ammoType.AmmoAmountChanged.AddListener(UpdatePrimaryAmmoDisplay);
            UpdatePrimaryAmmoDisplay(weapon.primaryFire.ammoType.currentAmount);
        }
        if (weapon.secondaryFire.ammoType != null) {
            if (weapon.secondaryFire.ammoType != weapon.primaryFire.ammoType) {
                secondaryAmmoLabel.enabled = true;
                secondaryAmmoDisplay.enabled = true;
                weapon.secondaryFire.ammoType.AmmoAmountChanged.AddListener(UpdateSecondaryAmmoDisplay);
                UpdateSecondaryAmmoDisplay(weapon.secondaryFire.ammoType.currentAmount);
            } else
                // Share label and display with primary's
                weapon.secondaryFire.ammoType.AmmoAmountChanged.AddListener(UpdatePrimaryAmmoDisplay);
        }
    }

    private void UpdatePrimaryAmmoDisplay(int amount) {
        primaryAmmoDisplay.text = amount.ToString();
    }

    private void UpdateSecondaryAmmoDisplay(int amount) {
        secondaryAmmoDisplay.text = amount.ToString();
    }
}
