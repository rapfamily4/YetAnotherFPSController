/** Luigi Rapetta, 2023 **/

using System;
using UnityEngine.Events;
using UnityEngine;
using System.Diagnostics;
using UnityEditor.Rendering;

[Serializable]
public class HealthObject {
    [Min(0f)] public float maxHealth = 100f;
    [Range(0f, 1f)] public float criticalHealthRatio = 0.3f;
    public bool invulnerable = false;

    [NonSerialized, HideInInspector, Min(0f)] public float currentHealth;
    public float currentRatio => currentHealth / maxHealth;
    public bool canHeal => currentHealth < maxHealth;
    public bool isCritical => currentRatio <= criticalHealthRatio;

    [NonSerialized, HideInInspector] public UnityEvent<float, GameObject> OnDamaged;
    [NonSerialized, HideInInspector] public UnityEvent<float> OnHealed;
    [NonSerialized, HideInInspector] public UnityEvent OnDie;


    public HealthObject() {
        OnDamaged = new UnityEvent<float, GameObject>();
        OnHealed = new UnityEvent<float>();
        OnDie = new UnityEvent();
    }

    ~HealthObject() {
        OnDamaged = null;
        OnHealed = null;
        OnDie = null;
    }
}