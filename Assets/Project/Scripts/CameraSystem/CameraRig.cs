using UnityEngine;

namespace SpaceShooter.CameraSystem
{
    /// <summary>
    /// Perspective camera for the portrait arena. Two modes:
    ///  - Static (FrameStatic): keeps the downward tilt for 3D depth but does NOT follow — it
    ///    computes a distance that frames the whole rectangular field for the current aspect.
    ///    This is the Archero / Vampire-Survivors fixed-arena look.
    ///  - Follow (SetTarget): legacy tilted chase camera, kept for landscape/free-scroll modes.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        [SerializeField] Transform _target;
        [SerializeField] float _tilt = 62f;        // degrees down from horizontal
        [SerializeField] float _distance = 40f;    // along the view ray from the focus point
        [SerializeField] float _follow = 9f;        // higher = snappier (follow mode)
        [SerializeField] float _lookAhead = 3.5f;   // units ahead of motion (follow mode)

        [Header("Static framing")]
        [SerializeField, Range(0.6f, 1f)] float _fill = 0.92f;  // 1 = field touches screen edges

        Camera _cam;
        bool _static;
        Vector3 _lastTargetPos;
        Vector3 _smoothLead;

        Camera Cam => _cam != null ? _cam : (_cam = GetComponent<Camera>());

        /// <summary>
        /// Lock the camera to statically frame a rectangular field, keeping the tilt for depth.
        /// `center` is the field centre (XZ on the ground), `extents` its half-width/half-height.
        /// </summary>
        public void FrameStatic(Vector3 center, Vector2 extents)
        {
            _static = true;
            _target = null;
            Quaternion rot = Quaternion.Euler(_tilt, 0f, 0f);
            Vector3 viewDir = rot * Vector3.forward;
            transform.rotation = rot;

            // The four ground corners we must keep on-screen.
            Vector3[] corners =
            {
                center + new Vector3(-extents.x, 0f,  extents.y),
                center + new Vector3( extents.x, 0f,  extents.y),
                center + new Vector3(-extents.x, 0f, -extents.y),
                center + new Vector3( extents.x, 0f, -extents.y),
            };

            // Proportionally adjust distance until the worst corner sits at `_fill` of the frame.
            float d = Mathf.Max(5f, _distance);
            for (int iter = 0; iter < 10; iter++)
            {
                transform.position = center - viewDir * d;
                float maxNdc = 0.0001f;
                for (int i = 0; i < corners.Length; i++)
                {
                    Vector3 vp = Cam.WorldToViewportPoint(corners[i]);
                    if (vp.z <= 0f) { maxNdc = 10f; break; }   // behind camera => push back hard
                    float ndc = Mathf.Max(Mathf.Abs(vp.x - 0.5f), Mathf.Abs(vp.y - 0.5f)) * 2f;
                    if (ndc > maxNdc) maxNdc = ndc;
                }
                d *= maxNdc / _fill;
            }
            _distance = d;
            transform.position = center - viewDir * d;
        }

        public void SetTarget(Transform t)
        {
            _static = false;
            _target = t;
            if (t != null)
            {
                _lastTargetPos = t.position;
                Quaternion rot = Quaternion.Euler(_tilt, 0f, 0f);
                transform.position = t.position - (rot * Vector3.forward) * _distance;
                transform.rotation = rot;
            }
        }

        void LateUpdate()
        {
            if (_static || _target == null) return;

            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 vel = (_target.position - _lastTargetPos) / dt;
            _lastTargetPos = _target.position;

            Vector3 lead = vel * 0.15f;
            lead.y = 0f;
            lead = Vector3.ClampMagnitude(lead, _lookAhead);
            _smoothLead = Vector3.Lerp(_smoothLead, lead, 1f - Mathf.Exp(-4f * dt));

            Quaternion rot = Quaternion.Euler(_tilt, 0f, 0f);
            Vector3 focus = _target.position + _smoothLead;
            Vector3 desired = focus - (rot * Vector3.forward) * _distance;

            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-_follow * dt));
            transform.rotation = rot;
        }
    }
}
