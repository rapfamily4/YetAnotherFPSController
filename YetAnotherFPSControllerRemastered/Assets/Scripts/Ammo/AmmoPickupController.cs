using UnityEngine;

public class AmmoPickupController : MonoBehaviour
{
    public Ammo ammoResource;
    [Min(0)] public int ammoOnPickup;

    private void OnTriggerEnter(Collider other) {
        if (other.tag != Constants.TAG_PLAYER)
            return;
        if (ammoResource != null) {
            ammoResource.currentAmount += ammoOnPickup;
            Destroy(gameObject);
        }
    }
}
