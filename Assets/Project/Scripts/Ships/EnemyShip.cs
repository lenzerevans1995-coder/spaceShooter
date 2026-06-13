using UnityEngine;
using SpaceShooter.Combat;

namespace SpaceShooter.Ships
{
    /// <summary>
    /// Enemy ship AI: holds a preferred range from the player (approach when far, back off
    /// when close), strafes sideways, and faces the player. Firing is handled by an
    /// EmitterController (Phase 3 pattern timeline) aimed at the player, added separately.
    /// </summary>
    public class EnemyShip : ShipActor
    {
        [Header("AI movement")]
        [SerializeField] float _moveSpeed = 9f;
        [SerializeField] float _accel = 40f;
        [SerializeField] float _desiredRange = 18f;
        [SerializeField] float _strafe = 0.6f;
        [SerializeField] float _strafeFlipEvery = 2.5f;
        [SerializeField] float _turnLerp = 6f;

        Transform _target;
        CombatField _field;
        Vector3 _vel;
        float _strafeDir = 1f;
        float _strafeTimer;

        public void Setup(Transform target, CombatField field)
        {
            _target = target; _field = field;
            _strafeDir = Random.value < 0.5f ? -1f : 1f;
        }

        void Update()
        {
            if (_target == null) return;
            float dt = Time.deltaTime;

            Vector3 to = _target.position - _tf.position; to.y = 0f;
            float dist = to.magnitude;
            Vector3 dir = dist > 0.01f ? to / dist : _tf.forward;

            Vector3 desired = Vector3.zero;
            if (dist > _desiredRange + 2f) desired += dir;          // approach
            else if (dist < _desiredRange - 2f) desired -= dir;     // retreat

            _strafeTimer += dt;
            if (_strafeTimer >= _strafeFlipEvery) { _strafeTimer = 0f; _strafeDir *= -1f; }
            Vector3 perp = new Vector3(-dir.z, 0f, dir.x);
            desired += perp * (_strafeDir * _strafe);

            Vector3 targetVel = Vector3.ClampMagnitude(desired, 1f) * _moveSpeed;
            _vel = Vector3.MoveTowards(_vel, targetVel, _accel * dt);
            Vector3 np = _tf.position + _vel * dt;
            if (_field != null) np = _field.Clamp(np);
            _tf.position = np;

            if (dir.sqrMagnitude > 0.001f)
                _tf.rotation = Quaternion.Slerp(_tf.rotation, Quaternion.LookRotation(dir, Vector3.up), _turnLerp * dt);
        }
    }
}
