using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable Object/LevelData")]
public class LevelData : ScriptableObject {
    [Header("Scene Information")]
    public string sceneName;
    public bool isMenu = false;
}
