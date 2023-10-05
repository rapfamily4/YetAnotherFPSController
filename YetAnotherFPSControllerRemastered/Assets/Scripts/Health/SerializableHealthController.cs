/** Luigi Rapetta, 2023 **/


using UnityEngine;

public class SerializableHealthController : HealthControllerBase {
    [SerializeField] private SerializableHealthObject entityHealth;
    public override HealthObject healthObject => entityHealth.healthObject;
}