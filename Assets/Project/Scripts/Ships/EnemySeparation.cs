using UnityEngine;
using SpaceShooter.Combat;

namespace SpaceShooter.Ships
{
    /// <summary>
    /// Lightweight Boids-style separation steering. Pushes this enemy away from nearby same-faction
    /// enemies so a drifting wave spreads into an organic swarm instead of stacking on one point.
    ///
    /// NOTE on the implementation: the brief asked for Physics.OverlapSphere, but ships in this
    /// project carry NO physics colliders — hit detection is a radius check against the shared
    /// <see cref="TargetRegistry"/>. OverlapSphere would therefore find nothing. Iterating the
    /// registry (a short list of live targets) is faster than a physics query, allocation-free, and
    /// needs no colliders or layers. If enemies ever gain trigger colliders, swap Compute()'s loop
    /// for Physics.OverlapSphereNonAlloc against an Enemy layer — the steering math is identical.
    /// </summary>
    public class EnemySeparation : MonoBehaviour
    {
        [SerializeField] float _radius = 3.2f;     // neighbours closer than this push back
        [SerializeField] float _strength = 10f;    // peak repulsion speed (units/sec) at contact

        Transform _tf;
        ShipActor _self;

        void Awake()
        {
            _tf = transform;
            _self = GetComponent<ShipActor>();
        }

        /// <summary>
        /// Velocity-space repulsion (units/sec) from nearby same-faction enemies. Each neighbour
        /// contributes a push along the line away from it, weighted 1→0 across the radius (closer =
        /// stronger). Cheap O(n) over the live target list; n is tiny (a wave of enemies).
        /// </summary>
        public Vector3 Compute()
        {
            var targets = TargetRegistry.Targets;
            Vector3 pos = _tf.position;
            Vector3 push = Vector3.zero;
            float r = _radius;

            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t == null || ReferenceEquals(t, _self)) continue;
                if (!t.IsAlive || (_self != null && t.Faction != _self.Faction)) continue;

                Vector3 d = pos - t.Position; d.y = 0f;
                float dist = d.magnitude;
                if (dist >= r || dist < 0.0001f)
                {
                    if (dist < 0.0001f) push += new Vector3(0.01f, 0f, 0f); // exact overlap: nudge
                    continue;
                }
                push += (d / dist) * (1f - dist / r);
            }

            return push * _strength;
        }
    }
}
