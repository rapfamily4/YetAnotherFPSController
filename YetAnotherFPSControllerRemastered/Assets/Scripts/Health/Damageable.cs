using UnityEngine;


public class Damageable : MonoBehaviour {
    [Tooltip("Multiplier to apply to the received damage.")]
    public float damageMultiplier = 1f;
    [Tooltip("Multiplier to apply to self damage.")]
    public float selfDamageMultiplier = 1f;
    
    public HealthControllerBase health { get; private set; }


    void Awake() {
        // Find Health either at the same level, or higher in the hierarchy
        health = GetComponent<HealthControllerBase>();
        if (!health)
            health = GetComponentInParent<HealthControllerBase>();
    }

    public void InflictDamage(float damage, GameObject source) {
        // Abort if no Health component was found
        if (!health) return;

        // Compute damage to deal
        float totalDamage = damage;
        totalDamage *= damageMultiplier;
        if (health.gameObject == source)
            totalDamage *= selfDamageMultiplier;

        // Deal damage
        health.TakeDamage(totalDamage, source);
    }
}
