using System;
using UnityEngine;
using UnityEngine.Events;


[CreateAssetMenu(menuName = "Scriptable Object/Event Channel/Integer Event Channel")]
public class IntegerEventChannel : ScriptableObject {
    // --- Event
    [NonSerialized] public UnityEvent<int> IntegerEvent;


    // --- ScriptableObject methods
    public void OnEnable() {
        IntegerEvent = new UnityEvent<int>();
    }

    public void OnDisable() {
        IntegerEvent = null;
    }
}
