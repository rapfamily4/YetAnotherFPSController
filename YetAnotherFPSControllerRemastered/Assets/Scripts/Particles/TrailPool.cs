using UnityEngine;
using UnityEngine.Pool;

[CreateAssetMenu(menuName = "Scriptable Object/Particles/Trail Pool")]
public class TrailPool : SerializablePool<TrailController> {
    override protected void InitPool() {
        DisposePool();
        m_pool = new LinkedPool<TrailController>(
            createFunc: () => {
                TrailController trail = Instantiate(prefab);
                trail.trailPool = m_pool;
                return trail;
            },
            actionOnGet: (obj) => obj.gameObject.SetActive(true),
            actionOnRelease: (obj) => obj.gameObject.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj.gameObject),
            collectionCheck: true,
            maxSize: poolSize
        );
    }
}
