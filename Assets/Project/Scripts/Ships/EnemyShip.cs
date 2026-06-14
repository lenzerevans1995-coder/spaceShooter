using UnityEngine;
using SpaceShooter.Combat;

namespace SpaceShooter.Ships
{
    /// <summary>
    /// Enemy ship AI with two movement modes:
    ///  - Seek (default): holds a preferred range from the player, strafes, faces the player.
    ///  - Drift (treadmill): enters from above the top edge and drifts DOWN the screen (-Z) with a
    ///    gentle horizontal lean toward the player, despawning once it crosses the bottom edge. This
    ///    drives the portrait "wave treadmill" survival loop without moving the arena.
    /// Both modes fold in <see cref="EnemySeparation"/> so a wave spreads into a swarm. Firing is an
    /// EmitterController aimed at the player, added separately.
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

        [Header("Drift (treadmill) mode")]
        [SerializeField] float _driftSpeed = 6f;
        [Tooltip("0 = straight down, 1 = strongly leans toward the player's column.")]
        [SerializeField, Range(0f, 1f)] float _horizontalChase = 0.18f;
        [SerializeField] float _despawnMargin = 4f;

        Transform _target;
        CombatField _field;
        EnemySeparation _sep;
        Vector3 _vel;
        float _strafeDir = 1f;
        float _strafeTimer;
        bool _drift;

        public void Setup(Transform target, CombatField field)
        {
            _target = target; _field = field;
            _sep = GetComponent<EnemySeparation>();
            _strafeDir = Random.value < 0.5f ? -1f : 1f;
        }

        /// <summary>Switch to treadmill drift-down behaviour (enters top, exits bottom).</summary>
        public void SetDrift(float downSpeed)
        {
            _drift = true;
            _driftSpeed = downSpeed;
        }

        void Update()
        {
            if (_target == null) return;
            float dt = Time.deltaTime;

            Vector3 to = _target.position - _tf.position; to.y = 0f;
            float dist = to.magnitude;
            Vector3 dir = dist > 0.01f ? to / dist : _tf.forward;

            Vector3 desiredVel;
            if (_drift)
            {
                Vector3 v = Vector3.back * _driftSpeed;                       // down the screen (-Z)
                float dx = _target.position.x - _tf.position.x;
                v.x += Mathf.Clamp(dx, -1f, 1f) * _driftSpeed * _horizontalChase;
                desiredVel = v;
            }
            else
            {
                Vector3 desired = Vector3.zero;
                if (dist > _desiredRange + 2f) desired += dir;               // approach
                else if (dist < _desiredRange - 2f) desired -= dir;          // retreat

                _strafeTimer += dt;
                if (_strafeTimer >= _strafeFlipEvery) { _strafeTimer = 0f; _strafeDir *= -1f; }
                Vector3 perp = new Vector3(-dir.z, 0f, dir.x);
                desired += perp * (_strafeDir * _strafe);
                desiredVel = Vector3.ClampMagnitude(desired, 1f) * _moveSpeed;
            }

            if (_sep != null) desiredVel += _sep.Compute();                  // Boids separation

            _vel = Vector3.MoveTowards(_vel, desiredVel, _accel * dt);
            Vector3 np = _tf.position + _vel * dt;

            if (_drift)
            {
                // Clamp to the field WIDTH only; Z flows freely so they ride the treadmill off-screen.
                if (_field != null)
                {
                    float ex = _field.Extents.x;
                    np.x = Mathf.Clamp(np.x, _field.Center.x - ex, _field.Center.x + ex);
                    if (np.z < _field.BottomEdgeZ - _despawnMargin) { Despawn(); return; }
                }
            }
            else if (_field != null)
            {
                np = _field.Clamp(np);
            }
            _tf.position = np;

            if (dir.sqrMagnitude > 0.001f)
                _tf.rotation = Quaternion.Slerp(_tf.rotation, Quaternion.LookRotation(dir, Vector3.up), _turnLerp * dt);
        }

        /// <summary>Leave the treadmill at the bottom: deactivate WITHOUT counting as a player kill.</summary>
        void Despawn()
        {
            _vel = Vector3.zero;
            gameObject.SetActive(false);   // OnDisable unregisters from TargetRegistry
        }
    }
}
