using UnityEngine;

namespace SpaceShooter.Core
{
    /// <summary>Entry state. Initializes core services, then hands off to the meta map.</summary>
    public class BootState : IGameState
    {
        readonly GameDirector _director;
        public BootState(GameDirector director) { _director = director; }

        public void Enter()
        {
            Debug.Log("[GameDirector] Boot -> core services up.");
            _director.ChangeState(new GalaxyMapState(_director));
        }

        public void Tick(float dt) { }
        public void Exit() { }
    }

    /// <summary>Planet / level selection meta screen (stub — UI wired in Phase 6/7).</summary>
    public class GalaxyMapState : IGameState
    {
        readonly GameDirector _director;
        public GalaxyMapState(GameDirector director) { _director = director; }

        public void Enter() { Debug.Log("[GameDirector] Entered GalaxyMap (stub)."); }
        public void Tick(float dt) { }
        public void Exit() { }
    }

    /// <summary>Active round: enemies spawn and bullets fly (shell for Phase 3/4).</summary>
    public class CombatState : IGameState
    {
        readonly GameDirector _director;
        public CombatState(GameDirector director) { _director = director; }

        public void Enter() { Debug.Log("[GameDirector] Entered Combat (stub)."); }
        public void Tick(float dt) { }
        public void Exit() { }
    }
}
