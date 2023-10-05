using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Scriptable Object/Ammo")]
public class Ammo : ScriptableObject {
    public string ammoName = "Ammo Name";
    [Min(0)] public int maxAmount;
    public int currentAmount
    {
        get { return m_currentAmount; }
        set { 
            m_currentAmount = Mathf.Clamp(value, 0, maxAmount);
            AmmoAmountChanged?.Invoke(m_currentAmount);
        }
    }

    [NonSerialized] public UnityEvent<int> AmmoAmountChanged;

    [NonSerialized] private int m_currentAmount = 0;

    private void OnEnable() {
        hideFlags = HideFlags.DontUnloadUnusedAsset; // It'll ensure us that the SO won't be unloaded if unused
        AmmoAmountChanged = new UnityEvent<int>();
    }

    public void OnDisable() {
        AmmoAmountChanged = null;
    }
}
