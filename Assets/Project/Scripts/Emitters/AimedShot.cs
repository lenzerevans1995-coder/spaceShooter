using UnityEngine;

namespace SpaceShooter.Emitters
{
    /// <summary>
    /// Straight line fired at the aim direction. count == 1 is a single aimed bullet;
    /// count > 1 with spreadDegrees == 0 is an inline burst, or a narrow aimed spread.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceShooter/Emitters/Aimed Shot", fileName = "AimedShot")]
    public class AimedShot : ProjectilePatternSO
    {
        [Header("Aim")]
        [Min(1)] public int count = 1;
        public float spreadDegrees = 0f;   // 0 = perfectly straight line

        public override void Emit(in AttackContext ctx)
        {
            float baseAngle = DirToAngle(ctx.AimDir);
            if (count == 1) { Fire(in ctx, AngleToDir(baseAngle)); return; }

            float start = baseAngle - spreadDegrees * 0.5f;
            float step = spreadDegrees / (count - 1);
            for (int i = 0; i < count; i++)
                Fire(in ctx, AngleToDir(start + i * step));
        }
    }
}
