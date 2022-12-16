using UnityEngine;

[CreateAssetMenu(menuName = "Antarctica/LevelData")]
public class LevelData : ScriptableObject {
    [Header("Scene Information")]
    public string sceneName;
    public bool isMenu = false;
}
