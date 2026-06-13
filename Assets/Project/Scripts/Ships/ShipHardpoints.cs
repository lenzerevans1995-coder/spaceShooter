using System.Collections.Generic;
using UnityEngine;

namespace SpaceShooter.Ships
{
    /// <summary>
    /// Per-ship mount points: where weapons fire from (muzzles) and where thruster VFX attach.
    /// Different hulls have different layouts. Author these visually with the ShipHardpoints
    /// editor tool (Add buttons + Scene-view handles). Points are child Transforms, so they
    /// move and rotate with the ship; a fire point shoots along its forward (+Z), a thruster
    /// emits along its back (-Z).
    /// </summary>
    public class ShipHardpoints : MonoBehaviour
    {
        [Tooltip("Muzzles — projectiles spawn here. Each fires along the point's +Z (forward arrow).")]
        public List<Transform> firePoints = new List<Transform>();

        [Tooltip("Engine mounts — thruster VFX attach here, emitting along -Z (back).")]
        public List<Transform> thrusterPoints = new List<Transform>();

        public int FireCount => firePoints != null ? firePoints.Count : 0;
        public int ThrusterCount => thrusterPoints != null ? thrusterPoints.Count : 0;

        /// <summary>Live fire-point transforms (skips any null/removed entries).</summary>
        public IEnumerable<Transform> ValidFirePoints()
        {
            if (firePoints == null) yield break;
            for (int i = 0; i < firePoints.Count; i++) if (firePoints[i] != null) yield return firePoints[i];
        }

        public IEnumerable<Transform> ValidThrusterPoints()
        {
            if (thrusterPoints == null) yield break;
            for (int i = 0; i < thrusterPoints.Count; i++) if (thrusterPoints[i] != null) yield return thrusterPoints[i];
        }

        void OnDrawGizmos()
        {
            if (firePoints != null)
                foreach (var f in firePoints)
                    if (f != null)
                    {
                        Gizmos.color = new Color(1f, 0.4f, 0.3f);
                        Gizmos.DrawSphere(f.position, 0.12f);
                        Gizmos.DrawRay(f.position, f.forward * 0.7f);
                    }

            if (thrusterPoints != null)
                foreach (var t in thrusterPoints)
                    if (t != null)
                    {
                        Gizmos.color = new Color(0.3f, 0.8f, 1f);
                        Gizmos.DrawSphere(t.position, 0.12f);
                        Gizmos.DrawRay(t.position, -t.forward * 0.7f);
                    }
        }
    }
}
