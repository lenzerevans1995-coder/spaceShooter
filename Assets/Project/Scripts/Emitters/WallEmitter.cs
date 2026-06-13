using UnityEngine;

namespace SpaceShooter.Emitters
{
    /// <summary>
    /// A curtain / vertical-spread wall: a row of bullets spaced across `width` PERPENDICULAR
    /// to the travel direction, all moving the same way. Optionally skips one index to leave a
    /// dodge lane.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceShooter/Emitters/Wall", fileName = "WallEmitter")]
    public class WallEmitter : ProjectilePatternSO
    {
        [Header("Wall shape")]
        [Min(1)] public int count = 10;
        public float width = 18f;
        [Tooltip("Index of a bullet to skip to leave a dodge gap; < 0 = solid wall.")]
        public int gapIndex = -1;

        public override void Emit(in AttackContext ctx)
        {
            Vector3 travel = ctx.AimDir;
            Vector3 perp = new Vector3(-travel.z, 0f, travel.x); // 90° rotation in XZ
            float start = -width * 0.5f;
            float step = count > 1 ? width / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                if (i == gapIndex) continue;
                var p = MakeParams(in ctx, travel);
                p.position = ctx.Origin + perp * (start + i * step);
                ctx.Bullets.Spawn(in p);
            }
        }
    }
}
