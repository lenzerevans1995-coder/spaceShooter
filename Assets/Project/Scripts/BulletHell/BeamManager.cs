using System.Collections.Generic;
using UnityEngine;
using SpaceShooter.Combat;

namespace SpaceShooter.BulletHell
{
    /// <summary>
    /// Owns active beams (typically few), pools them, and runs point-to-segment collision
    /// against the shared TargetRegistry during each beam's Active phase. Parallel to the
    /// BulletManager but for continuous line hazards.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class BeamManager : MonoBehaviour
    {
        public static BeamManager Instance { get; private set; }

        readonly List<Beam> _active = new(16);
        readonly Stack<Beam> _free = new();
        Transform _container;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _container = new GameObject("PooledBeams").transform;
            _container.SetParent(transform, false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public Beam Spawn(Vector3 origin, float startAngle, float length, float width,
                          float telegraph, float active, float fade, float sweep,
                          float dps, Color color, Faction faction)
        {
            Beam b = _free.Count > 0 ? _free.Pop() : CreateBeam();
            b.gameObject.SetActive(true);
            b.Spawn(origin, startAngle, length, width, telegraph, active, fade, sweep, dps, color, faction);
            _active.Add(b);
            return b;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var targets = TargetRegistry.Targets;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Beam b = _active[i];
                bool alive = b.Tick(dt);
                if (alive && b.IsDamaging) Collide(b, targets, dt);

                if (!alive)
                {
                    b.gameObject.SetActive(false);
                    _free.Push(b);
                    int last = _active.Count - 1;
                    _active[i] = _active[last];
                    _active.RemoveAt(last);
                }
            }
        }

        /// <summary>Per-target point-to-segment distance check in XZ; applies DPS * dt while in range.</summary>
        void Collide(Beam b, IReadOnlyList<ITarget> targets, float dt)
        {
            Vector3 a = b.Origin;
            Vector3 dir = b.Dir;
            float len = b.Length;
            float hw = b.DamageHalfWidth;
            float bx = dir.x * len, bz = dir.z * len;
            float segSqr = bx * bx + bz * bz;
            if (segSqr < 1e-6f) return;

            for (int i = 0; i < targets.Count; i++)
            {
                ITarget t = targets[i];
                if (!t.IsAlive || t.Faction == b.Faction) continue;

                Vector3 p = t.Position;
                float apx = p.x - a.x, apz = p.z - a.z;
                float u = Mathf.Clamp01((apx * bx + apz * bz) / segSqr);
                float cx = a.x + bx * u, cz = a.z + bz * u;
                float dx = p.x - cx, dz = p.z - cz;
                float r = hw + t.HitRadius;
                if (dx * dx + dz * dz <= r * r)
                    t.ApplyDamage(b.Dps * dt);
            }
        }

        Beam CreateBeam()
        {
            var go = new GameObject("Beam");
            go.transform.SetParent(_container, false);
            Beam b = go.AddComponent<Beam>(); // RequireComponent adds the LineRenderer
            go.SetActive(false);
            return b;
        }
    }
}
