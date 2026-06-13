using UnityEngine;

namespace SpaceShooter.Emitters
{
    /// <summary>
    /// A sustained beam attack: telegraph (warning) → active (damaging, optionally sweeping)
    /// → fade. Spawns one Beam via the BeamManager. Slot this into an EmitterController
    /// timeline exactly like any projectile pattern.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceShooter/Emitters/Beam", fileName = "BeamEmitter")]
    public class BeamPatternSO : AttackPatternSO
    {
        [Header("Geometry")]
        public float length = 60f;
        public float width = 1.5f;
        public bool aimAtTarget = true;
        public float angleOffset = 0f;

        [Header("Sweep (degrees swept across the active phase; 0 = fixed)")]
        public float sweepDegrees = 0f;

        [Header("Timing (seconds)")]
        public float telegraphTime = 0.8f;
        public float activeTime = 1.2f;
        public float fadeTime = 0.3f;

        [Header("Damage")]
        public float damagePerSecond = 8f;

        [Header("Color")]
        public Color color = new Color(1f, 0.25f, 0.35f);

        public override void Emit(in AttackContext ctx)
        {
            if (ctx.Beams == null) return; // scene has no BeamManager
            float startAngle = angleOffset + (aimAtTarget ? DirToAngle(ctx.AimDir) : 0f);
            ctx.Beams.Spawn(ctx.Origin, startAngle, length, width,
                            telegraphTime, activeTime, fadeTime, sweepDegrees,
                            damagePerSecond, color, ctx.Faction);
        }
    }
}
