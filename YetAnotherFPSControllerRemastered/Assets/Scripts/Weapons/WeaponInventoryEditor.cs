using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(WeaponInventory))]
[CanEditMultipleObjects]
public class WeaponInventoryEditor : Editor {
    // --- Properties
    SerializedProperty weaponSlots;

    // --- Editor methods
    void OnEnable() {
        weaponSlots = serializedObject.FindProperty(Constants.EDITORPROP_WEAPONSLOTS);
    }

    public override void OnInspectorGUI() {
        // Update the representation of the serialized object
        serializedObject.Update();

        // Weapon Slots
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(weaponSlots);
        if (EditorGUI.EndChangeCheck()) {
            // Immediately apply any edits in order to instantiate weapons coherently
            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying)
                EventManager.TriggerEvent(Constants.EVENT_WEAPONSLOTSMODIFIED);
        }

        // Apply any modified properties
        serializedObject.ApplyModifiedProperties();
    }
}
