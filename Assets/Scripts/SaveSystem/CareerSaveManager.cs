using UnityEngine;
using RacingGame.Career;
namespace RacingGame.SaveSystem
{
    /// <summary>
    /// High-level manager that integrates the save system with career mode.
    /// Provides easy-to-use save/load methods for UI and game logic.
    /// </summary>
    public class CareerSaveManager : MonoBehaviour
    {
        public static CareerSaveManager Instance { get; private set; }
        [Header("Settings")]
        public bool autoSaveAfterRace = true;
        public bool autoSaveAfterSeason = true;
        [Header("Current State")]
        public string currentSlotName;
        public WorldSaveData currentWorldState;
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        /// <summary>
        /// Start a new career and create initial save
        /// </summary>
        public void StartNewCareer(string slotName, DriverCareerSave playerCareer, PlayerCareerContext context)
        {
            currentSlotName = slotName;
            // Capture initial world state
            currentWorldState = WorldStateSerializer.CaptureWorld(
                currentSeason: 1,
                currentRound: 1,
                phase: CareerPhase.PreSeason,
                playerCareer: playerCareer,
                playerContext: context
            );
            // Save immediately
            SaveCurrentState();
            Debug.Log($"🏁 [CareerSaveManager] New career started: {slotName}");
        }
        /// <summary>
        /// Load an existing career
        /// </summary>
        public bool LoadCareer(string slotName)
        {
            var data = WorldSaveSystem.LoadWorld(slotName);
            if (data == null)
            {
                Debug.LogError($"[CareerSaveManager] Failed to load career: {slotName}");
                return false;
            }
            currentSlotName = slotName;
            currentWorldState = data;
            // Restore world state
            bool success = WorldStateRestorer.RestoreWorld(data);
            if (success)
            {
                Debug.Log($"✅ [CareerSaveManager] Career loaded: {slotName} (Season {data.currentSeason})");
            }
            return success;
        }
        /// <summary>
        /// Save current state to disk
        /// </summary>
        public void SaveCurrentState()
        {
            if (string.IsNullOrEmpty(currentSlotName))
            {
                Debug.LogWarning("[CareerSaveManager] No active career to save");
                return;
            }
            // Update player career in world state
            if (currentWorldState != null && currentWorldState.playerCareer != null)
            {
                // Sync player career data
                currentWorldState.playerCareer.lastSaved = System.DateTime.Now;
            }
            WorldSaveSystem.SaveWorld(currentWorldState, currentSlotName);
        }
        /// <summary>
        /// Auto-save after a race
        /// </summary>
        public void AutoSaveAfterRace(int newRound)
        {
            if (!autoSaveAfterRace) return;
            if (currentWorldState != null)
            {
                currentWorldState.currentRound = newRound;
                SaveCurrentState();
            }
        }
        /// <summary>
        /// Auto-save after season ends
        /// </summary>
        public void AutoSaveAfterSeason(int newSeason)
        {
            if (!autoSaveAfterSeason) return;
            if (currentWorldState != null)
            {
                currentWorldState.currentSeason = newSeason;
                currentWorldState.currentRound = 1;
                currentWorldState.currentPhase = CareerPhase.PreSeason;
                SaveCurrentState();
            }
        }
        /// <summary>
        /// Delete current career save
        /// </summary>
        public void DeleteCurrentCareer()
        {
            if (!string.IsNullOrEmpty(currentSlotName))
            {
                WorldSaveSystem.DeleteSave(currentSlotName);
                currentSlotName = null;
                currentWorldState = null;
            }
        }
        /// <summary>
        /// Get list of available career saves
        /// </summary>
        public System.Collections.Generic.List<string> GetAvailableCareers()
        {
            return WorldSaveSystem.GetAvailableSaves();
        }
    }
}
