using UnityEngine;

namespace SpaceShooter.CameraSystem
{
    /// <summary>
    /// Perspective follow camera for the top-down arena. Sits above and behind the target at
    /// a fixed downward tilt and smoothly chases it, with optional look-ahead toward motion.
    /// Tuned for landscape; the field is larger than the viewport so the player explores it.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        [SerializeField] Transform _target;
        [SerializeField] float _tilt = 62f;       // degrees down from horizontal
        [SerializeField] float _distance = 40f;   // along the view ray from the focus point
        [SerializeField] float _follow = 9f;       // higher = snappier
        [SerializeField] float _lookAhead = 3.5f;  // units ahead of the target's motion

        Vector3 _lastTargetPos;
        Vector3 _smoothLead;

        public void SetTarget(Transform t)
        {
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
            if (_target == null) return;

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
