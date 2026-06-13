using UnityEngine;
using SpaceShooter.Combat;

namespace SpaceShooter.BulletHell
{
    /// <summary>Optional per-bullet steering applied each tick on top of base speed/accel.</summary>
    public enum BulletBehavior
    {
        Straight = 0,   // uses the constant TurnRate (0 = dead straight)
        Homing = 1,     // steers toward nearest opposing target (param1 = turn deg/s)
        SineWeave = 2   // oscillates around its launch heading (param1 = freq, param2 = amplitude deg)
    }

    /// <summary>
    /// Everything needed to launch one bullet. Passed by readonly ref (`in`) so patterns
    /// can fill a template once and tweak per-bullet without struct copies.
    /// </summary>
    public struct BulletSpawnParams
    {
        public GameObject prefab;
        public Vector3 position;
        public Vector3 direction;
        public float speed;
        public float accel;
        public float turnRate;
        public float damage;
        public float radius;
        public float lifetime;
        public Faction faction;
        public BulletBehavior behavior;
        public float behaviorParam1;
        public float behaviorParam2;
    }
}
