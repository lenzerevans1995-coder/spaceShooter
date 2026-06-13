using UnityEngine;

namespace SpaceShooter.Combat
{
    /// <summary>
    /// Anything a bullet can hit. Targets register with the BulletManager so collision
    /// is a cheap per-frame radius check on the XZ play plane rather than Unity physics.
    /// </summary>
    public interface ITarget
    {
        Faction Faction { get; }
        Vector3 Position { get; }   // world position on the play plane (XZ)
        float HitRadius { get; }
        bool IsAlive { get; }
        void ApplyDamage(float amount);
    }
}
