using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneReloader : MonoBehaviour {
    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag(Constants.TAG_PLAYER))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }
}
