// Luigi Rapetta, 2023
// Special thanks to Simone Bozzardi

using System;
using UnityEngine;
using UnityEngine.Events;


public enum WeaponFireType {
    SemiAutomatic,
    Automatic
}

public struct FireState {
    public bool locked;              // Is the firemode locked? e.g.: firemode locked if weapon is raising or lowering.
    public bool waiting;             // Is the firemode waiting other firemodes to finish?
    public bool buffered;            // Is the input for this firemode buffered?
    public float delayCounter;       // Elapsed time from the start of the fire sequence
    public float burstDelayCounter;  // Elapsed time from the start of the single fire being shot
    public int burstShotsCounter;    // Counter of shots fired within this fire sequence
}

[CreateAssetMenu(menuName = "Scriptable Object/Weapons/Weapon Fire Mode")]
public class WeaponFireMode : ScriptableObject {
    // --- Inspector parameters
    [Header("Fire")]
    public WeaponFireType type = WeaponFireType.SemiAutomatic;
    public ProjectilePool projectilePool;
    [Min(0)] public int projectilesFired = 1;
    public float spreadAngle = 0f;
    public float delay = 0.5f;

    [Header("Ammo")]
    public Ammo ammoType;
    [Min(0)] public int ammoPerShot = 1;

    [Header("Interrupt behaviour")]
    public bool canInterruptOtherFireMode = false;

    [Header("Burst")]
    [Min(1)] public int burstShots = 1;
    public float burstDelay = 0.1f;

    // --- Public members
    [NonSerialized] public FireState state;

    // --- Public properties
    public bool isBusy => state.burstShotsCounter < burstShots || state.burstDelayCounter < burstDelay || state.delayCounter < delay;

    // --- Events
    [NonSerialized] public UnityEvent<WeaponFireMode> Fired;
    [NonSerialized] public UnityEvent<WeaponFireMode> PutOtherFireModeToWait;
    [NonSerialized] public UnityEvent<WeaponFireMode> Finished;


    // --- ScriptableObject methods
    public void OnEnable() {
        Fired = new UnityEvent<WeaponFireMode>();
        PutOtherFireModeToWait = new UnityEvent<WeaponFireMode>();
        Finished = new UnityEvent<WeaponFireMode>();
    }

    public void OnDisable() {
        Fired = null;
        PutOtherFireModeToWait = null;
        Finished = null;
    }

    // --- WeaponFireMode methods
    public void ResetState() {
        state.locked = true;
        state.waiting = false;
        state.buffered = false;
        state.delayCounter = delay;
        state.burstDelayCounter = burstDelay;
        state.burstShotsCounter = burstShots;
    }

    public void Trigger(bool inputDown, bool inputHeld, bool inputUp) {
        state.buffered = inputDown || inputHeld;
        HandleInput(inputDown, inputHeld, inputUp);
    }

    public void Update() {
        // Perform fire on held input
        if (state.buffered) {
            if (type == WeaponFireType.SemiAutomatic)
                HandleInput(true, false, false);
            else HandleInput(false, true, false);
        }

        // Handle burst sequence
        TryShootSingle();

        if (isBusy) {
            // Update delay counters
            if (state.delayCounter < delay) {
                state.delayCounter += Time.deltaTime;
                if (state.delayCounter > delay)
                    state.delayCounter = delay;
            }
            if (state.burstDelayCounter < burstDelay) {
                state.burstDelayCounter += Time.deltaTime;
                if (state.burstDelayCounter > burstDelay)
                    state.burstDelayCounter = burstDelay;
            }

            // Inform listeners the firemode has finished the fire sequence.
            // NOTE: The fire sequence won't be truly finished until any buffered input is treated.
            if (!isBusy && !state.waiting && !state.buffered) {
                //Debug.Log(name + " has finished its fire sequence: invoke Finished!");
                Finished?.Invoke(this);
            }
        }
    }

    public void PutToWait() {
        state.burstShotsCounter = burstShots;
        state.waiting = true;
    }

    private void HandleInput(bool inputDown, bool inputHeld, bool inputUp) {
        switch (type) {
            case WeaponFireType.SemiAutomatic:
                if (inputDown)
                    TryBeginFireSequence();
                break;

            case WeaponFireType.Automatic:
                if (inputDown || inputHeld)
                    TryBeginFireSequence();
                break;
        }
    }

    // It tries to begin a fire "sequence": a fire sequence can be composed of a burst of shots.
    private void TryBeginFireSequence() {
        // Return if weapon is not ready to fire
        if (state.locked || (state.waiting && !canInterruptOtherFireMode) || state.delayCounter < delay) return;

        // Reset burst shots counter and try to shoot
        state.burstShotsCounter = 0;
        TryShootSingle();

        // Reset buffer if semi-automatic
        if (type == WeaponFireType.SemiAutomatic)
            state.buffered = false;
    }

    private void TryShootSingle() {
        // Abort if...
        if (state.burstShotsCounter >= burstShots || state.burstDelayCounter < burstDelay) return;
        if (ammoType != null && ammoType.currentAmount < ammoPerShot) {
            state.burstShotsCounter = burstShots;
            return;
        }

        // Put other firemode to wait
        if (state.burstShotsCounter == 0) {
            state.waiting = false;
            PutOtherFireModeToWait?.Invoke(this);
        }

        // Reset delay counters and update burst phase
        state.delayCounter = 0f;
        state.burstDelayCounter = 0f;
        state.burstShotsCounter++;

        // Consume ammo
        if (ammoType != null)
            ammoType.currentAmount -= ammoPerShot;

        // Inform listeners the firemode has successfully fired
        Fired?.Invoke(this);
    }
}
