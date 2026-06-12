using UnityEngine;
using RacingGame.Core;
namespace RacingGame.Core
{
    /// <summary>
    /// The central brain of the game. 
    /// It holds references to all major systems and manages the Game Phase.
    /// Attach this to an empty GameObject named "GameManager" in your main scene.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        [Header("Game State")]
        public GamePhase CurrentPhase { get; private set; } = GamePhase.MainMenu;
        [Header("System References (Drag in Inspector)")]
        // public CareerManager CareerManager;
        // public RaceDirector RaceDirector;
        // public UIManager UIManager;
        void Awake()
        {
            // Singleton pattern - ensures only one GameManager exists
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // Survives scene changes
            // Set mobile performance targets
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Debug.Log("[GameManager] Foundation initialized. Ready for systems.");
        }
        /// <summary>
        /// Call this to change the game state (e.g., from Hub to Race).
        /// </summary>
        public void SetGamePhase(GamePhase newPhase)
        {
            CurrentPhase = newPhase;
            Debug.Log($"[GameManager] Phase changed to: {newPhase}");
            // Future: Trigger UI updates based on phase
            // if (UIManager != null) UIManager.OnPhaseChanged(newPhase);
        }
        // Example: How systems will talk to each other later
        public void StartRace()
        {
            SetGamePhase(GamePhase.InRace);
            // RaceDirector.StartRace();
        }
        public void ReturnToHub()
        {
            SetGamePhase(GamePhase.CareerHub);
            // SceneManager.LoadScene("CareerScene");
        }
    }
}
