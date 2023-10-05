using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerCameraController : MonoBehaviour {
    // --- Inspector parameters
    [Header("General")]
    [Range(0f, 1f)] public float height = 0.85f;
    [Tooltip("The degrees by which the camera is rotated for each unit it's displaced along its local X, Y and Z axes. " +
             "Displacements along X rotate around Z, while displacements along Y and Z will rotate along X.")]
    public Vector3 rotationOnDisplacement = new Vector3(-8f, 8f, 8f);

    [Header("Push")]
    [Min(0f)] public float pushSmoothness = 0.05f;
    [Min(0f)] public float pushResetSmoothness = 0.15f;

    [Header("Recoil")]
    [Min(0f)] public float recoilSmoothness = 0.05f;
    [Min(0f)] public float recoilResetSmoothness = 0.15f;

    [Header("Shake")]
    public float shakeSpeed = 2.5f;
    public float shakeSmoothness = 0.05f;
    public float shakeResetSpeed = 0.35f;

    [Header("Debug")]
    public bool printDebugInfo = false;


    // --- Public interface
    public float pitch { 
        get { return m_cameraHolderRotation.x; }
        set { m_cameraHolderRotation.x = Mathf.Clamp(value, -90f, 90f); }
    }
    public float yaw {
        get { return m_cameraHolderRotation.y; }
        set { m_cameraHolderRotation.y = value % 360f; }
    }

    public Transform baseCamera {get; private set;}

    // --- Private members
    private CapsuleCollider m_parentCollider;
    private Vector3 m_cameraHolderRotation = Vector3.zero;
    // Push (GLOBAL)
    private Vector3 m_push = Vector3.zero;
    private Vector3 m_pushTarget = Vector3.zero;
    private Vector3 m_pushVelocity = Vector3.zero;
    private Vector3 m_pushResetVelocity = Vector3.zero;
    // Shake (LOCAL)
    private Vector3 m_shakeDisplacement = Vector3.zero;
    private Vector3 m_shakeDisplacementTarget = Vector3.zero;
    private Vector3 m_shakeDisplacementVelocity = Vector3.zero;
    private Vector3 m_shakeSeeds = Vector3.zero;
    private float m_currentShakeAmount = 0f;
    // Recoil (LOCAL)
    private Vector3 m_recoil = Vector3.zero;
    private Vector3 m_recoilTarget = Vector3.zero;
    private Vector3 m_recoilVelocity = Vector3.zero;
    private Vector3 m_recoilResetVelocity = Vector3.zero;


    // --- MonoBehaviour methods
    private void Awake() {
        // Fetch references
        m_parentCollider = transform.parent.GetComponent<CapsuleCollider>();
        foreach (Camera cam in transform.parent.GetComponentsInChildren<Camera>())
            if (cam.GetUniversalAdditionalCameraData().renderType == CameraRenderType.Base)
                baseCamera = cam.transform;
    }

    private void LateUpdate() {
        // Push
        m_push = Vector3.SmoothDamp(m_push, m_pushTarget, ref m_pushVelocity, pushSmoothness);
        m_pushTarget = Vector3.SmoothDamp(m_pushTarget, Vector3.zero, ref m_pushResetVelocity, pushResetSmoothness);

        // Recoil
        m_recoil = Vector3.SmoothDamp(m_recoil, m_recoilTarget, ref m_recoilVelocity, recoilSmoothness);
        m_recoilTarget = Vector3.SmoothDamp(m_recoilTarget, Vector3.zero, ref m_recoilResetVelocity, recoilResetSmoothness);

        // Compute noise to simulate camera shake
        if (m_currentShakeAmount > 0) {
            float noiseCoord = Time.timeSinceLevelLoad * shakeSpeed;
            m_shakeDisplacementTarget.x = Mathf.PerlinNoise(noiseCoord + m_shakeSeeds.x, noiseCoord + m_shakeSeeds.x);
            m_shakeDisplacementTarget.y = Mathf.PerlinNoise(noiseCoord + m_shakeSeeds.y, noiseCoord + m_shakeSeeds.y);
            m_shakeDisplacementTarget.z = Mathf.PerlinNoise(noiseCoord + m_shakeSeeds.z, noiseCoord + m_shakeSeeds.z);
            if (m_shakeDisplacementTarget.x >= 0.5f)
                m_shakeDisplacementTarget.x = MathUtils.ConvertToNewRange(m_shakeDisplacementTarget.x, 0.5f, 1f, 0.125f, 1f);
            else
                m_shakeDisplacementTarget.x = MathUtils.ConvertToNewRange(m_shakeDisplacementTarget.x, 0f, 0.5f, -1f, -0.125f);
            if (m_shakeDisplacementTarget.y >= 0.5f)
                m_shakeDisplacementTarget.y = MathUtils.ConvertToNewRange(m_shakeDisplacementTarget.y, 0.5f, 1f, 0.125f, 1f);
            else
                m_shakeDisplacementTarget.y = MathUtils.ConvertToNewRange(m_shakeDisplacementTarget.y, 0f, 0.5f, -1f, -0.125f);
            if (m_shakeDisplacementTarget.z >= 0.5f)
                m_shakeDisplacementTarget.z = MathUtils.ConvertToNewRange(m_shakeDisplacementTarget.z, 0.5f, 1f, 0.125f, 1f);
            else
                m_shakeDisplacementTarget.z = MathUtils.ConvertToNewRange(m_shakeDisplacementTarget.z, 0f, 0.5f, -1f, -0.125f);
            m_shakeDisplacementTarget *= m_currentShakeAmount;
            m_currentShakeAmount -= shakeResetSpeed * Time.deltaTime;
        } else {
            m_currentShakeAmount = 0f;
            m_shakeDisplacementTarget = Vector3.zero;
        }
        m_shakeDisplacement = Vector3.SmoothDamp(m_shakeDisplacement, m_shakeDisplacementTarget, ref m_shakeDisplacementVelocity, shakeSmoothness);

        // Update camera holder's global position and rotation
        transform.SetPositionAndRotation(
            m_parentCollider.transform.position + m_parentCollider.transform.up * m_parentCollider.height * height + m_push,
            Quaternion.Euler(m_cameraHolderRotation)
        );
        
        // Base camera position
        Vector3 newBaseCameraPosition = m_shakeDisplacement;

        // Base camera rotation
        Vector3 newBaseCameraRotation = Vector3.zero;
        // Rotation on push
        Vector3 localPush = baseCamera.InverseTransformVector(m_push);
        newBaseCameraRotation.z = localPush.x * rotationOnDisplacement.x;
        newBaseCameraRotation.x = localPush.z * rotationOnDisplacement.z + localPush.y * rotationOnDisplacement.y;
        // Recoil
        newBaseCameraRotation += m_recoil;
        // Shake
        newBaseCameraRotation.z += m_shakeDisplacement.x * rotationOnDisplacement.x;
        newBaseCameraRotation.x += m_shakeDisplacement.z * rotationOnDisplacement.z + m_shakeDisplacement.y * rotationOnDisplacement.y;

        // Apply new base camera transform
        baseCamera.SetLocalPositionAndRotation(newBaseCameraPosition, Quaternion.Euler(newBaseCameraRotation));
    }

    void OnGUI() {
        if (!printDebugInfo) return;

        string state = "shakeDisplacement: " + m_shakeDisplacement +
                       "\nshakeDisplacementTarget: " + m_shakeDisplacement;
        GUILayout.Label($"<color='black'><size=14>{state}</size></color>");
    }


    // --- PlayerCameraController methods
    public void RotateCamera(Vector2 delta) {
        // Update camera angles
        pitch += delta.y * Time.deltaTime;
        yaw += delta.x * Time.deltaTime;
    }

    public void PushCameraGlobal(Vector3 globalTarget, bool force = false) {
        if (force)
            m_pushTarget = globalTarget;
        else
            m_pushTarget += globalTarget;
    }

    public void RecoilCamera(Vector3 recoilAmount, bool force = false) {
        if (force)
            m_recoilTarget = recoilAmount;
        else
            m_recoilTarget += recoilAmount;
    }

    public void ShakeCamera(float shakeAmount) {
        m_currentShakeAmount += shakeAmount;
        m_shakeSeeds.x = UnityEngine.Random.Range(0f, 100f);
        m_shakeSeeds.y = UnityEngine.Random.Range(0f, 100f);
        m_shakeSeeds.z = UnityEngine.Random.Range(0f, 100f);
    }
}
