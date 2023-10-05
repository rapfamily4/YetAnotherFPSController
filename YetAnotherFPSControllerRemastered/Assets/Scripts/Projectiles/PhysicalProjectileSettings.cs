using System;
using UnityEngine;
using UnityEngine.Events;


[CreateAssetMenu(menuName = "Scriptable Object/Projectile/Physical Projectile Settings")]
public class PhysicalProjectileSettings : ScriptableObject {
    // --- Inspector parameters
    [Header("Gameplay")]
    public float damage = 10;

    [Header("Collision Detection")]
    [Min(0f)] public float radius = 0.01f;
    public float root = 0f;
    public float tip = 0f;
    public LayerMask hittableLayers = -1;
    [Tooltip("The distance along which trajectory correction is applied. Not applied if it's less than 0.")]
    public float trajectoryCorrectionDistance = -1;

    [Header("Physics")]
    public float speed = 15f;
    public float gravityAcceleration = 0f;
    [Range(0f, 1f)] public float drag = 0f;
    [Tooltip("The number of bounces the projectile can perform. No limit if it's less than 0.")]
    public int numberOfBounces = 0;
    [Min(0f)] public float impactElasticity = 1f;

    [Header("Life Time")]
    [Min(0f)] public float timeToLive = 15f;
    public bool hitWhenTimeExpires = false;

    [Header("Particles")]
    public TrailPool trailPool;


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
