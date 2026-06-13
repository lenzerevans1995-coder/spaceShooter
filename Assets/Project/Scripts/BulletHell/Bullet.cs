using UnityEngine;
using SpaceShooter.Combat;

namespace SpaceShooter.BulletHell
{
    /// <summary>
    /// A single pooled projectile. Deliberately has NO Update() — the BulletManager ticks
    /// every live bullet from one central loop, far cheaper than thousands of individual
    /// Unity Update callbacks. Movement is on the XZ play plane (world up = Y). Supports an
    /// optional per-bullet behavior (homing / sine-weave) layered on speed + accel.
    /// </summary>
    public class Bullet : MonoBehaviour
    {
        Vector3 _direction;       // current unit heading (XZ)
        Vector3 _baseDirection;   // launch heading, for SineWeave
        float _speed, _accel, _turnRate, _damage, _radius, _lifetime, _age;
        Faction _faction;
        BulletBehavior _behavior;
        float _bp1, _bp2;
        Transform _tf;
        ParticleSystem[] _particles;   // optional VFX (pack projectiles); restarted on spawn for pooling

        /// <summary>The prefab this instance was pooled from. Set once by BulletPool.</summary>
        public GameObject PrefabKey { get; internal set; }

        public Faction Faction => _faction;
        public float Damage => _damage;
        public float Radius => _radius;
        public Vector3 Position => _tf.position;

        void Awake()
        {
            _tf = transform;
            _particles = GetComponentsInChildren<ParticleSystem>(true); // empty for primitive bullets
        }

        /// <summary>Configure a freshly-pooled bullet and place it in the world.</summary>
        public void Spawn(in BulletSpawnParams p)
        {
            if (_tf == null) _tf = transform; // safety if spawned the same frame as creation
            _tf.position = p.position;
            _direction = p.direction.sqrMagnitude > 1e-4f ? p.direction.normalized : Vector3.right;
            _baseDirection = _direction;
            _speed = p.speed;
            _accel = p.accel;
            _turnRate = p.turnRate;
            _damage = p.damage;
            _radius = p.radius;
            _lifetime = p.lifetime;
            _age = 0f;
            _faction = p.faction;
            _behavior = p.behavior;
            _bp1 = p.behaviorParam1;
            _bp2 = p.behaviorParam2;
            FaceDirection();

            // Restart any VFX so pooled particle projectiles don't show stale state.
            if (_particles != null)
            {
                for (int i = 0; i < _particles.Length; i++)
                {
                    _particles[i].Clear(true);
                    _particles[i].Play(true);
                }
            }
        }

        /// <summary>
        /// Advance one frame. Returns false when expired (off-screen culling and collision
        /// are owned by the BulletManager, not here).
        /// </summary>
        public bool Tick(float dt)
        {
            _age += dt;
            if (_age >= _lifetime) return false;

            switch (_behavior)
            {
                case BulletBehavior.Homing:
                    SteerTowardNearest(dt);
                    break;
                case BulletBehavior.SineWeave:
                    float off = Mathf.Sin(_age * _bp1) * _bp2;
                    _direction = Quaternion.AngleAxis(off, Vector3.up) * _baseDirection;
                    FaceDirection();
                    break;
                default: // Straight — constant turn supported (spirals/curves)
                    if (_turnRate != 0f)
                    {
                        _direction = Quaternion.AngleAxis(_turnRate * dt, Vector3.up) * _direction;
                        FaceDirection();
                    }
                    break;
            }

            if (_accel != 0f)
            {
                _speed += _accel * dt;
                if (_speed < 0f) _speed = 0f;
            }

            _tf.position += _direction * (_speed * dt);
            return true;
        }

        void SteerTowardNearest(float dt)
        {
            var targets = TargetRegistry.Targets;
            ITarget best = null;
            float bestSqr = float.MaxValue;
            Vector3 pos = _tf.position;
            for (int i = 0; i < targets.Count; i++)
            {
                ITarget t = targets[i];
                if (!t.IsAlive || t.Faction == _faction) continue;
                Vector3 d = t.Position - pos;
                float s = d.x * d.x + d.z * d.z;
                if (s < bestSqr) { bestSqr = s; best = t; }
            }
            if (best == null) return;

            Vector3 desired = best.Position - pos;
            desired.y = 0f;
            if (desired.sqrMagnitude < 1e-4f) return;
            desired.Normalize();

            float maxRad = _bp1 * Mathf.Deg2Rad * dt;
            _direction = Vector3.RotateTowards(_direction, desired, maxRad, 0f);
            _direction.y = 0f;
            _direction.Normalize();
            FaceDirection();
        }

        void FaceDirection()
        {
            // Orient the projectile's forward (+Z) along travel. Pack VFX emit along forward.
            if (_direction.sqrMagnitude > 1e-4f)
                _tf.rotation = Quaternion.LookRotation(_direction, Vector3.up);
        }
    }
}
