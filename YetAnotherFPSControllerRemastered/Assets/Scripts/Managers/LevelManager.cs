using UnityEngine;
using UnityEngine.SceneManagement;


public class LevelManager : MonoBehaviour {
    // --- Public members
    public LevelData levelDataInitializer = null;

    // --- Public properties
    public LevelData currentLevel { get; private set; }
    public bool isCurrentlyLoading { get; private set; }


    // --- MonoBehaviour methods
    private void Awake() {
        // If m_currentLevel is null, initialize it with levelDataInitializer
        if (!currentLevel)
            currentLevel = levelDataInitializer;
    }

    private void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    // --- LevelManager methods
    public void LoadLevel(LevelData levelToLoad) {
        // Return if given LevelData is invalid
        if (levelToLoad == null) return;

        // Reset timeScale to 1 (aka normal speed)
        Time.timeScale = 1f;

        // Mark level manager as currently loading
        isCurrentlyLoading = true;

        // Load scene
        currentLevel = levelToLoad;
        Debug.Log("LevelManager.LoadLevel() on scene \"" + levelToLoad.sceneName + "\"");
        SceneManager.LoadScene(levelToLoad.sceneName);
    }

    public void ReloadCurrentLevel() {
        // Reset timeScale to 1 (aka normal speed)
        Time.timeScale = 1f;

        // Mark level manager as currently loading
        isCurrentlyLoading = true;

        // Reload the current scene
        Scene activeScene = SceneManager.GetActiveScene();
        Debug.Log("LevelManager.ReloadCurrentLevel() on scene \"" + activeScene.name + "\"");
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode) {
        // Mark level manager as not currently loading
        isCurrentlyLoading = false;

        // Trigger SCENELOADED event
        Debug.Log("LevelManager.OnSceneLoaded() with scene \"" + scene.name + "\"");
        EventManager.TriggerEvent(Constants.EVENT_SCENELOADED);
        EventManager.TriggerEvent(Constants.EVENT_SCENELOADED, currentLevel);
    }
}
