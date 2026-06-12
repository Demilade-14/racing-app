using UnityEngine;
using RacingGame.Career;
using RacingGame.Data;

namespace RacingGame.UI
{
    // ═══════════════════════════════════════════════════════════════════════
    //  UI MANAGER
    //  Central screen controller for Driver Career + Manager Career.
    //  Attach to a persistent GameObject in your career scene.
    //  Assign all SerializeField slots in the Unity Inspector.
    // ═══════════════════════════════════════════════════════════════════════
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        // ── Screen GameObjects ────────────────────────────────────────────
        [Header("Screens")]
        [SerializeField] GameObject careerModeSelectorScreen;
        [SerializeField] GameObject driverCreationScreen;
        [SerializeField] GameObject careerHubScreen;
        [SerializeField] GameObject raceWeekendScreen;
        [SerializeField] GameObject postRaceScreen;
        [SerializeField] GameObject seasonEndScreen;
        [SerializeField] GameObject contractOfficeScreen;
        [SerializeField] GameObject transferMarketScreen;
        [SerializeField] GameObject trainingScreen;
        [SerializeField] GameObject mediaEventsScreen;

        // ── Screen Components (cached) ────────────────────────────────────
        CareerModeSelectorScreen _selectorComp;
        DriverCreationScreen     _creationComp;
        CareerHubScreen          _hubComp;
        RaceWeekendScreen        _weekendComp;
        PostRaceScreen           _postRaceComp;
        SeasonEndScreen          _seasonEndComp;

        // ── Career Manager reference ──────────────────────────────────────
        [Header("Systems")]
        [SerializeField] DriverCareerManager careerManager;

        // ── Snapshot for before/after stat comparison ─────────────────────
        DriverStatSnapshot _preSeasonSnapshot;

        // ═══════════════════════════════════════════════════════════════════
        //  UNITY LIFECYCLE
        // ═══════════════════════════════════════════════════════════════════
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            HideAllScreens();
        }

        void Start()
        {
            // Cache and wire all screen components
            WireCareerModeSelector();
            WireDriverCreation();
            WireCareerHub();
            WireRaceWeekend();
            WirePostRace();
            WireSeasonEnd();

            // Wire DriverCareerManager events
            if (careerManager != null)
            {
                careerManager.OnRaceCompleted      += OnRaceCompleted;
                careerManager.OnSeasonComplete     += OnSeasonComplete;
                careerManager.OnContractExpiring   += OnContractExpiring;
                careerManager.OnNewspaper          += OnNewspaper;
                careerManager.OnAccoladeUnlocked   += OnAccoladeUnlocked;
                careerManager.OnEventCardReady     += OnEventCardReady;
                careerManager.OnContractOffersReady += OnContractOffersReady;
            }

            // Start on career mode selector
            ShowScreen(careerModeSelectorScreen);
        }

        void OnDestroy()
        {
            // Unsubscribe to avoid memory leaks
            if (careerManager != null)
            {
                careerManager.OnRaceCompleted      -= OnRaceCompleted;
                careerManager.OnSeasonComplete     -= OnSeasonComplete;
                careerManager.OnContractExpiring   -= OnContractExpiring;
                careerManager.OnNewspaper          -= OnNewspaper;
                careerManager.OnAccoladeUnlocked   -= OnAccoladeUnlocked;
                careerManager.OnEventCardReady     -= OnEventCardReady;
                careerManager.OnContractOffersReady -= OnContractOffersReady;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  SCREEN WIRING
        // ═══════════════════════════════════════════════════════════════════
        void WireCareerModeSelector()
        {
            if (careerModeSelectorScreen == null) return;
            _selectorComp = careerModeSelectorScreen.GetComponent<CareerModeSelectorScreen>();
            if (_selectorComp == null) return;

            _selectorComp.OnDriverCareerNew     += () => ShowScreen(driverCreationScreen);
            _selectorComp.OnDriverCareerLoad    += OpenSaveSlotSelector;
            _selectorComp.OnManagerCareerNew    += StartManagerCareer;
            _selectorComp.OnManagerCareerLoad   += OpenSaveSlotSelector;
        }

        void WireDriverCreation()
        {
            if (driverCreationScreen == null) return;
            _creationComp = driverCreationScreen.GetComponent<DriverCreationScreen>();
            if (_creationComp == null) return;

            _creationComp.OnBack += () => ShowScreen(careerModeSelectorScreen);
            _creationComp.OnCreationComplete += HandleDriverCreated;
        }

        void WireCareerHub()
        {
            if (careerHubScreen == null) return;
            _hubComp = careerHubScreen.GetComponent<CareerHubScreen>();
            if (_hubComp == null) return;

            _hubComp.OnActionSelected += HandleHubAction;
        }

        void WireRaceWeekend()
        {
            if (raceWeekendScreen == null) return;
            _weekendComp = raceWeekendScreen.GetComponent<RaceWeekendScreen>();
            if (_weekendComp == null) return;

            _weekendComp.OnBack           += () => ShowScreen(careerHubScreen);
            _weekendComp.OnSimulate       += HandleSimulateSession;
            _weekendComp.OnStartSession   += HandleStartSession;
            _weekendComp.OnDeployMode     += HandleDeployModeChange;
            _weekendComp.OnCompleteProgram += HandlePracticeProgram;
        }

        void WirePostRace()
        {
            if (postRaceScreen == null) return;
            _postRaceComp = postRaceScreen.GetComponent<PostRaceScreen>();
            if (_postRaceComp == null) return;

            _postRaceComp.OnNext         += HandleNextEvent;
            _postRaceComp.OnReturnToHub  += () => ShowScreen(careerHubScreen);
        }

        void WireSeasonEnd()
        {
            if (seasonEndScreen == null) return;
            _seasonEndComp = seasonEndScreen.GetComponent<SeasonEndScreen>();
            if (_seasonEndComp == null) return;

            _seasonEndComp.OnContinue += HandleNextSeason;
            _seasonEndComp.OnRetire   += HandleDriverRetired;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  SCREEN NAVIGATION
        // ═══════════════════════════════════════════════════════════════════
        public void ShowScreen(GameObject screen)
        {
            HideAllScreens();
            if (screen != null)
            {
                screen.SetActive(true);
                Debug.Log($"[UIManager] Showing screen: {screen.name}");
            }
        }

        void HideAllScreens()
        {
            careerModeSelectorScreen?.SetActive(false);
            driverCreationScreen?.SetActive(false);
            careerHubScreen?.SetActive(false);
            raceWeekendScreen?.SetActive(false);
            postRaceScreen?.SetActive(false);
            seasonEndScreen?.SetActive(false);
            contractOfficeScreen?.SetActive(false);
            transferMarketScreen?.SetActive(false);
            trainingScreen?.SetActive(false);
            mediaEventsScreen?.SetActive(false);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  CAREER START HANDLERS
        // ═══════════════════════════════════════════════════════════════════
        void HandleDriverCreated(DriverCreationData data)
        {
            if (careerManager == null)
            {
                Debug.LogError("[UIManager] DriverCareerManager is not assigned!");
                return;
            }

            careerManager.StartNewCareer(
                driverName:  data.driverName,
                nationality: data.nationality,
                teamName:    data.teamName,
                startInF2:   data.startInF2,
                iconType:    data.iconType,
                iconBaseName: data.iconBaseName
            );

            // Apply icon stats if applicable
            if (data.iconType == DriverIconType.Icon && data.selectedIcon != null)
                careerManager.ApplyDriverIcon(data.selectedIcon);

            RefreshCareerHub();
            ShowScreen(careerHubScreen);

            Debug.Log($"[UIManager] Driver career started for {data.driverName}");
        }

        void StartManagerCareer()
        {
            // TODO: wire to ManagerSystem.cs
            Debug.Log("[UIManager] Manager career selected — wire to ManagerSystem");
        }

        void OpenSaveSlotSelector()
        {
            // TODO: open save slot screen
            Debug.Log("[UIManager] Load career — open save slot selector");
        }

        // ═══════════════════════════════════════════════════════════════════
        //  HUB ACTION HANDLER
        // ═══════════════════════════════════════════════════════════════════
        void HandleHubAction(string action)
        {
            switch (action)
            {
                case "race":
                    OpenRaceWeekend();
                    break;

                case "training":
                    ShowScreen(trainingScreen);
                    break;

                case "media":
                    ShowScreen(mediaEventsScreen);
                    break;

                case "contract":
                    ShowScreen(contractOfficeScreen);
                    break;

                case "transfer":
                    ShowScreen(transferMarketScreen);
                    break;

                default:
                    Debug.LogWarning($"[UIManager] Unknown hub action: {action}");
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  RACE WEEKEND
        // ═══════════════════════════════════════════════════════════════════
        void OpenRaceWeekend()
        {
            if (careerManager?.CurrentSave == null) return;

            var save = careerManager.CurrentSave;
            if (save.currentRound >= save.calendar.Count)
            {
                Debug.LogWarning("[UIManager] No more rounds in calendar.");
                return;
            }

            var weekend = save.calendar[save.currentRound];

            _weekendComp?.SetData(
                weekend,
                careerManager.ERSTracker,
                new Car2026Data()
            );

            ShowScreen(raceWeekendScreen);
        }

        void HandleSimulateSession(string session)
        {
            if (careerManager == null) return;

            var save    = careerManager.CurrentSave;
            var weekend = save?.calendar[save.currentRound];
            if (weekend == null) return;

            // Simulate session result
            switch (session)
            {
                case "fp1":        weekend.fp1Done   = true; break;
                case "fp2":        weekend.fp2Done   = true; break;
                case "fp3":        weekend.fp3Done   = true; break;
                case "qualifying":
                    weekend.qualiDone    = true;
                    weekend.gridPosition = Random.Range(3, 15);
                    break;
                case "race":
                    SimulateRaceResult(weekend);
                    return; // SimulateRaceResult handles screen transition
            }

            _weekendComp?.RefreshUI();
            Debug.Log($"[UIManager] Session simulated: {session}");
        }

        void HandleStartSession(string session)
        {
            // Route to actual session gameplay
            // For sessions other than race, simulate for now
            if (session != "race")
            {
                HandleSimulateSession(session);
                return;
            }

            // Load race scene / start race loop
            Debug.Log("[UIManager] Starting race session — load race scene here");
        }

        void SimulateRaceResult(DriverRaceWeekend weekend)
        {
            // Generate a plausible simulated result
            int pos = Random.Range(1, 12);
            int pts = pos switch { 1 => 25, 2 => 18, 3 => 15, 4 => 12,
                                   5 => 10, 6 => 8,  7 => 6,  8 => 4,
                                   9 => 2,  10 => 1, _ => 0 };

            var result = new RaceResult
            {
                finishPosition = pos,
                pointsEarned   = pts,
                hasFastestLap  = Random.value > 0.8f,
                retired        = false,
                driverName     = careerManager.PlayerProfile?.driverName ?? "Player",
                teamName       = careerManager.PlayerProfile?.currentTeam ?? "—"
            };

            weekend.result = result;
            careerManager.ProcessRaceWeekend(weekend, result);
            // OnRaceCompleted event fires → handled below
        }

        void HandleDeployModeChange(string mode)
        {
            if (careerManager?.PlayerProfile == null) return;
            careerManager.PlayerProfile.preferredDeployMode = mode switch
            {
                "Harvest"  => DeploymentMode.Harvest,
                "Attack"   => DeploymentMode.Attack,
                _          => DeploymentMode.Balanced,
            };
            Debug.Log($"[UIManager] Deploy mode changed to {mode}");
        }

        void HandlePracticeProgram(PracticeProgram program)
        {
            if (program == null) return;
            careerManager?.CompletePracticeProgram(program, Random.Range(60f, 95f));
            _weekendComp?.RefreshUI();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  CAREER MANAGER EVENTS
        // ═══════════════════════════════════════════════════════════════════
        void OnRaceCompleted(RaceResult result, int xpEarned)
        {
            // Advance the round counter
            careerManager?.AdvanceRound();

            // Show post-race screen
            var standings = careerManager?.Championship?.GetStandingsList();
            _postRaceComp?.ShowResults(result, standings, careerManager?.PlayerProfile);
            ShowScreen(postRaceScreen);

            Debug.Log($"[UIManager] Race complete — P{result.finishPosition} XP+{xpEarned}");
        }

        void OnSeasonComplete(int champPosition)
        {
            // Take stat snapshot AFTER EndSeason has already updated stats
            // (snapshot was taken by HandleNextEvent before EndSeason was called)
            _seasonEndComp?.ShowSeasonEnd(
                careerManager.PlayerProfile,
                champPosition,
                _preSeasonSnapshot
            );
            ShowScreen(seasonEndScreen);

            Debug.Log($"[UIManager] Season complete — P{champPosition}");
        }

        void OnContractExpiring()
        {
            // Show notification on hub (handled by ToastNotification in React UI)
            Debug.Log("[UIManager] Contract expiring soon — notify player");
        }

        void OnNewspaper(string headline)
        {
            Debug.Log($"[UIManager] 📰 {headline}");
            // Pass headline to any in-game news ticker / toast system here
        }

        void OnAccoladeUnlocked(Accolade accolade)
        {
            Debug.Log($"[UIManager] 🏅 Accolade unlocked: {accolade.title}");
            // Trigger accolade popup UI here
        }

        void OnEventCardReady(EventCard card)
        {
            Debug.Log($"[UIManager] 🃏 Event card ready: {card.headline}");
            // Open media events screen or show modal
            ShowScreen(mediaEventsScreen);
            var mediaComp = mediaEventsScreen?.GetComponent<MediaEventsScreen>();
            mediaComp?.ShowCard(card, HandleEventCardChoice);
        }

        void HandleEventCardChoice(EventCard card, int choiceIndex)
        {
            careerManager?.ResolveEventCard(card, choiceIndex);
            ShowScreen(careerHubScreen);
            RefreshCareerHub();
        }

        void OnContractOffersReady(System.Collections.Generic.List<ContractOffer> offers)
        {
            Debug.Log($"[UIManager] {offers.Count} contract offer(s) ready");
            // Auto-navigate to contract office if in transfer window
            if (careerManager?.CurrentSave?.phase == CareerPhase.TransferWindow)
                ShowScreen(contractOfficeScreen);

            var contractComp = contractOfficeScreen?.GetComponent<ContractOfficeScreen>();
            contractComp?.PopulateOffers(offers, HandleContractAccepted);
        }

        void HandleContractAccepted(ContractOffer offer)
        {
            careerManager?.SignContract(offer);
            RefreshCareerHub();
            ShowScreen(careerHubScreen);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  SEASON FLOW
        // ═══════════════════════════════════════════════════════════════════
        void HandleNextEvent()
        {
            if (careerManager?.CurrentSave == null) return;

            var save = careerManager.CurrentSave;
            bool isLastRound = save.currentRound >= save.calendar.Count;

            if (isLastRound)
            {
                // Snapshot stats before EndSeason modifies them
                _preSeasonSnapshot = DriverStatSnapshot.FromProfile(careerManager.PlayerProfile);

                // EndSeason fires OnSeasonComplete → OnSeasonComplete handler shows season end screen
                int finalPos = careerManager.Championship?.GetPlayerPosition() ?? 10;
                careerManager.EndSeason(finalPos);
            }
            else
            {
                RefreshCareerHub();
                ShowScreen(careerHubScreen);
            }
        }

        void HandleNextSeason()
        {
            _preSeasonSnapshot = null;
            RefreshCareerHub();
            ShowScreen(careerHubScreen);
            Debug.Log("[UIManager] New season started");
        }

        void HandleDriverRetired()
        {
            // Legacy screen is shown by SeasonEndScreen itself
            Debug.Log("[UIManager] Driver retired — legacy screen active");
        }

        // ═══════════════════════════════════════════════════════════════════
        //  HUB REFRESH
        // ═══════════════════════════════════════════════════════════════════
        void RefreshCareerHub()
        {
            if (_hubComp == null || careerManager == null) return;

            var profile  = careerManager.PlayerProfile;
            var save     = careerManager.CurrentSave;
            var standing = careerManager.Championship?.GetStandingsList();
            var nextCircuit = save?.currentRound < save?.calendar?.Count
                ? save.calendar[save.currentRound]
                : null;

            _hubComp.Refresh(
                profile:     profile,
                season:      profile?.season ?? 1,
                round:       save?.currentRound ?? 0,
                totalRounds: save?.calendar?.Count ?? 24,
                nextCircuit: nextCircuit,
                standings:   standing,
                objectives:  careerManager.Objectives?.Current
            );
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER CREATION DATA  —  payload passed from DriverCreationScreen
    //  to UIManager.HandleDriverCreated()
    // ═══════════════════════════════════════════════════════════════════════
    [System.Serializable]
    public class DriverCreationData
    {
        public string        driverName;
        public string        nationality;
        public string        teamName;
        public bool          startInF2    = true;
        public DriverIconType iconType    = DriverIconType.Custom;
        public string        iconBaseName;
        public DriverIcon    selectedIcon;
        public string        helmetColorHex = "#E8002D";
        public string        suitColorHex   = "#FFFFFF";
        public int           raceNumber;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  STUB INTERFACES
    //  These are the minimum method/event signatures that each screen
    //  component must implement. Fill in the full UI logic per screen.
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Attach to careerModeSelectorScreen GameObject</summary>
    public class CareerModeSelectorScreen : MonoBehaviour
    {
        public System.Action OnDriverCareerNew;
        public System.Action OnDriverCareerLoad;
        public System.Action OnManagerCareerNew;
        public System.Action OnManagerCareerLoad;
    }

    /// <summary>Attach to driverCreationScreen GameObject</summary>
    public class DriverCreationScreen : MonoBehaviour
    {
        public System.Action                   OnBack;
        public System.Action<DriverCreationData> OnCreationComplete;
    }

    /// <summary>Attach to careerHubScreen GameObject</summary>
    public class CareerHubScreen : MonoBehaviour
    {
        public System.Action<string> OnActionSelected;

        public virtual void Refresh(
            DriverProfile profile, int season, int round, int totalRounds,
            DriverRaceWeekend nextCircuit,
            System.Collections.Generic.List<ChampionshipEntry> standings,
            System.Collections.Generic.List<SeasonObjective> objectives) { }
    }

    /// <summary>Attach to raceWeekendScreen GameObject</summary>
    public class RaceWeekendScreen : MonoBehaviour
    {
        public System.Action              OnBack;
        public System.Action<string>      OnSimulate;
        public System.Action<string>      OnStartSession;
        public System.Action<string>      OnDeployMode;
        public System.Action<PracticeProgram> OnCompleteProgram;

        public virtual void SetData(DriverRaceWeekend weekend,
            ERSCareerTracker ersTracker, Car2026Data carData) { }
        public virtual void RefreshUI() { }
    }

    /// <summary>Attach to postRaceScreen GameObject</summary>
    public class PostRaceScreen : MonoBehaviour
    {
        public System.Action OnNext;
        public System.Action OnReturnToHub;

        public virtual void ShowResults(
            RaceResult result,
            System.Collections.Generic.List<ChampionshipEntry> standings,
            DriverProfile profile) { }
    }

    /// <summary>Attach to mediaEventsScreen GameObject</summary>
    public class MediaEventsScreen : MonoBehaviour
    {
        public virtual void ShowCard(EventCard card,
            System.Action<EventCard, int> onChoice) { }
    }

    /// <summary>Attach to contractOfficeScreen GameObject</summary>
    public class ContractOfficeScreen : MonoBehaviour
    {
        public virtual void PopulateOffers(
            System.Collections.Generic.List<ContractOffer> offers,
            System.Action<ContractOffer> onAccept) { }
    }
}