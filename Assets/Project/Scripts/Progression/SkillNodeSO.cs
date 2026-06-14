using System.Collections.Generic;
using UnityEngine;

namespace SpaceShooter.Progression
{
    /// <summary>Which base stat a modifier touches. Maps to fields on <see cref="PlayerStats"/>.</summary>
    public enum StatType
    {
        MoveSpeed,
        FireRate,         // shots/sec scalar (higher = faster; converted to fireInterval)
        Damage,
        ProjectileSpeed,
        ProjectileCount,  // bolts per shot (rounded)
        ProjectileRadius, // bullet hit radius (thicker shots)
        MaxHp,
        HitboxFraction,   // smaller = easier to dodge/graze
    }

    /// <summary>How a modifier combines with the running value for its stat.</summary>
    public enum ModOp
    {
        Flat,     // += value
        Percent,  // accumulates; applied once as *(1 + sum) — so two +20% = +40%, not +44%
        Multiply, // *= value (compounding; use sparingly for big power spikes)
    }

    /// <summary>Special, non-numeric unlocks a node can grant. Flags so a build can stack several.</summary>
    [System.Flags]
    public enum SpecialUnlock
    {
        None          = 0,
        ScatterShot   = 1 << 0,  // swaps the aimed bolt for a spread fan
        PiercingShots = 1 << 1,  // bullets pass through the first target
        HomingShots   = 1 << 2,  // bullets curve toward enemies
        SideCannons   = 1 << 3,  // extra rear/side fire points
        Shield        = 1 << 4,  // absorbs one hit, recharges
    }

    [System.Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public ModOp op;
        public float value;
    }

    /// <summary>
    /// One upgrade in the roguelike skill tree. Pure data: a set of stat modifiers and/or special
    /// unlocks, gated behind prerequisite nodes. <see cref="PlayerStatsManager"/> reads the unlocked
    /// set and folds every node's modifiers into the ship's final stats. Authored as assets so
    /// designers build trees in the inspector without touching code.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillNode", menuName = "SpaceShooter/Skill Node", order = 0)]
    public class SkillNodeSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique id used for save/load (NOT the asset name).")]
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Tree shape")]
        [Tooltip("Every node here must be unlocked before this node can be taken.")]
        public List<SkillNodeSO> prerequisites = new List<SkillNodeSO>();
        [Tooltip("Skill-point / currency cost to unlock.")]
        public int cost = 1;

        [Header("Effect")]
        public List<StatModifier> modifiers = new List<StatModifier>();
        public SpecialUnlock unlocks = SpecialUnlock.None;

        /// <summary>True if every prerequisite is in the unlocked set (empty prereqs = a root node).</summary>
        public bool PrerequisitesMet(ICollection<SkillNodeSO> unlocked)
        {
            if (prerequisites == null) return true;
            for (int i = 0; i < prerequisites.Count; i++)
            {
                var p = prerequisites[i];
                if (p != null && !unlocked.Contains(p)) return false;
            }
            return true;
        }
    }
}
