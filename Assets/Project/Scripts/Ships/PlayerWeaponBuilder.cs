using UnityEngine;
using SpaceShooter.BulletHell;
using SpaceShooter.Emitters;
using SpaceShooter.Progression;

namespace SpaceShooter.Ships
{
    /// <summary>
    /// Turns the player's computed <see cref="PlayerStats"/> into a live <see cref="AttackPatternSO"/>.
    /// Called whenever the skill tree recalculates, so weapon upgrades (more damage, faster bolts,
    /// scatter, homing) take effect the instant a node is unlocked. The pattern is an in-memory
    /// ScriptableObject instance — not an asset — rebuilt fresh each change.
    /// </summary>
    public static class PlayerWeaponBuilder
    {
        const float Lifetime = 2.2f;

        public static AttackPatternSO Build(PlayerStats s, GameObject bulletPrefab)
        {
            ProjectilePatternSO pat;

            // ScatterShot (or any multi-projectile bonus) fans the bolts; otherwise a single aimed shot.
            bool scatter = s.Has(SpecialUnlock.ScatterShot) || s.projectileCount > 1;
            if (scatter)
            {
                int n = Mathf.Max(2, s.projectileCount);
                var f = ScriptableObject.CreateInstance<FanEmitter>();
                f.count = n;
                f.arcDegrees = Mathf.Min(70f, 9f * n);   // wider fan as the count grows
                pat = f;
            }
            else
            {
                var a = ScriptableObject.CreateInstance<AimedShot>();
                a.count = 1;
                a.spreadDegrees = 0f;
                pat = a;
            }

            pat.bulletPrefab = bulletPrefab;
            pat.speed = s.projectileSpeed;
            pat.damage = s.damage;
            pat.radius = s.projectileRadius;
            pat.lifetime = Lifetime;

            if (s.Has(SpecialUnlock.HomingShots))
            {
                pat.behavior = BulletBehavior.Homing;
                pat.behaviorParam1 = 200f;   // turn deg/s
            }

            return pat;
        }
    }
}
