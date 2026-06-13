using System.Collections.Generic;
using UnityEngine;
using SpaceShooter.Combat;

namespace SpaceShooter.Flow
{
    /// <summary>
    /// Tracks one round: all enemies dead = Won, player dead = Lost. The vertical-slice
    /// version of the round loop; richer planet/round progression arrives in Phase 7.
    /// </summary>
    public class RoundController
    {
        public enum State { InProgress, Won, Lost }

        public State Current { get; private set; } = State.InProgress;
        public int EnemiesAlive { get; private set; }
        public int EnemiesTotal { get; private set; }

        public void Begin(ShipActor player, List<ShipActor> enemies)
        {
            EnemiesTotal = enemies.Count;
            EnemiesAlive = enemies.Count;

            foreach (var e in enemies)
                e.Died += OnEnemyDied;

            if (player != null) player.Died += _ => { if (Current == State.InProgress) Current = State.Lost; };
        }

        void OnEnemyDied(ShipActor _)
        {
            EnemiesAlive = Mathf.Max(0, EnemiesAlive - 1);
            if (EnemiesAlive <= 0 && Current == State.InProgress)
                Current = State.Won;
        }
    }
}
