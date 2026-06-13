using UnityEngine;

namespace SpaceShooter.Combat
{
    /// <summary>
    /// Shared base for player and enemy ships: health, faction, and ITarget registration so
    /// bullets and beams can hit them via the shared TargetRegistry. Movement/AI/firing live
    /// in the concrete subclasses.
    /// </summary>
    public abstract class ShipActor : MonoBehaviour, ITarget
    {
        [SerializeField] protected Faction _faction = Faction.Enemy;
        [SerializeField] protected float _maxHp = 30f;
        [SerializeField] protected float _hitRadius = 1.2f;

        protected float _hp;
        protected Transform _tf;

        public Faction Faction => _faction;
        public Vector3 Position => _tf.position;
        public float HitRadius => _hitRadius;
        public bool IsAlive => _hp > 0f;
        public float Hp => _hp;
        public float MaxHp => _maxHp;

        /// <summary>Raised once when this ship dies. Argument is the ship that died.</summary>
        public event System.Action<ShipActor> Died;

        protected virtual void Awake()
        {
            _tf = transform;
            _hp = _maxHp;
        }

        protected virtual void OnEnable() => TargetRegistry.Register(this);
        protected virtual void OnDisable() => TargetRegistry.Unregister(this);

        /// <summary>Runtime setup (sets HP too, avoiding the Awake-captured-default pitfall).</summary>
        public void Configure(Faction faction, float maxHp, float hitRadius)
        {
            _faction = faction;
            _maxHp = maxHp;
            _hitRadius = hitRadius;
            _hp = maxHp;
        }

        public virtual void ApplyDamage(float amount)
        {
            if (_hp <= 0f) return;
            _hp -= amount;
            if (_hp <= 0f)
            {
                _hp = 0f;
                OnDeath();
            }
        }

        protected virtual void OnDeath()
        {
            Died?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
