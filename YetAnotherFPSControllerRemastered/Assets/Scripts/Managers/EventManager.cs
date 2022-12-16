using UnityEngine.Events;
using System.Collections.Generic;

public class EventManager : Singleton<EventManager> {
    // --- Private members
    private Dictionary<string, UnityEvent> m_eventDictionary;
    private Dictionary<string, UnityEvent<int>> m_intEventDictionary;
    private Dictionary<string, UnityEvent<float>> m_floatEventDictionary;
    private Dictionary<string, UnityEvent<string>> m_stringEventDictionary;
    private Dictionary<string, UnityEvent<LevelData>> m_levelDataEventDictionary;


    // --- MonoBehaviour methods
    void Awake() {
        // Instantiate the singleton instance of the event manager
        // It will destroy the game object if an instance is already present.
        if (!Instantiate()) return;

        // Create a new event dictionary
        if (m_eventDictionary == null)
            m_eventDictionary = new Dictionary<string, UnityEvent>();
        if (m_intEventDictionary == null)
            m_intEventDictionary = new Dictionary<string, UnityEvent<int>>();
        if (m_floatEventDictionary == null)
            m_floatEventDictionary = new Dictionary<string, UnityEvent<float>>();
        if (m_stringEventDictionary == null)
            m_stringEventDictionary = new Dictionary<string, UnityEvent<string>>();
        if (m_levelDataEventDictionary == null)
            m_levelDataEventDictionary = new Dictionary<string, UnityEvent<LevelData>>();
    }

    // --- EventManager methods
    #region StartListening
    public static void StartListening(string eventName, UnityAction listener) {
        if (instance.m_eventDictionary == null)
            instance.m_eventDictionary = new Dictionary<string, UnityEvent>();

        UnityEvent unityEvent = null;
        if (instance.m_eventDictionary.TryGetValue(eventName, out unityEvent))
            unityEvent.AddListener(listener);
        else {
            unityEvent = new UnityEvent();
            unityEvent.AddListener(listener);
            instance.m_eventDictionary.Add(eventName, unityEvent);
        }
    }

    public static void StartListening(string eventName, UnityAction<int> listener) {
        if (instance.m_intEventDictionary == null)
            instance.m_intEventDictionary = new Dictionary<string, UnityEvent<int>>();

        UnityEvent<int> unityEvent = null;
        if (instance.m_intEventDictionary.TryGetValue(eventName, out unityEvent))
            unityEvent.AddListener(listener);
        else {
            unityEvent = new UnityEvent<int>();
            unityEvent.AddListener(listener);
            instance.m_intEventDictionary.Add(eventName, unityEvent);
        }
    }

    public static void StartListening(string eventName, UnityAction<float> listener) {
        if (instance.m_floatEventDictionary == null)
            instance.m_floatEventDictionary = new Dictionary<string, UnityEvent<float>>();

        UnityEvent<float> unityEvent = null;
        if (instance.m_floatEventDictionary.TryGetValue(eventName, out unityEvent))
            unityEvent.AddListener(listener);
        else {
            unityEvent = new UnityEvent<float>();
            unityEvent.AddListener(listener);
            instance.m_floatEventDictionary.Add(eventName, unityEvent);
        }
    }

    public static void StartListening(string eventName, UnityAction<string> listener) {
        if (instance.m_stringEventDictionary == null)
            instance.m_stringEventDictionary = new Dictionary<string, UnityEvent<string>>();

        UnityEvent<string> unityEvent = null;
        if (instance.m_stringEventDictionary.TryGetValue(eventName, out unityEvent))
            unityEvent.AddListener(listener);
        else {
            unityEvent = new UnityEvent<string>();
            unityEvent.AddListener(listener);
            instance.m_stringEventDictionary.Add(eventName, unityEvent);
        }
    }

    public static void StartListening(string eventName, UnityAction<LevelData> listener) {
        if (instance.m_levelDataEventDictionary == null)
            instance.m_levelDataEventDictionary = new Dictionary<string, UnityEvent<LevelData>>();

        UnityEvent<LevelData> unityEvent = null;
        if (instance.m_levelDataEventDictionary.TryGetValue(eventName, out unityEvent))
            unityEvent.AddListener(listener);
        else {
            unityEvent = new UnityEvent<LevelData>();
            unityEvent.AddListener(listener);
            instance.m_levelDataEventDictionary.Add(eventName, unityEvent);
        }
    }
    #endregion

    #region StopListening
    public static void StopListening(string eventName, UnityAction listener) {
        if (instance.m_eventDictionary == null)
            instance.m_eventDictionary = new Dictionary<string, UnityEvent>();

        if (instance == null) return;
        UnityEvent unityEvent = null;
        if (instance.m_eventDictionary.TryGetValue(eventName, out unityEvent))
            unityEvent.RemoveListener(listener);
    }

    public static void StopListening(string eventName, UnityAction<int> listener) {
        if (instance.m_intEventDictionary == null)
            instance.m_intEventDictionary = new Dictionary<string, UnityEvent<int>>();

        if (instance == null) return;
        UnityEvent<int> unityEvent = null;
        if (instance.m_intEventDictionary.TryGetValue(eventName, out unityEvent))
            unityEvent.RemoveListener(listener);
    }

    public static void StopListening(string eventName, UnityAction<float> listener) {
        if (instance.m_floatEventDictionary == null)
            instance.m_floatEventDictionary = new Dictionary<string, UnityEvent<float>>();

        if (instance == null) return;
        UnityEvent<float> unityEvent = null;
        if (instance.m_floatEventDictionary.TryGetValue(eventName, out unityEvent))
            unityEvent.RemoveListener(listener);
    }

    public static void StopListening(string eventName, UnityAction<string> listener) {
        if (instance.m_stringEventDictionary == null)
            instance.m_stringEventDictionary = new Dictionary<string, UnityEvent<string>>();

        if (instance == null) return;
        UnityEvent<string> unityEvent = null;
        if (instance.m_stringEventDictionary.TryGetValue(eventName, out unityEvent))
            unityEvent.RemoveListener(listener);
    }

    public static void StopListening(string eventName, UnityAction<LevelData> listener) {
        if (instance.m_levelDataEventDictionary == null)
            instance.m_levelDataEventDictionary = new Dictionary<string, UnityEvent<LevelData>>();

        if (instance == null) return;
        UnityEvent<LevelData> unityEvent = null;
        if (instance.m_levelDataEventDictionary.TryGetValue(eventName, out unityEvent))
            unityEvent.RemoveListener(listener);
    }
    #endregion

    #region TriggerEvent
    public static void TriggerEvent(string eventName) {
        if (instance.m_eventDictionary == null)
            instance.m_eventDictionary = new Dictionary<string, UnityEvent>();

        UnityEvent unityEvent = null;
        if (instance.m_eventDictionary.TryGetValue(eventName, out unityEvent))
            unityEvent.Invoke();
    }

    public static void TriggerEvent(string eventName, int value) {
        if (instance.m_intEventDictionary == null)
            instance.m_intEventDictionary = new Dictionary<string, UnityEvent<int>>();

        UnityEvent<int> unityEvent = null;
        if (instance.m_intEventDictionary.TryGetValue(eventName, out unityEvent))
            unityEvent.Invoke(value);
    }

    public static void TriggerEvent(string eventName, float value) {
        if (instance.m_floatEventDictionary == null)
            instance.m_floatEventDictionary = new Dictionary<string, UnityEvent<float>>();

        UnityEvent<float> unityEvent = null;
        if (instance.m_floatEventDictionary.TryGetValue(eventName, out unityEvent))
            unityEvent.Invoke(value);
    }

    public static void TriggerEvent(string eventName, string value) {
        if (instance.m_stringEventDictionary == null)
            instance.m_stringEventDictionary = new Dictionary<string, UnityEvent<string>>();

        UnityEvent<string> unityEvent = null;
        if (instance.m_stringEventDictionary.TryGetValue(eventName, out unityEvent))
            unityEvent.Invoke(value);
    }

    public static void TriggerEvent(string eventName, LevelData value) {
        if (instance.m_levelDataEventDictionary == null)
            instance.m_levelDataEventDictionary = new Dictionary<string, UnityEvent<LevelData>>();

        UnityEvent<LevelData> unityEvent = null;
        if (instance.m_levelDataEventDictionary.TryGetValue(eventName, out unityEvent))
            unityEvent.Invoke(value);
    }
    #endregion
}

