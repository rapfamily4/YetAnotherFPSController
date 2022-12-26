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
        // Update representation of the serialized object
        serializedObject.Update();

        // Weapon Slots
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(weaponSlots);
        if (EditorGUI.EndChangeCheck() && Application.isPlaying)
            EventManager.TriggerEvent(Constants.EVENT_WEAPONSLOTSMODIFIED);

        // Apply changes onto the serialized object
        serializedObject.ApplyModifiedProperties();
    }
}
