using UnityEngine;

namespace SpaceShooter.Core
{
    /// <summary>
    /// Persistent, single-instance driver for the whole game flow. Lives in the Core
    /// scene and survives additive Combat scene loads. Owns exactly one active
    /// IGameState and pumps it every frame. Nothing else should call SceneManager
    /// directly — route all flow transitions through here.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameDirector : MonoBehaviour
    {
        public static GameDirector Instance { get; private set; }

        public IGameState CurrentState { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            // Phase 2: boot through stub states to prove the machine. Real flow
            // (planet select -> rounds -> upgrades -> boss) is wired in later phases.
            ChangeState(new BootState(this));
        }

        void Update()
        {
            CurrentState?.Tick(Time.deltaTime);
        }

        /// <summary>Swap the active state, running Exit on the old and Enter on the new.</summary>
        public void ChangeState(IGameState next)
        {
            CurrentState?.Exit();
            CurrentState = next;
            CurrentState?.Enter();
        }
    }
}
