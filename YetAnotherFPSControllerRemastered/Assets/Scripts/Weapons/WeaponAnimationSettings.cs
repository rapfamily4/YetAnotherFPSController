using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable Object/WeaponAnimationSettings")]
public class WeaponAnimationSettings : ScriptableObject {
    [Header("View Sway")]
    [Min(0f)] public Vector3 viewSwayAmount = Vector3.one * 1f;
    [Min(0f)] public Vector3 viewSwayClamp = Vector3.one * 2f;
    [Min(0f)] public float viewSwaySmoothing = 0.1f;
    [Min(0f)] public float viewSwayResetSmoothing = 0.1f;
    public bool invertViewSwayX = false;
    public bool invertViewSwayY = false;
    public bool invertViewSwayZ = false;

    [Header("Movement Sway")]
    [Min(0f)] public Vector2 movementSwayAmount = Vector2.one * 2f;
    [Min(0f)] public float movementSwaySmoothing = 0.1f;
    [Min(0f)] public float movementSwayResetSmoothing = 0.1f;
    public bool invertMovementSwayX = true;
    public bool invertMovementSwayY = false;

    [Header("Movement Pan")]
    [Min(0f)] public Vector2 movementPanAmount = Vector2.one * 0.025f;
    [Min(0f)] public float movementPanSmoothing = 0.1f;
    [Min(0f)] public float movementPanResetSmoothing = 0.1f;
    public bool invertMovementPanX = true;
    public bool invertMovementPanY = true;
}
