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

        Transform[] _firePoints;   // optional muzzles from ShipHardpoints; null = fire from ship center
        float _timer;
        int _shot;

        public void Configure(AttackPatternSO pattern, Faction faction, float fireInterval)
        {
            _pattern = pattern; _faction = faction; _fireInterval = fireInterval;
        }

        /// <summary>Assign muzzles so shots originate from the hull's authored fire points.</summary>
        public void SetFirePoints(Transform[] firePoints) => _firePoints = firePoints;

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
            if (_firePoints != null && _firePoints.Length > 0)
            {
                // One volley per muzzle, spawned at the muzzle, aimed by the ship.
                for (int i = 0; i < _firePoints.Length; i++)
                {
                    var fp = _firePoints[i];
                    if (fp != null) Emit(fp.position, aimDir);
                }
            }
            else
            {
                Emit(origin, aimDir);
            }
        }

        void Emit(Vector3 pos, Vector3 aimDir)
        {
            var ctx = new AttackContext(BulletManager.Instance, BeamManager.Instance,
                                        pos, aimDir, _faction, _shot++);
            _pattern.Emit(in ctx);
        }
    }
}
