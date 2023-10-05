using System;
using UnityEngine;
using UnityEngine.Events;


[CreateAssetMenu(menuName = "Scriptable Object/Event Channel/Void Event Channel")]
public class VoidEventChannel : ScriptableObject {
    // --- Event
    [NonSerialized] public UnityEvent VoidEvent;


    // --- ScriptableObject methods
    public void OnEnable() {
        VoidEvent = new UnityEvent();
    }

    public void OnDisable() {
        VoidEvent = null;
    }
}
