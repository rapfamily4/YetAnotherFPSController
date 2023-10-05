using UnityEngine;
using UnityEngine.Pool;

[CreateAssetMenu(menuName = "Scriptable Object/Particles/Line Pool")]
public class LinePool : SerializablePool<LineController> {
    override protected void InitPool() {
        DisposePool();
        m_pool = new LinkedPool<LineController>(
            createFunc: () => {
                LineController line = Instantiate(prefab);
                line.linePool = m_pool;
                return line;
            },
            actionOnGet: (obj) => obj.gameObject.SetActive(true),
            actionOnRelease: (obj) => obj.gameObject.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj.gameObject),
            collectionCheck: true,
            maxSize: poolSize
        );
    }
}
