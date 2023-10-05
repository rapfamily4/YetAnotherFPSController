/** Luigi Rapetta, 2023 **/

using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable Object/Serializable Health Object")]
public class SerializableHealthObject : ScriptableObject {
    public HealthObject healthObject;

    private void OnEnable() {
        hideFlags = HideFlags.DontUnloadUnusedAsset; // It'll ensure us that the SO won't be unloaded if unused
    }
}