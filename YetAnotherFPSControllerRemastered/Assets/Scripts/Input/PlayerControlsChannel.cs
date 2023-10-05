using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using TMPro;


[CreateAssetMenu(menuName = "Scriptable Object/Player Controls Channel")]
public class PlayerControlsChannel : ScriptableObject {
    // --- Public properties
    [field: NonSerialized] public PlayerControls inputActions { get; private set; }
    [field: NonSerialized] public bool isBusy { get; private set; }

    // --- Events
    [NonSerialized] public UnityEvent RebindStarted;
    [NonSerialized] public UnityEvent RebindComplete;
    [NonSerialized] public UnityEvent RebindCanceled;
    [NonSerialized] public UnityEvent RebindReset;


    // --- ScriptableObject methods
    private void OnEnable() {
        // Istantiate PlayerControls and setup
        if (inputActions == null) {
            inputActions = new PlayerControls();
            RebindStarted = new UnityEvent();
            RebindComplete = new UnityEvent();
            RebindCanceled = new UnityEvent();
            RebindReset = new UnityEvent();
            isBusy = false;
        }

        // Load player preferences
        foreach (InputAction action in inputActions.asset)
            LoadBindOverride(action);
    }

    // --- PlayerControlsChannel methods
    public string GetBindingName(string actionName, int bindingIndex, InputBinding.DisplayStringOptions options = default) {
        InputAction action = inputActions.asset.FindAction(actionName);
        return action.GetBindingDisplayString(bindingIndex, options);
    }

    public void StartRebind(string actionName, int bindingIndex, TMP_Text statusText, bool excludeMouse) {
        // Abort if already busy
        if (isBusy) return;

        InputAction action = inputActions.asset.FindAction(actionName);
        if (action == null || action.bindings.Count <= bindingIndex) {
            Debug.LogError("InputManager.StartRebind has invalid binding index.");
            return;
        }

        // Handle composite bindings
        if (action.bindings[bindingIndex].isComposite) {
            var firstPartIndex = bindingIndex + 1;
            if (firstPartIndex < action.bindings.Count && action.bindings[firstPartIndex].isPartOfComposite) {
                DoRebind(action, bindingIndex, statusText, true, excludeMouse);
            }
        } else DoRebind(action, bindingIndex, statusText, false, excludeMouse);
    }

    public void ResetAllRebindings() {
        // Abort if already busy
        if (isBusy) return;

        foreach (InputAction action in inputActions.asset)
            ResetBindOverride(action);
        RebindReset.Invoke();
    }

    private void DoRebind(InputAction actionToRebind, int bindingIndex, TMP_Text statusText, bool allCompositeParts, bool excludeMouse) {
        if (actionToRebind == null || bindingIndex < 0)
            return;

        // Write feedback prompt
        statusText.text = $"Waiting for input ({actionToRebind.expectedControlType})...\nPress Backspace to abort.";

        // Setup rebind
        actionToRebind.Disable();
        var rebind = actionToRebind.PerformInteractiveRebinding(bindingIndex);
        rebind.OnComplete(operation => {
            // Do rebind
            actionToRebind.Enable();
            operation.Dispose();
            if (allCompositeParts) {
                var nextBindingIndex = bindingIndex + 1;
                if (nextBindingIndex < actionToRebind.bindings.Count && actionToRebind.bindings[nextBindingIndex].isPartOfComposite)
                    DoRebind(actionToRebind, nextBindingIndex, statusText, true, excludeMouse);
            }
            isBusy = false;
            statusText.text = "";
            SaveBindingOverride(actionToRebind);

            // Invoke events
            RebindComplete.Invoke();
        });
        rebind.OnCancel(operation => {
            // Cancel rebind
            actionToRebind.Enable();
            operation.Dispose();
            isBusy = false;
            statusText.text = "";

            // Invoke events
            RebindCanceled.Invoke();
        });
        rebind.WithCancelingThrough(Constants.INPUT_ESCAPE);
        rebind.WithCancelingThrough(Constants.INPUT_BACKSPACE);
        if (excludeMouse)
            rebind.WithControlsExcluding(Constants.INPUT_MOUSE);

        // Start rebind
        isBusy = true;
        RebindStarted.Invoke();
        rebind.Start(); // This call starts the rebinding process
    }

    private void ResetBindOverride(InputAction action) {
        for (int i = 0; i < action.bindings.Count; i++) {
            action.RemoveBindingOverride(i);
        }
        SaveBindingOverride(action);
    }

    private void LoadBindOverride(InputAction action) {
        for (int i = 0; i < action.bindings.Count; i++) {
            string prefString = PlayerPrefs.GetString(action.actionMap + action.name + i);
            if (!string.IsNullOrEmpty(prefString)) {
                action.ApplyBindingOverride(i, prefString);
                //Debug.Log("InputManager.LoadBindOverride: \"" + prefString + "\" override was applied.");
            }
        }
    }

    private void SaveBindingOverride(InputAction action) {
        for (int i = 0; i < action.bindings.Count; i++) {
            PlayerPrefs.SetString(action.actionMap + action.name + i, action.bindings[i].overridePath);
        }
    }
}
