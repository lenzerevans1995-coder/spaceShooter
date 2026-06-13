using UnityEngine;
using SpaceShooter.Combat;

namespace SpaceShooter.Test
{
    /// <summary>
    /// Minimal ITarget for verifying bullet + beam collision before real ships exist
    /// (Phase 4). Registers with the shared TargetRegistry, takes damage, and flashes.
    /// </summary>
    public class TestDummyTarget : MonoBehaviour, ITarget
    {
        [SerializeField] Faction _faction = Faction.Player;
        [SerializeField] float _maxHp = 100f;
        [SerializeField] float _hitRadius = 1f;

        float _hp;
        Transform _tf;
        Renderer _renderer;
        MaterialPropertyBlock _mpb;
        float _flash;

        public Faction Faction => _faction;
        public Vector3 Position => _tf.position;
        public float HitRadius => _hitRadius;
        public bool IsAlive => _hp > 0f;
        public float Hp => _hp;

        void Awake()
        {
            _tf = transform;
            _hp = _maxHp;
            _renderer = GetComponentInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
        }

        void OnEnable() => TargetRegistry.Register(this);
        void OnDisable() => TargetRegistry.Unregister(this);

        /// <summary>Runtime setup (sets HP too — avoids the Awake-captured-default pitfall).</summary>
        public void Init(Faction faction, float maxHp, float hitRadius)
        {
            _faction = faction;
            _maxHp = maxHp;
            _hitRadius = hitRadius;
            _hp = maxHp;
        }

        public void ApplyDamage(float amount)
        {
            if (_hp <= 0f) return;
            _hp -= amount;
            _flash = 1f;
            if (_hp <= 0f) gameObject.SetActive(false);
        }

        void Update()
        {
            if (_flash > 0f && _renderer != null)
            {
                _flash = Mathf.Max(0f, _flash - Time.deltaTime * 4f);
                _mpb.SetColor("_BaseColor", Color.Lerp(Color.white, Color.red, _flash));
                _renderer.SetPropertyBlock(_mpb);
            }
        }
    }
}
