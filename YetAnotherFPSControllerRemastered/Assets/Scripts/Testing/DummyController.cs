using UnityEngine;

public class DummyController : MonoBehaviour {
    [Min(0f)] public float timeBeforeHealthRestores = 5f;
    [Min(0f)] public float healthRestoredPerSecond = 50f;

    private HealthControllerBase m_healthController;
    private TMPro.TMP_Text m_displayer;
    private float m_heathRestoreCounter;

    private void Awake() {
        m_healthController = GetComponent<HealthControllerBase>();
        m_displayer = GetComponentInChildren<TMPro.TMP_Text>();
    }

    private void OnEnable() {
        m_healthController.healthObject.OnDamaged?.AddListener(UpdateDisplayer);
        m_healthController.healthObject.OnDamaged?.AddListener(ResetHealthRestoreCounter);
        m_healthController.healthObject.OnHealed?.AddListener(UpdateDisplayer);
    }

    private void OnDisable() {
        m_healthController.healthObject.OnDamaged?.RemoveListener(UpdateDisplayer);
        m_healthController.healthObject.OnDamaged?.RemoveListener(ResetHealthRestoreCounter);
        m_healthController.healthObject.OnHealed?.RemoveListener(UpdateDisplayer);
    }

    private void Start() {
        m_heathRestoreCounter = timeBeforeHealthRestores;
        OnEnable(); // If this script's Awake ran before HealthController's, try now to subscribe to events.
        UpdateDisplayer(-1f);
    }

    private void Update() {
        if (m_heathRestoreCounter < timeBeforeHealthRestores) {
            m_heathRestoreCounter += Time.deltaTime;
            Mathf.Clamp(m_heathRestoreCounter, 0f, timeBeforeHealthRestores);
        } else if (m_healthController.healthObject.currentHealth < m_healthController.healthObject.maxHealth)
            m_healthController.Heal(healthRestoredPerSecond * Time.deltaTime);
    }

    private void ResetHealthRestoreCounter(float amount, GameObject source) {
        m_heathRestoreCounter = 0f;
    }

    private void UpdateDisplayer(float amount) {
        UpdateDisplayer(amount, null);
    }

    private void UpdateDisplayer(float amount, GameObject source) {
        m_displayer.text = Mathf.CeilToInt(m_healthController.healthObject.currentHealth).ToString();
    }
}
