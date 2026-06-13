using UnityEngine;

namespace SpaceShooter.Emitters
{
    /// <summary>N bullets evenly spread across an arc centered on the aim direction (horizontal spread).</summary>
    [CreateAssetMenu(menuName = "SpaceShooter/Emitters/Fan", fileName = "FanEmitter")]
    public class FanEmitter : ProjectilePatternSO
    {
        [Header("Fan shape")]
        [Min(1)] public int count = 7;
        public float arcDegrees = 60f;

        public override void Emit(in AttackContext ctx)
        {
            float baseAngle = DirToAngle(ctx.AimDir);
            if (count == 1) { Fire(in ctx, AngleToDir(baseAngle)); return; }

            float start = baseAngle - arcDegrees * 0.5f;
            float step = arcDegrees / (count - 1);
            for (int i = 0; i < count; i++)
                Fire(in ctx, AngleToDir(start + i * step));
        }
    }
}
