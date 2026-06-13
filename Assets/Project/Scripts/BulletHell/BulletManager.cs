using System.Collections.Generic;
using UnityEngine;
using SpaceShooter.Combat;

namespace SpaceShooter.BulletHell
{
    /// <summary>
    /// Owns every live bullet and ticks them in a single batched loop, owns the pool, and
    /// runs collision against the shared TargetRegistry. This central design is what lets
    /// the game sustain thousands of simultaneous bullets at 60 fps.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class BulletManager : MonoBehaviour
    {
        public static BulletManager Instance { get; private set; }

        [Header("Playfield bounds (XZ). Bullets beyond these + padding are culled.")]
        [SerializeField] Vector2 _fieldCenter = Vector2.zero;
        [SerializeField] Vector2 _fieldHalfExtents = new Vector2(40f, 24f);
        [SerializeField] float _cullPadding = 3f;

        // When set (> 0) the cull region is a disc of this radius around _fieldCenter, matching
        // the circular arena, instead of the rectangle above. Configured from the field radius so
        // bullets fired outward from the arena edge still fly visibly past it before they die.
        [SerializeField] float _cullRadius = 0f;

        /// <summary>Switch culling to a disc matching the round's circular arena (+ padding).</summary>
        public void SetCullCircle(Vector2 center, float radius)
        {
            _fieldCenter = center;
            _cullRadius = Mathf.Max(0f, radius);
        }

        [Header("Pooling")]
        [SerializeField] int _initialCapacity = 2048;

        BulletPool _pool;
        readonly List<Bullet> _active = new(2048);

        public int ActiveCount => _active.Count;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            var container = new GameObject("PooledBullets").transform;
            container.SetParent(transform, false);
            _pool = new BulletPool(container);
            _active.Capacity = _initialCapacity;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void PrewarmType(GameObject bulletPrefab, int count) => _pool.Prewarm(bulletPrefab, count);

        /// <summary>Spawn a single bullet. Called by projectile patterns.</summary>
        public Bullet Spawn(in BulletSpawnParams p)
        {
            Bullet b = _pool.Get(p.prefab);
            b.Spawn(in p);
            _active.Add(b);
            return b;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            bool circular = _cullRadius > 0f;
            float minX = _fieldCenter.x - _fieldHalfExtents.x - _cullPadding;
            float maxX = _fieldCenter.x + _fieldHalfExtents.x + _cullPadding;
            float minZ = _fieldCenter.y - _fieldHalfExtents.y - _cullPadding;
            float maxZ = _fieldCenter.y + _fieldHalfExtents.y + _cullPadding;
            float cullSqr = _cullRadius * _cullRadius;

            var targets = TargetRegistry.Targets;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Bullet b = _active[i];
                bool alive = b.Tick(dt);

                if (alive)
                {
                    Vector3 p = b.Position;
                    bool outOfBounds;
                    if (circular)
                    {
                        float dx = p.x - _fieldCenter.x, dz = p.z - _fieldCenter.y;
                        outOfBounds = dx * dx + dz * dz > cullSqr;
                    }
                    else
                    {
                        outOfBounds = p.x < minX || p.x > maxX || p.z < minZ || p.z > maxZ;
                    }

                    if (outOfBounds)
                        alive = false;
                    else if (CheckCollision(b, targets))
                        alive = false;
                }

                if (!alive)
                {
                    _pool.Return(b);
                    int last = _active.Count - 1;
                    _active[i] = _active[last];
                    _active.RemoveAt(last);
                }
            }
        }

        /// <summary>
        /// Naive radius check against opposing targets. Fine while target count is small
        /// (a handful of enemies + the player). A spatial grid is a later optimization if
        /// target counts ever grow large.
        /// </summary>
        bool CheckCollision(Bullet b, IReadOnlyList<ITarget> targets)
        {
            for (int t = 0; t < targets.Count; t++)
            {
                ITarget target = targets[t];
                if (!target.IsAlive || target.Faction == b.Faction) continue;

                Vector3 d = target.Position - b.Position;
                float r = target.HitRadius + b.Radius;
                if (d.x * d.x + d.z * d.z <= r * r)
                {
                    target.ApplyDamage(b.Damage);
                    return true;
                }
            }
            return false;
        }
    }
}
