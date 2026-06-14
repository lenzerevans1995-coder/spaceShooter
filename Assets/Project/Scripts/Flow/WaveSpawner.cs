using System;
using System.Collections.Generic;
using UnityEngine;
using SpaceShooter.Ships;

namespace SpaceShooter.Flow
{
    /// <summary>
    /// Drives the "wave treadmill": on a cadence, spawns drifting enemies just above the top edge,
    /// up to a live-population cap, ramping difficulty over time. The enemies do the downward drift
    /// and self-despawn at the bottom (EnemyShip drift mode), so this just manages spawn timing and
    /// population — no arena movement, creating the infinite-scroll illusion on a fixed field.
    ///
    /// Building a hull is delegated back to the bootstrap (it owns the Synty-hull pipeline), so this
    /// stays a pure scheduler.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        Func<Vector3> _spawnPoint;             // a fresh point just above the top edge
        Func<Vector3, EnemyShip> _build;       // build + return a drift enemy at a position
        float _baseInterval = 1.4f;
        float _minInterval = 0.45f;
        float _rampSeconds = 90f;              // interval eases from base→min across this time
        int _maxAlive = 14;

        float _timer;
        float _elapsed;
        readonly List<EnemyShip> _alive = new List<EnemyShip>(32);

        public int AliveCount => _alive.Count;
        public float Elapsed => _elapsed;

        public void Configure(Func<Vector3> spawnPoint, Func<Vector3, EnemyShip> build,
                              float baseInterval, float minInterval, int maxAlive, float rampSeconds)
        {
            _spawnPoint = spawnPoint;
            _build = build;
            _baseInterval = baseInterval;
            _minInterval = minInterval;
            _maxAlive = maxAlive;
            _rampSeconds = Mathf.Max(1f, rampSeconds);
            _timer = 0.25f;   // first spawn shortly after start
        }

        void Update()
        {
            if (_build == null) return;
            float dt = Time.deltaTime;
            _elapsed += dt;

            // Drop dead/despawned enemies (drift exit deactivates them; death disables them too).
            for (int i = _alive.Count - 1; i >= 0; i--)
                if (_alive[i] == null || !_alive[i].gameObject.activeInHierarchy)
                    _alive.RemoveAt(i);

            if (_alive.Count >= _maxAlive) return;

            float t = Mathf.Clamp01(_elapsed / _rampSeconds);
            float interval = Mathf.Lerp(_baseInterval, _minInterval, t);

            _timer += dt;
            if (_timer >= interval)
            {
                _timer = 0f;
                var e = _build(_spawnPoint());
                if (e != null) _alive.Add(e);
            }
        }
    }
}
