using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Pool;

public class LineController : MonoBehaviour
{
    [Min(0f)] public float time = 1f;
    [Tooltip("Will the color and alpha change over time? Please note that, due to how LineRenderer's properties are accessed, " +
             "this operation will generate garbage.")]
    public bool animateColorOverTime = true;
    public AnimationCurve sizeOverTime;
    public Gradient colorOverTime;

    [HideInInspector] public LinkedPool<LineController> linePool;

    private LineRenderer m_lineRenderer;
    private bool m_releaseTimerEnabled;
    private float m_releaseTimer;
    private float m_originalWidthMultiplier;
    private GradientColorKey[] m_originalColorKeys;
    private GradientAlphaKey[] m_originalAlphaKeys;

    private void Awake() {
        m_lineRenderer = GetComponent<LineRenderer>();
        if (m_lineRenderer != false) {
            // Note that Gradient.colorKeys is a property that is calling a native method.
            // This does create a new array each time it is read since the actual keys have
            // to be copied from the native side into managed memory.
            //
            // Source: https://forum.unity.com/threads/change-color-keys-on-a-gradient.436179/#post-8121782
            m_originalWidthMultiplier = m_lineRenderer.widthMultiplier;
            m_originalColorKeys = m_lineRenderer.colorGradient.colorKeys;
            m_originalAlphaKeys = m_lineRenderer.colorGradient.alphaKeys;
        }
    }

    private void OnEnable() {
        m_releaseTimerEnabled = false;
        m_releaseTimer = 0f;
    }

    private void Update() {
        if (m_releaseTimerEnabled) {
            if (m_lineRenderer != null) {
                float currentTime = m_releaseTimer / time;
                m_lineRenderer.widthMultiplier = m_originalWidthMultiplier * sizeOverTime.Evaluate(currentTime);

                if (animateColorOverTime) {
                    GradientColorKey[] colorKeys = new GradientColorKey[m_originalColorKeys.Length];
                    for (int i = 0; i < colorKeys.Length; i++)
                        colorKeys[i].color = m_originalColorKeys[i].color * colorOverTime.Evaluate(currentTime);
                    GradientAlphaKey[] alphaKeys = new GradientAlphaKey[m_originalAlphaKeys.Length];
                    for (int i = 0; i < alphaKeys.Length; i++)
                        alphaKeys[i].alpha = m_originalAlphaKeys[i].alpha * colorOverTime.Evaluate(currentTime).a;
                    // Apparently, also LineRenderer.colorGradient is calling a native method.
                    // This does create a new object each time it is read.
                    Gradient newGradient = m_lineRenderer.colorGradient;
                    newGradient.SetKeys(colorKeys, alphaKeys);
                    m_lineRenderer.colorGradient = newGradient;
                }
            }
            m_releaseTimer += Time.deltaTime;
            if (m_lineRenderer == null || m_releaseTimer >= time)
                linePool.Release(this);
        }
    }

    public void AnimateLine(Vector3 startPoint, Vector3 endPoint) {
        m_releaseTimerEnabled = true;
        m_lineRenderer.SetPosition(0, startPoint);
        m_lineRenderer.SetPosition(1, endPoint);
        m_lineRenderer.widthMultiplier = m_originalWidthMultiplier;
        if (animateColorOverTime)
            m_lineRenderer.colorGradient.SetKeys(m_originalColorKeys, m_originalAlphaKeys);
    }
}
