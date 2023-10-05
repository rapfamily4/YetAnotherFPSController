using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable Object/Weapons/Animations/Weapon Gameplay Animation Settings")]
public class WeaponGameplayAnimationSettings : ScriptableObject {
    [Header("Switching")]
    public Vector3 switchingUnequipPosition = -Vector3.one * 0.25f;
    public Vector3 switchingUnequipRotation = new Vector3(15f, -15f, 15f);
    [Min(0f)] public float switchingTime = 0.2f;
    [Min(0f)] public float switchingSmoothing = 0.055f;

    [Header("Recoil Backwards")]
    [Min(0f)] public float primaryRecoilAmount = 0.075f;
    [Min(0f)] public float secondaryRecoilAmount = 0.075f;
    [Min(0f)] public float maxRecoilDisplacement = 0.1f;
    [Min(0f)] public float recoilSmoothing = 0.125f;
    [Min(0f)] public float recoilResetSmoothing = 0.4f;

    [Header("Recoil Noise")]
    [Min(0f)] public Vector2 noiseAmount = Vector2.one * 0.075f;
    [Min(0f)] public float noiseSpeed = 20f;
    [Min(1f)] public float noiseFalloff = 2f;

    [Header("Recoil Camera Effects")]
    [Min(0f)] public float primaryRecoilCameraPushback = 0f;
    [Min(0f)] public float secondaryRecoilCameraPushback = 0f;
}
