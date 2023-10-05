using UnityEngine;

public class WeaponPickupController : MonoBehaviour
{
    public WeaponController weaponPrefab;

    private void OnTriggerEnter(Collider other) {
        if (other.tag != Constants.TAG_PLAYER)
            return;
        PlayerWeaponInventoryController inventoryController = other.GetComponentInChildren<PlayerWeaponInventoryController>();
        if (inventoryController != null) {
            inventoryController.PickUpWeapon(weaponPrefab);
            Destroy(gameObject);
        }
    }
}
