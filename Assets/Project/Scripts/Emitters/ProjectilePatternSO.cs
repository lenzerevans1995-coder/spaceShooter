using UnityEngine;
using SpaceShooter.BulletHell;

namespace SpaceShooter.Emitters
{
    /// <summary>
    /// Base for all projectile (pooled-bullet) patterns. Holds the shared per-bullet stats
    /// and a helper to fire one bullet along a direction. Concrete patterns only decide the
    /// SHAPE (which directions/positions) by overriding Emit.
    /// </summary>
    public abstract class ProjectilePatternSO : AttackPatternSO
    {
        [Header("Bullet visual")]
        public GameObject bulletPrefab;

        [Header("Shared bullet stats")]
        public float speed = 12f;
        public float acceleration = 0f;
        public float turnRate = 0f;
        public float damage = 1f;
        public float radius = 0.25f;
        public float lifetime = 6f;

        [Header("Behavior")]
        public BulletBehavior behavior = BulletBehavior.Straight;
        [Tooltip("Homing: turn deg/s.  SineWeave: frequency.")]
        public float behaviorParam1 = 0f;
        [Tooltip("SineWeave: amplitude in degrees.")]
        public float behaviorParam2 = 0f;

        /// <summary>Fill a spawn template for this pattern's stats aimed along <paramref name="dir"/>.</summary>
        protected BulletSpawnParams MakeParams(in AttackContext ctx, Vector3 dir)
        {
            return new BulletSpawnParams
            {
                prefab = bulletPrefab,
                position = ctx.Origin,
                direction = dir,
                speed = speed,
                accel = acceleration,
                turnRate = turnRate,
                damage = damage,
                radius = radius,
                lifetime = lifetime,
                faction = ctx.Faction,
                behavior = behavior,
                behaviorParam1 = behaviorParam1,
                behaviorParam2 = behaviorParam2
            };
        }

        /// <summary>Convenience: fire one bullet from the origin along a direction.</summary>
        protected void Fire(in AttackContext ctx, Vector3 dir)
        {
            var p = MakeParams(in ctx, dir);
            ctx.Bullets.Spawn(in p);
        }
    }
}
