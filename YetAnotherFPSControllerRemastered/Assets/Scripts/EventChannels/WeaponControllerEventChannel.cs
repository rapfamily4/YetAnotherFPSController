using System;
using UnityEngine;
using UnityEngine.Events;


[CreateAssetMenu(menuName = "Scriptable Object/Event Channel/Weapon Controller Event Channel")]
public class WeaponControllerEventChannel : ScriptableObject {
    // --- Event
    [NonSerialized] public UnityEvent<WeaponController> WeaponControllerEvent;


    // --- ScriptableObject methods
    public void OnEnable() {
        WeaponControllerEvent = new UnityEvent<WeaponController>();
    }

    public void OnDisable() {
        WeaponControllerEvent = null;
    }
}
