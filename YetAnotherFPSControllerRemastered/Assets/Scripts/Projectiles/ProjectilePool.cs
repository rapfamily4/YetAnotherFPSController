using UnityEngine;
using UnityEngine.Pool;

[CreateAssetMenu(menuName = "Scriptable Object/Projectile/Projectile Pool")]
public class ProjectilePool : SerializablePool<ProjectileBase> {
    public bool isHitscan { get; private set; } = false;

    private void Awake() {
        isHitscan = prefab.GetType() == typeof(HitscanProjectile);
    }

    protected override void OnValidate() {
        isHitscan = prefab.GetType() == typeof(HitscanProjectile);
        base.OnValidate();
    }

    override protected void InitPool() {
        DisposePool();
        m_pool = new LinkedPool<ProjectileBase>(
            createFunc: () => {
                ProjectileBase projectile = Instantiate(prefab);
                projectile.projectilePool = m_pool;
                projectile.ignoredColliders = new System.Collections.Generic.List<Collider>();
                return projectile;
            },
            actionOnGet: (obj) => obj.gameObject.SetActive(true),
            actionOnRelease: (obj) => obj.gameObject.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj.gameObject),
            collectionCheck: true,
            maxSize: poolSize
        );
    }
}
