using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable Object/Weapons/Animations/Weapon Movement Animation Settings")]
public class WeaponMovementAnimationSettings : ScriptableObject {
    [Header("Idle")]
    public float idleBobAmount = 0.0125f;
    public float idleSwayAmount = 1f;
    [Min(0f)] public float idleFrequency = 0.25f;
    [Min(0f)] public Vector3 idleNoiseAmount = Vector2.one;
    [Min(0f)] public float idleNoiseSpeed = 0.8f;

    [Header("Movement Bob")]
    [Min(0f)] public Vector3 movementBobTranslationAmount = Vector3.one * 0.0125f;
    public Vector3 movementBobRotationAmount = Vector3.one;
    [Min(0f)] public float movementBobSmoothing = 0.05f;
    [Min(0f)] public float movementBobResetSmoothing = 0.1f;

    [Header("Movement Pan")]
    [Min(0f)] public Vector2 movementPanAmount = Vector2.one * 0.025f;
    [Min(0f)] public float movementPanSmoothing = 0.1f;
    [Min(0f)] public float movementPanResetSmoothing = 0.1f;
    public bool invertMovementPanX = true;
    public bool invertMovementPanY = true;

    [Header("Movement Sway")]
    [Min(0f)] public Vector2 movementSwayAmount = Vector2.one * 2f;
    [Min(0f)] public float movementSwaySmoothing = 0.1f;
    [Min(0f)] public float movementSwayResetSmoothing = 0.1f;
    public bool invertMovementSwayX = true;
    public bool invertMovementSwayY = false;

    [Header("View Sway")]
    [Min(0f)] public Vector3 viewSwayAmount = Vector3.one * 1f;
    [Min(0f)] public Vector3 viewSwayClamp = Vector3.one * 2f;
    [Min(0f)] public float viewSwaySmoothing = 0.1f;
    [Min(0f)] public float viewSwayResetSmoothing = 0.1f;
    public bool invertViewSwayX = false;
    public bool invertViewSwayY = false;
    public bool invertViewSwayZ = false;

    [Header("Physics-based Animations")]
    [Min(0f)] public float elasticity = 100f;
    [Min(0f)] public float damping = 8f;
    [Min(0f)] public float jumpVelocity = 1.25f;
    [Min(0f)] public float landingVelocity = 0.75f;
    [Min(0f)] public float thrustVelocity = 1.25f;
    [Tooltip("The degrees by which the weapon is rotated on the local X axis, " +
             "for each unit the weapon is displaced on its local Y axis.")]
    public float rotationAmountOnVerticalAxis = 70f;
    [Tooltip("The degrees by which the weapon is rotated on the local axes, " +
             "for each unit the weapon is displaced on its local X and Z axes; " +
             "displacements on X will rotate on Y and Z, while displacements on " +
             "Z will rotate on X.")]
    public Vector3 rotationAmountOnHorizontalPlane = Vector3.one * 35f;
}
