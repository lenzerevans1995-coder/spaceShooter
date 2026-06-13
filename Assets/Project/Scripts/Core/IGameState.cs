namespace SpaceShooter.Core
{
    /// <summary>
    /// A single state in the GameDirector's high-level flow
    /// (GalaxyMap, Combat, Upgrade, BossFight, ...). States are plain C# objects;
    /// the GameDirector owns their lifecycle and is the only thing that transitions.
    /// </summary>
    public interface IGameState
    {
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }
}
