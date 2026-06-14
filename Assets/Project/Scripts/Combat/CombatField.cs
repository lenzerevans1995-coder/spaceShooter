using UnityEngine;

namespace SpaceShooter.Combat
{
    /// <summary>
    /// A rectangular, portrait-oriented play arena for one round. Ships are clamped inside the
    /// box (half-width on X, half-height on Z). The camera frames the whole field statically, so
    /// the field is sized to the viewport rather than scrolled. Enemies/asteroids are biased to
    /// enter from the top edge and pressure downward (Archero / Vampire-Survivors feel).
    /// </summary>
    public class CombatField : MonoBehaviour
    {
        [Header("Field half-size (X = half-width, Z = half-height). Portrait => Z > X.")]
        [SerializeField] Vector2 _extents = new Vector2(13f, 26f);

        public Vector2 Extents => _extents;
        public Vector3 Center => transform.position;

        /// <summary>World Z of the top edge (where enemies/asteroids enter).</summary>
        public float TopEdgeZ => Center.z + _extents.y;
        /// <summary>World Z of the bottom edge.</summary>
        public float BottomEdgeZ => Center.z - _extents.y;

        public void SetExtents(Vector2 extents) => _extents = extents;

        /// <summary>Clamp a position to the rectangular field (XZ), preserving its Y.</summary>
        public Vector3 Clamp(Vector3 p)
        {
            Vector3 d = p - Center;
            d.x = Mathf.Clamp(d.x, -_extents.x, _extents.x);
            d.z = Mathf.Clamp(d.z, -_extents.y, _extents.y);
            return new Vector3(Center.x + d.x, p.y, Center.z + d.z);
        }

        /// <summary>A random point inside the field, kept `margin` away from every edge.</summary>
        public Vector3 RandomPoint(float margin = 0f)
        {
            float sx = Mathf.Max(0f, _extents.x - margin);
            float sz = Mathf.Max(0f, _extents.y - margin);
            return Center + new Vector3(Random.Range(-sx, sx), 0f, Random.Range(-sz, sz));
        }

        /// <summary>
        /// A spawn point along the top of the field for entering enemies/asteroids. `inset` pulls
        /// it down from the very top edge (use the field interior); pass a negative inset to spawn
        /// just ABOVE the top edge so things drift on-screen. `sideMargin` keeps it off the L/R walls.
        /// </summary>
        public Vector3 TopSpawnPoint(float inset = 0f, float sideMargin = 1.5f)
        {
            float sx = Mathf.Max(0f, _extents.x - sideMargin);
            float z = Center.z + _extents.y - inset;
            return new Vector3(Center.x + Random.Range(-sx, sx), 0f, z);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.6f);
            Vector3 c = Center;
            Vector3 tl = c + new Vector3(-_extents.x, 0f, _extents.y);
            Vector3 tr = c + new Vector3(_extents.x, 0f, _extents.y);
            Vector3 bl = c + new Vector3(-_extents.x, 0f, -_extents.y);
            Vector3 br = c + new Vector3(_extents.x, 0f, -_extents.y);
            Gizmos.DrawLine(tl, tr); Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl); Gizmos.DrawLine(bl, tl);
        }
    }
}
