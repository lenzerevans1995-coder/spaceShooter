using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceShooter.Progression
{
    /// <summary>The ship's authored starting stats, before any skill node is applied.</summary>
    [Serializable]
    public struct BaseStats
    {
        public float moveSpeed;
        public float fireRate;          // shots/sec
        public float damage;
        public float projectileSpeed;
        public int projectileCount;
        public float projectileRadius;
        public float maxHp;
        public float hitboxFraction;    // fraction of the measured mesh radius used as the hit radius

        public static BaseStats Default => new BaseStats
        {
            moveSpeed = 18f,
            fireRate = 7.1f,            // ~0.14s interval
            damage = 2.5f,
            projectileSpeed = 34f,
            projectileCount = 1,
            projectileRadius = 0.4f,
            maxHp = 60f,
            hitboxFraction = 0.55f,
        };
    }

    /// <summary>Final, computed stats handed to the ship/weapon each time the build changes.</summary>
    public struct PlayerStats
    {
        public float moveSpeed;
        public float fireRate;
        public float damage;
        public float projectileSpeed;
        public int projectileCount;
        public float projectileRadius;
        public float maxHp;
        public float hitboxFraction;
        public SpecialUnlock unlocks;

        /// <summary>Convenience: weapons think in seconds-between-shots, not shots/sec.</summary>
        public float FireInterval => fireRate > 0.0001f ? 1f / fireRate : 999f;
        public bool Has(SpecialUnlock u) => (unlocks & u) != 0;
    }

    /// <summary>
    /// Owns the player's progression: the set of unlocked <see cref="SkillNodeSO"/>s and the
    /// recomputed <see cref="PlayerStats"/> derived from them. Single source of truth for ship
    /// power. Recalculates by folding every unlocked node's modifiers onto <see cref="_base"/>,
    /// then raises <see cref="Changed"/> so the ship and weapon re-read their stats.
    ///
    /// WIRING (how the rest of the game consumes this):
    ///   • PlayerShip caches Current.moveSpeed / maxHp (subscribe to Changed to apply mid-run buffs).
    ///   • ShipWeapon uses Current.FireInterval for cadence and rebuilds its AttackPatternSO from
    ///     damage / projectileSpeed / projectileCount / projectileRadius. SpecialUnlock flags pick
    ///     the pattern *type* (e.g. ScatterShot => FanEmitter instead of AimedShot) and per-bullet
    ///     behavior (HomingShots => BulletBehavior.Homing, PiercingShots => skip-despawn-on-hit).
    ///   • Between waves, the upgrade UI calls TryUnlock(node); Recalculate() + Changed do the rest.
    /// </summary>
    public class PlayerStatsManager : MonoBehaviour
    {
        [SerializeField] BaseStats _base = BaseStats.Default;

        readonly HashSet<SkillNodeSO> _unlocked = new HashSet<SkillNodeSO>();

        public PlayerStats Current { get; private set; }
        public IReadOnlyCollection<SkillNodeSO> Unlocked => _unlocked;

        /// <summary>Raised whenever the unlocked set changes and stats are recomputed.</summary>
        public event Action<PlayerStats> Changed;

        void Awake() => Recalculate();

        public void SetBase(BaseStats b) { _base = b; Recalculate(); }

        public bool IsUnlocked(SkillNodeSO node) => node != null && _unlocked.Contains(node);

        public bool CanUnlock(SkillNodeSO node) =>
            node != null && !_unlocked.Contains(node) && node.PrerequisitesMet(_unlocked);

        /// <summary>Unlock a node if its prerequisites are met. Returns false otherwise.</summary>
        public bool TryUnlock(SkillNodeSO node)
        {
            if (!CanUnlock(node)) return false;
            _unlocked.Add(node);
            Recalculate();
            return true;
        }

        /// <summary>Reset to a fresh run (clears the tree). Keeps the authored base.</summary>
        public void ResetRun()
        {
            _unlocked.Clear();
            Recalculate();
        }

        /// <summary>
        /// Fold all unlocked nodes onto the base. Order within a stat: flats first, then the
        /// summed percent, then any multipliers — so ordering of nodes never changes the result.
        /// </summary>
        public void Recalculate()
        {
            // Accumulators keyed by stat.
            var flat = new Dictionary<StatType, float>();
            var pct = new Dictionary<StatType, float>();
            var mul = new Dictionary<StatType, float>();
            SpecialUnlock specials = SpecialUnlock.None;

            foreach (var node in _unlocked)
            {
                if (node == null) continue;
                specials |= node.unlocks;
                if (node.modifiers == null) continue;
                for (int i = 0; i < node.modifiers.Count; i++)
                {
                    var m = node.modifiers[i];
                    switch (m.op)
                    {
                        case ModOp.Flat: Add(flat, m.stat, m.value); break;
                        case ModOp.Percent: Add(pct, m.stat, m.value); break;
                        case ModOp.Multiply: Mul(mul, m.stat, m.value); break;
                    }
                }
            }

            Current = new PlayerStats
            {
                moveSpeed        = Compute(_base.moveSpeed,        StatType.MoveSpeed,        flat, pct, mul),
                fireRate         = Compute(_base.fireRate,         StatType.FireRate,         flat, pct, mul),
                damage           = Compute(_base.damage,           StatType.Damage,           flat, pct, mul),
                projectileSpeed  = Compute(_base.projectileSpeed,  StatType.ProjectileSpeed,  flat, pct, mul),
                projectileCount  = Mathf.Max(1, Mathf.RoundToInt(Compute(_base.projectileCount, StatType.ProjectileCount, flat, pct, mul))),
                projectileRadius = Compute(_base.projectileRadius, StatType.ProjectileRadius, flat, pct, mul),
                maxHp            = Compute(_base.maxHp,             StatType.MaxHp,            flat, pct, mul),
                hitboxFraction   = Mathf.Clamp(Compute(_base.hitboxFraction, StatType.HitboxFraction, flat, pct, mul), 0.05f, 1f),
                unlocks          = specials,
            };

            Changed?.Invoke(Current);
        }

        static void Add(Dictionary<StatType, float> d, StatType s, float v) { d.TryGetValue(s, out float c); d[s] = c + v; }
        static void Mul(Dictionary<StatType, float> d, StatType s, float v) { if (!d.TryGetValue(s, out float c)) c = 1f; d[s] = c * v; }

        static float Compute(float baseVal, StatType s,
            Dictionary<StatType, float> flat, Dictionary<StatType, float> pct, Dictionary<StatType, float> mul)
        {
            float v = baseVal;
            if (flat.TryGetValue(s, out float f)) v += f;
            if (pct.TryGetValue(s, out float p)) v *= 1f + p;
            if (mul.TryGetValue(s, out float m)) v *= m;
            return v;
        }
    }
}
