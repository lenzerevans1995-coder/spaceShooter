using UnityEngine;
using SpaceShooter.BulletHell;
using SpaceShooter.Combat;

namespace SpaceShooter.Emitters
{
    /// <summary>
    /// Per-volley context handed to a pattern's Emit(). Carries BOTH hazard managers so a
    /// single pattern asset can spawn bullets or a beam without the controller caring which.
    /// </summary>
    public readonly struct AttackContext
    {
        public readonly BulletManager Bullets;
        public readonly BeamManager Beams;
        public readonly Vector3 Origin;
        public readonly Vector3 AimDir;   // XZ unit vector
        public readonly Faction Faction;
        public readonly int ShotIndex;    // increments per volley over the emitter's life

        public AttackContext(BulletManager bullets, BeamManager beams, Vector3 origin,
                             Vector3 aimDir, Faction faction, int shotIndex)
        {
            Bullets = bullets;
            Beams = beams;
            Origin = origin;
            AimDir = aimDir;
            Faction = faction;
            ShotIndex = shotIndex;
        }
    }
}
