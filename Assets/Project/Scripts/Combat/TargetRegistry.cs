using System.Collections.Generic;
using UnityEngine;

namespace SpaceShooter.Combat
{
    /// <summary>
    /// Shared registry of hittable targets, queried each frame by BOTH the BulletManager
    /// and BeamManager. Static so any hazard system can see targets without wiring; reset
    /// on play so stale entries never leak between sessions.
    /// </summary>
    public static class TargetRegistry
    {
        static readonly List<ITarget> _targets = new(64);

        public static IReadOnlyList<ITarget> Targets => _targets;

        public static void Register(ITarget t)
        {
            if (t != null && !_targets.Contains(t)) _targets.Add(t);
        }

        public static void Unregister(ITarget t) => _targets.Remove(t);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState() => _targets.Clear();
    }
}
