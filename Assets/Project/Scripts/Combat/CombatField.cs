using UnityEngine;

namespace SpaceShooter.Combat
{
    /// <summary>
    /// A circular play arena for one round. Ships are clamped inside its radius; the field
    /// is larger than the camera viewport, so the camera follows the player across it.
    /// </summary>
    public class CombatField : MonoBehaviour
    {
        [SerializeField] float _radius = 34f;

        public float Radius => _radius;
        public Vector3 Center => transform.position;

        public void SetRadius(float r) => _radius = r;

        /// <summary>Clamp a position to the field disc (XZ), preserving its Y.</summary>
        public Vector3 Clamp(Vector3 p)
        {
            Vector3 d = p - Center; d.y = 0f;
            float r = d.magnitude;
            if (r > _radius && r > 0.0001f) d = d / r * _radius;
            return new Vector3(Center.x + d.x, p.y, Center.z + d.z);
        }

        /// <summary>A random point inside the field, kept `margin` away from the edge.</summary>
        public Vector3 RandomPoint(float margin = 0f)
        {
            Vector2 c = Random.insideUnitCircle * Mathf.Max(0f, _radius - margin);
            return Center + new Vector3(c.x, 0f, c.y);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.5f);
            const int seg = 48;
            Vector3 prev = Center + new Vector3(_radius, 0f, 0f);
            for (int i = 1; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                Vector3 next = Center + new Vector3(Mathf.Cos(a) * _radius, 0f, Mathf.Sin(a) * _radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
