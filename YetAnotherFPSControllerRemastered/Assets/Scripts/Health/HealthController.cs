/** Luigi Rapetta, 2023 **/


using UnityEngine;

public class HealthController : HealthControllerBase {
    [SerializeField] private HealthObject entityHealth;
    public override HealthObject healthObject => entityHealth;
}