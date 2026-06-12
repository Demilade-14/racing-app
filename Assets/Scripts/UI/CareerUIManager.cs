using UnityEngine;
using RacingGame.Career;
namespace RacingGame.UI
{
    /// <summary>
    /// Central UI Manager for Career Mode screens.
    /// Coordinates transitions between:
    /// - Career Mode Selector
    /// - Driver Creation
    /// - Career Hub
    /// - Race Weekend
    /// - Post Race
    /// - Season End
    /// </summary>
    public class CareerUIManager : MonoBehaviour
    {
        public static CareerUIManager Instance { get; private set; }
        [Header("Screen Panels (assign in inspector)")]
        [SerializeField] private GameObject careerModeSelectorPanel;
        [SerializeField] private GameObject driverCreationPanel;
        [SerializeField] private GameObject careerHubPanel;
        [SerializeField] private GameObject raceWeekendPanel;
        [SerializeField] private GameObject postRacePanel;
        [SerializeField] private GameObject seasonEndPanel;
        [Header("References")]
        [SerializeField] private DriverCareerManager careerManager;
        private GameObject currentScreen;
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        void Start()
        {
            // Wire up screen events
            WireUpEvents();
            // Show initial screen
            ShowScreen(careerModeSelectorPanel);
        }
        void WireUpEvents()
        {
            // Career Mode Selector
            var selector = careerModeSelectorPanel?.GetComponent<CareerModeSelectorScreen>();
            if (selector != null)
            {
                selector.OnModeSelected += HandleModeSelected;
            }
            // Driver Creation
            var creation = driverCreationPanel?.GetComponent<DriverCreationScreen>();
            if (creation != null)
            {
                creation.OnCreationComplete += HandleDriverCreated;
                creation.OnBack += () => ShowScreen(careerModeSelectorPanel);
            }
            // Career Hub
            var hub = careerHubPanel?.GetComponent<CareerHubScreen>();
            if (hub != null)
            {
                hub.OnActionSelected += HandleHubAction;
            }
            // Race Weekend
            var weekend = raceWeekendPanel?.GetComponent<RaceWeekendScreen>();
            if (weekend != null)
            {
                weekend.OnBack += () => ShowScreen(careerHubPanel);
                weekend.OnSessionComplete += HandleSessionComplete;
            }
            // Post Race
            var postRace = postRacePanel?.GetComponent<PostRaceScreen>();
            if (postRace != null)
            {
                postRace.OnNext += HandleNextEvent;
                postRace.OnReturnToHub += () => ShowScreen(careerHubPanel);
            }
            // Season End
            var seasonEnd = seasonEndPanel?.GetComponent<SeasonEndScreen>();
            if (seasonEnd != null)
            {
                seasonEnd.OnContinue += HandleNextSeason;
                seasonEnd.OnRetire += HandleDriverRetired;
            }
        }
        public void ShowScreen(GameObject screen)
        {
            if (currentScreen != null)
                currentScreen.SetActive(false);
            currentScreen = screen;
            if (currentScreen != null)
                currentScreen.SetActive(true);
        }
        // ── Event Handlers ──────────────────────────────────────────────────
        void HandleModeSelected(CareerModeType mode, string action)
        {
            if (action == "new")
            {
                ShowScreen(driverCreationPanel);
            }
            else // load
            {
                Debug.Log("[UIManager] Load career - TODO: Show slot selector");
            }
        }
        void HandleDriverCreated(DriverProfile profile)
        {
            careerManager?.StartNewCareer(profile);
            ShowScreen(careerHubPanel);
        }
        void HandleHubAction(string action)
        {
            switch (action)
            {
                case "race":
                    var weekend = careerManager?.GetCurrentWeekend();
                    if (weekend != null)
                    {
                        var weekendScreen = raceWeekendPanel?.GetComponent<RaceWeekendScreen>();
                        weekendScreen?.SetData(weekend, new Car2026Data());
                        ShowScreen(raceWeekendPanel);
                    }
                    break;
                case "training":
                    Debug.Log("[UIManager] Training - TODO");
                    break;
                case "media":
                    Debug.Log("[UIManager] Media - TODO");
                    break;
                case "contract":
                    Debug.Log("[UIManager] Contracts - TODO");
                    break;
                case "transfer":
                    Debug.Log("[UIManager] Transfer market - TODO");
                    break;
            }
        }
        void HandleSessionComplete(string session, RaceResult result)
        {
            if (session == "race" && result != null)
            {
                var postRaceScreen = postRacePanel?.GetComponent<PostRaceScreen>();
                var weekend = careerManager?.GetCurrentWeekend();
                var standings = careerManager?.GetChampionshipStandings();
                postRaceScreen?.ShowResults(result, weekend, standings, careerManager?.PlayerProfile);
                ShowScreen(postRacePanel);
            }
            else
            {
                // Refresh weekend screen for FP1/FP2/FP3/Quali
                var weekendScreen = raceWeekendPanel?.GetComponent<RaceWeekendScreen>();
                weekendScreen?.RefreshUI();
            }
        }
        void HandleNextEvent()
        {
            var career = careerManager?.CurrentSave;
            if (career != null)
            {
                if (career.currentRound >= 23) // End of season
                {
                    var seasonEndScreen = seasonEndPanel?.GetComponent<SeasonEndScreen>();
                    var prevStats = new SeasonRecord(); // TODO: Get actual prev stats
                    seasonEndScreen?.ShowSeasonEnd(careerManager.PlayerProfile, 5, prevStats);
                    ShowScreen(seasonEndPanel);
                }
                else
                {
                    careerManager?.AdvanceToNextRound();
                    ShowScreen(careerHubPanel);
                }
            }
        }
        void HandleNextSeason()
        {
            careerManager?.EndSeason();
            ShowScreen(careerHubPanel);
        }
        void HandleDriverRetired()
        {
            Debug.Log("[UIManager] Driver retired - showing legacy");
            // Legacy screen is shown by SeasonEndScreen
        }
    }
}
