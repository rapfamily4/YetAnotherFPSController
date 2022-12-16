using UnityEngine;


[RequireComponent(typeof(LevelManager))]
public class GameManager : Singleton<GameManager> {
    // --- Public properties
    public static LevelManager levelManager { get; private set; }
    public static PlayerController playerController { get; private set; }


    // --- MonoBehaviour methods
    void Awake() {
        // Instantiate singleton; if it fails, it won't update the static references
        if (!Instantiate()) return;

        // Retrieve references of GameManager's components
        levelManager = GetComponent<LevelManager>();
    }

    void OnEnable() {
        EventManager.StartListening(Constants.EVENT_SCENELOADED, OnSceneLoaded);
    }

    void OnDisable() {
        EventManager.StopListening(Constants.EVENT_SCENELOADED, OnSceneLoaded);
    }


    // --- GameManager methods
    private void OnSceneLoaded(LevelData levelData) {
        Debug.Log("GameManager.OnSceneLoaded() on scene \"" + levelData.sceneName + "\"");

        // Retrieve player controller
        GameObject playerObject = GameObject.FindWithTag(Constants.TAG_PLAYER);
        if (playerObject) {
            // Retrieve PlayerController component
            playerController = playerObject.GetComponent<PlayerController>();
            if (!playerController) Debug.LogWarning("GameManager.OnSceneLoaded() found a player GameObject without a PlayerController component.");
            else Debug.Log("GameManager.OnSceneLoaded() found the player.");
        } else Debug.LogWarning("GameManager.OnSceneLoaded() couldn't find a player GameObject.");
    }
}
