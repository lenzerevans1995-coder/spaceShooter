using UnityEngine;

namespace SpaceShooter.Emitters
{
    /// <summary>
    /// One or more rotating arms. Each volley fires `arms` bullets evenly spaced, advancing
    /// the base angle by SpinPerShot every volley so successive bullets trace spiral arms.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceShooter/Emitters/Spiral", fileName = "SpiralEmitter")]
    public class SpiralEmitter : ProjectilePatternSO
    {
        [Header("Spiral shape")]
        [Min(1)] public int arms = 3;
        public float spinPerShot = 11f;
        public bool aimAtTarget = false;

        public override void Emit(in AttackContext ctx)
        {
            float armStep = 360f / arms;
            float baseOffset = spinPerShot * ctx.ShotIndex + (aimAtTarget ? DirToAngle(ctx.AimDir) : 0f);

            for (int i = 0; i < arms; i++)
                Fire(in ctx, AngleToDir(baseOffset + i * armStep));
        }
    }
}
