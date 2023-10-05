/** Luigi Rapetta, 2023. Based on FPS Microgame by Unity Technologies **/

using UnityEngine;


public abstract class HealthControllerBase : MonoBehaviour {
    // --- StartupBehaviour definition
    private enum StartupBehaviour {
        Keep,
        Reset
    }

    // --- Members
    public abstract HealthObject healthObject { get; }
    [SerializeField] private StartupBehaviour startupBehaviour = StartupBehaviour.Reset;
    private bool m_isDead = false;


    // --- Methods
    private void Awake() {
        switch (startupBehaviour) {
            case StartupBehaviour.Keep:
                Debug.Log(gameObject.name + "'s health kept as " + healthObject.currentHealth);
                break;

            case StartupBehaviour.Reset:
                healthObject.currentHealth = healthObject.maxHealth;
                Debug.Log(gameObject.name + "'s health reset to " + healthObject.currentHealth);
                break;
        }
    }

    public void Heal(float healAmount) {
        // Do heal
        float previousHealth = healthObject.currentHealth;
        healthObject.currentHealth += healAmount;
        healthObject.currentHealth = Mathf.Clamp(healthObject.currentHealth, 0f, healthObject.maxHealth);

        // Call event
        float trueHealAmount = healthObject.currentHealth - previousHealth;
        if (trueHealAmount > 0f)
            healthObject.OnHealed?.Invoke(trueHealAmount);
    }

    public void TakeDamage(float damage, GameObject damageSource) {
        // Abort if invincible
        if (healthObject.invulnerable) return;

        // Deal damage
        float previousHealth = healthObject.currentHealth;
        healthObject.currentHealth -= damage;
        healthObject.currentHealth = Mathf.Clamp(healthObject.currentHealth, 0f, healthObject.maxHealth);

        // Call event
        float trueDamageAmount = previousHealth - healthObject.currentHealth;
        if (trueDamageAmount > 0f)
            healthObject.OnDamaged?.Invoke(trueDamageAmount, damageSource);

        // Handle death if health reaches zero
        HandleDeath();
    }

    private void HandleDeath() {
        // Abort if already dead
        if (m_isDead) return;

        // Call event and set as dead
        if (healthObject.currentHealth <= 0f) {
            m_isDead = true;
            healthObject.OnDie?.Invoke();
        }
    }
}