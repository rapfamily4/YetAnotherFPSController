using System;
using UnityEngine;
using UnityEngine.Events;


[CreateAssetMenu(menuName = "Scriptable Object/Projectile/Hitscan Projectile Settings")]
public class HitscanProjectileSettings : ScriptableObject {
    // --- Inspector parameters
    [Header("Gameplay")]
    public float damage = 10;

    [Header("Collision Detection")]
    public LayerMask hittableLayers = -1;
    [Tooltip("It states how far the raycast will extend to.")]
    [Min(0f)] public float maxDistance = 1000f;
    [Tooltip("The distance along which trajectory correction is applied. Not applied if it's less than 0.")]
    public float trajectoryCorrectionDistance = -1;

    [Header("Particles")]
    public LinePool linePool;


    // --- OnValidate handling
#if UNITY_EDITOR
    [NonSerialized] public UnityEvent ProjectileSettingsChanged;

    public void OnEnable() {
        ProjectileSettingsChanged = new UnityEvent();
    }

    public void OnDisable() {
        ProjectileSettingsChanged = null;
    }

    private void OnValidate() {
        ProjectileSettingsChanged?.Invoke();
    }
#endif
}
