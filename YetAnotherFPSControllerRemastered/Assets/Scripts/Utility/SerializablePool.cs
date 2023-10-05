using System;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;


public abstract class SerializablePool<T> : ScriptableObject where T : class {
    // --- Members
    public T prefab;
    [Min(0)] public int poolSize = 10;

    [NonSerialized] protected LinkedPool<T> m_pool;


    // --- ScriptableObject methods
    private void OnEnable() {
        InitPool();
        SceneManager.sceneLoaded += InitPoolOnSceneLoaded;
    }

    private void OnDisable() {
        DisposePool();
        SceneManager.sceneLoaded -= InitPoolOnSceneLoaded;
    }

    virtual protected void OnValidate() {
        InitPool();
    }

    // --- ProjectilePool methods
    public T Get() {
        return m_pool.Get();
    }

    abstract protected void InitPool();

    protected void DisposePool() {
        // FORGIVE ME FATHER, FOR I HAVE SINNED
        try {
            m_pool?.Dispose();
        } catch (Exception ex) {
            if (ex is MissingReferenceException || ex is NullReferenceException) {
                Debug.LogWarning("From resource " + name + " of type " + this.GetType().ToString() + ": " +
                                 ex.Message +
                                 "To allow the fully disposal of the pool, it will be manually set to null. " +
                                 "The pool and any dangling references to (already destroyed) objects will be garbage collected.");
                m_pool = null; // dangling references will be garbage collected
                return;
            }
            throw;
        }
    }

    private void InitPoolOnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (mode == LoadSceneMode.Single)
            InitPool();
    }
}
