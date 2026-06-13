using UnityEngine;

namespace SpaceShooter.Emitters
{
    /// <summary>Chaotic burst: `count` bullets at random angles within an arc (centered on aim) and random speeds.</summary>
    [CreateAssetMenu(menuName = "SpaceShooter/Emitters/Scatter", fileName = "ScatterEmitter")]
    public class ScatterEmitter : ProjectilePatternSO
    {
        [Header("Scatter")]
        [Min(1)] public int count = 14;
        public float arcDegrees = 360f;
        public float minSpeed = 6f;
        public float maxSpeed = 12f;

        public override void Emit(in AttackContext ctx)
        {
            float baseAngle = DirToAngle(ctx.AimDir);
            float half = arcDegrees * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float a = baseAngle + Random.Range(-half, half);
                var p = MakeParams(in ctx, AngleToDir(a));
                p.speed = Random.Range(minSpeed, maxSpeed);
                ctx.Bullets.Spawn(in p);
            }
        }
    }
}
