using UnityEngine;
using UnityEngine.SceneManagement;

public class DamageTester : MonoBehaviour {
    public float damageDealtOnEnter = 10f;
    public float damageDealtPerSecond = 0f;

    private void OnTriggerEnter(Collider other) {
        other.GetComponentInChildren<Damageable>()?.InflictDamage(damageDealtOnEnter, gameObject);
    }

    private void OnTriggerStay(Collider other) {
        other.GetComponentInChildren<Damageable>()?.InflictDamage(damageDealtPerSecond * Time.deltaTime, gameObject);
    }
}
