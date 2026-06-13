using UnityEngine;

namespace SpaceShooter.Emitters
{
    /// <summary>
    /// Fires N bullets evenly spaced around a full circle. A non-zero SpinPerShot rotates
    /// the ring a little each volley, turning a static ring into a rotating spiral.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceShooter/Emitters/Ring", fileName = "RingEmitter")]
    public class RingEmitter : ProjectilePatternSO
    {
        [Header("Ring shape")]
        [Min(1)] public int bulletCount = 24;
        public float spinPerShot = 7f;     // degrees added each volley -> spiral
        public bool aimAtTarget = false;   // offset the whole ring toward the aim direction

        public override void Emit(in AttackContext ctx)
        {
            float step = 360f / bulletCount;
            float baseOffset = spinPerShot * ctx.ShotIndex;
            if (aimAtTarget) baseOffset += DirToAngle(ctx.AimDir);

            for (int i = 0; i < bulletCount; i++)
                Fire(in ctx, AngleToDir(baseOffset + i * step));
        }
    }
}
