using UnityEngine;
using SpaceShooter.BulletHell;
using SpaceShooter.Combat;
using SpaceShooter.Emitters;

namespace SpaceShooter.Ships
{
    /// <summary>
    /// Fires an AttackPatternSO on a cadence, aimed by the owner. The SAME pattern system the
    /// enemies use (Phase 3) — so player weapons and enemy attacks are unified, and skill-tree
    /// upgrades later just swap or tune the pattern.
    /// </summary>
    public class ShipWeapon : MonoBehaviour
    {
        [SerializeField] AttackPatternSO _pattern;
        [SerializeField] Faction _faction = Faction.Player;
        [SerializeField] float _fireInterval = 0.12f;

        float _timer;
        int _shot;

        public void Configure(AttackPatternSO pattern, Faction faction, float fireInterval)
        {
            _pattern = pattern; _faction = faction; _fireInterval = fireInterval;
        }

        /// <summary>Call each frame. Fires on cadence while <paramref name="firing"/> is held.</summary>
        public void Tick(Vector3 origin, Vector3 aimDir, bool firing, float dt)
        {
            if (_pattern == null) return;
            float interval = Mathf.Max(0.0001f, _fireInterval);

            if (!firing)
            {
                // Keep the timer "ready" so the first shot on press is immediate.
                _timer = interval;
                return;
            }

            _timer += dt;
            while (_timer >= interval)
            {
                _timer -= interval;
                Fire(origin, aimDir);
            }
        }

        void Fire(Vector3 origin, Vector3 aimDir)
        {
            var ctx = new AttackContext(BulletManager.Instance, BeamManager.Instance,
                                        origin, aimDir, _faction, _shot++);
            _pattern.Emit(in ctx);
        }
    }
}
