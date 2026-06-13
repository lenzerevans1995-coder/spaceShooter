using UnityEngine;
using SpaceShooter.Combat;

namespace SpaceShooter.BulletHell
{
    /// <summary>
    /// A sustained line hazard with three phases: Telegraph (thin warning, no damage) →
    /// Active (full width, damage-per-second, optionally sweeping) → Fade. Collision is run
    /// by the BeamManager; this component only advances phase state and drives the visual
    /// (a view-aligned LineRenderer so it reads from any camera angle).
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class Beam : MonoBehaviour
    {
        enum Phase { Telegraph, Active, Fade }

        Phase _phase;
        float _t;                       // time within current phase
        Vector3 _origin;
        float _startAngle, _sweep, _length, _width, _dps;
        float _telegraph, _active, _fade;
        Faction _faction;
        Color _color;

        LineRenderer _lr;
        Material _mat;

        public Faction Faction => _faction;
        public float Dps => _dps;
        public Vector3 Origin => _origin;
        public Vector3 Dir { get; private set; }
        public float Length => _length;
        public float DamageHalfWidth { get; private set; }
        public bool IsDamaging => _phase == Phase.Active;

        void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            _lr.useWorldSpace = true;
            _lr.positionCount = 2;
            _lr.numCapVertices = 4;
            _lr.alignment = LineAlignment.View;
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            _mat = new Material(sh);
            _lr.material = _mat;
        }

        public void Spawn(Vector3 origin, float startAngle, float length, float width,
                          float telegraph, float active, float fade, float sweep,
                          float dps, Color color, Faction faction)
        {
            _origin = origin;
            _startAngle = startAngle;
            _length = length;
            _width = width;
            _telegraph = Mathf.Max(0.0001f, telegraph);
            _active = Mathf.Max(0.0001f, active);
            _fade = Mathf.Max(0.0001f, fade);
            _sweep = sweep;
            _dps = dps;
            _color = color;
            _faction = faction;

            _phase = Phase.Telegraph;
            _t = 0f;
            UpdateGeometry(0f);
            ApplyVisual();
        }

        /// <summary>Advance one frame. Returns false once the fade completes.</summary>
        public bool Tick(float dt)
        {
            _t += dt;
            // Sequential (not else-if) so a large dt can cross multiple phases in one frame.
            if (_phase == Phase.Telegraph && _t >= _telegraph) { _phase = Phase.Active; _t -= _telegraph; }
            if (_phase == Phase.Active && _t >= _active) { _phase = Phase.Fade; _t -= _active; }
            if (_phase == Phase.Fade && _t >= _fade) return false;

            float activeProgress = _phase == Phase.Active ? Mathf.Clamp01(_t / _active)
                                 : _phase == Phase.Fade ? 1f
                                 : 0f;
            UpdateGeometry(activeProgress);
            ApplyVisual();
            return true;
        }

        void UpdateGeometry(float activeProgress)
        {
            float angle = _startAngle + _sweep * activeProgress;
            float r = angle * Mathf.Deg2Rad;
            Dir = new Vector3(Mathf.Cos(r), 0f, Mathf.Sin(r));
            _lr.SetPosition(0, _origin);
            _lr.SetPosition(1, _origin + Dir * _length);
        }

        void ApplyVisual()
        {
            float w, a;
            switch (_phase)
            {
                case Phase.Telegraph: w = _width * 0.18f; a = 0.35f; break;
                case Phase.Active:    w = _width;         a = 0.9f;  break;
                default:              w = _width;         a = 0.9f * (1f - Mathf.Clamp01(_t / _fade)); break;
            }
            DamageHalfWidth = _width * 0.5f;
            _lr.startWidth = w;
            _lr.endWidth = w;
            Color c = _color; c.a = a;
            _lr.startColor = c;
            _lr.endColor = c;
        }
    }
}
