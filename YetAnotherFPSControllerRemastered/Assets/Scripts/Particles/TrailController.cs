using System;
using UnityEngine;
using UnityEngine.Pool;

public class TrailController : MonoBehaviour
{
    [HideInInspector] public LinkedPool<TrailController> trailPool;

    private TrailRenderer m_trailRenderer;
    private bool m_releaseTimerEnabled;
    private float m_releaseTimer;

    private void Awake() {
        m_trailRenderer = GetComponent<TrailRenderer>();
    }

    private void OnEnable() {
        m_releaseTimerEnabled = false;
        m_releaseTimer = 0f;
    }

    private void Update() {
        if (m_releaseTimerEnabled) {
            m_releaseTimer += Time.deltaTime;
            if (m_trailRenderer == null || m_releaseTimer >= m_trailRenderer.time)
                trailPool.Release(this);
        }
    }

    public void ResetToPosition(Vector3 position) {
        transform.position = position;
        m_trailRenderer.Clear();
    }

    public void AddPosition(Vector3 endPosition) {
        m_trailRenderer.AddPosition(endPosition);
    }

    public void BeginRelease() {
        m_releaseTimerEnabled = true;
    }
}
