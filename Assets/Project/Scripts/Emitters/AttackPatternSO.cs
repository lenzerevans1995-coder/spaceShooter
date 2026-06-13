using UnityEngine;

namespace SpaceShooter.Emitters
{
    /// <summary>
    /// Base for every authored attack — projectile patterns AND beams. One volley = one
    /// Emit() call. Designers compose enemies and bosses purely from these assets via the
    /// EmitterController timeline; no per-boss code.
    /// </summary>
    public abstract class AttackPatternSO : ScriptableObject
    {
        public abstract void Emit(in AttackContext ctx);

        /// <summary>Angle in degrees (0 = +X, increasing toward +Z) -> XZ unit vector.</summary>
        protected static Vector3 AngleToDir(float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(r), 0f, Mathf.Sin(r));
        }

        /// <summary>XZ direction -> angle in degrees (inverse of AngleToDir).</summary>
        protected static float DirToAngle(Vector3 dir) => Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
    }
}
