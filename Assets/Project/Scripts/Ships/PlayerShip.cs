using UnityEngine;
using SpaceShooter.Combat;
using SpaceShooter.Inputs;
using SpaceShooter.Progression;

namespace SpaceShooter.Ships
{
    /// <summary>
    /// Player-controlled ship: analog movement clamped to the field, hull faces the aim (or
    /// motion), and fires its ShipWeapon while the aim stick / fire is held. Driven entirely
    /// by a source-agnostic ShipInput. Combat stats (speed, HP, hitbox, weapon) are driven by a
    /// PlayerStatsManager so skill-tree unlocks take effect immediately.
    /// </summary>
    public class PlayerShip : ShipActor
    {
        [Header("Movement")]
        [SerializeField] float _maxSpeed = 18f;
        [SerializeField] float _accel = 90f;
        [SerializeField] float _turnLerp = 14f;

        ShipInput _input;
        ShipWeapon _weapon;
        CombatField _field;
        Vector3 _vel;

        PlayerStatsManager _stats;
        GameObject _bulletPrefab;
        float _baseHitRadius = 1f;

        public void Setup(ShipInput input, ShipWeapon weapon, CombatField field)
        {
            _input = input; _weapon = weapon; _field = field;
        }

        /// <summary>
        /// Bind to the skill-tree stats source. Applies the current stats now and re-applies on every
        /// recalculation (unlock between waves), so upgrades are live without rebuilding the ship.
        /// `baseHitRadius` is the measured visual radius; the hitbox is that × the stats' fraction.
        /// </summary>
        public void BindStats(PlayerStatsManager stats, GameObject bulletPrefab, float baseHitRadius)
        {
            _stats = stats; _bulletPrefab = bulletPrefab; _baseHitRadius = baseHitRadius;
            if (_stats != null) { _stats.Changed += ApplyStats; ApplyStats(_stats.Current); }
        }

        void OnDestroy() { if (_stats != null) _stats.Changed -= ApplyStats; }

        void ApplyStats(PlayerStats s)
        {
            _maxSpeed = s.moveSpeed;
            SetMaxHp(s.maxHp, false);
            SetHitRadius(_baseHitRadius * s.hitboxFraction);
            if (_weapon != null && _bulletPrefab != null)
                _weapon.Configure(PlayerWeaponBuilder.Build(s, _bulletPrefab), Faction.Player, s.FireInterval);
        }

        void Update()
        {
            if (_input == null) return;
            float dt = Time.deltaTime;

            // Move.
            Vector3 move = new Vector3(_input.Move.x, 0f, _input.Move.y);
            Vector3 targetVel = Vector3.ClampMagnitude(move, 1f) * _maxSpeed;
            _vel = Vector3.MoveTowards(_vel, targetVel, _accel * dt);
            Vector3 np = _tf.position + _vel * dt;
            if (_field != null) np = _field.Clamp(np);
            _tf.position = np;

            // Face aim if aiming, else face motion.
            Vector3 aim = new Vector3(_input.Aim.x, 0f, _input.Aim.y);
            Vector3 face = aim.sqrMagnitude > 0.01f ? aim
                         : (_vel.sqrMagnitude > 0.01f ? _vel : _tf.forward);
            if (face.sqrMagnitude > 0.001f)
            {
                Quaternion tr = Quaternion.LookRotation(face.normalized, Vector3.up);
                _tf.rotation = Quaternion.Slerp(_tf.rotation, tr, _turnLerp * dt);
            }

            // Fire.
            Vector3 aimDir = aim.sqrMagnitude > 0.01f ? aim.normalized : _tf.forward;
            if (_weapon != null) _weapon.Tick(_tf.position, aimDir, _input.FireHeld, dt);
        }
    }
}
