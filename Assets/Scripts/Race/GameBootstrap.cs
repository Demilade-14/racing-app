using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RacingGame.Data;
using RacingGame.Physics;
using RacingGame.AI;
using RacingGame.Race;
using RacingGame.UI;
using RacingGame.Input;
using RacingGame.Audio;
using RacingGame.Save;
using RacingGame.VFX;
using RacingGame.Camera;
using RacingGame.Content;
using RacingGame.Manager;
using RacingGame.Career;

namespace RacingGame
{
    // ═══════════════════════════════════════════════════════════════════════
    //  GAME BOOTSTRAP  – persistent singleton, first thing loaded
    // ═══════════════════════════════════════════════════════════════════════
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        [Header("Prefabs")]
        public GameObject saveManagerPrefab;
        public GameObject audioManagerPrefab;

        [Header("Game Config")]
        public int targetFrameRate = 60;

        public GameMode CurrentMode { get; private set; } = GameMode.Career;

        // Shared game state
        public CircuitData       SelectedCircuit   { get; private set; }
        public WeatherData       SessionWeather    { get; private set; } = new();
        public List<TeamData>    AllTeams          { get; private set; }
        public List<DriverStats> AllDrivers        { get; private set; }
        public List<CircuitData> AllCircuits       { get; private set; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Content catalogue
            AllTeams    = GameContent.Teams();
            AllDrivers  = GameContent.Drivers();
            AllCircuits = GameContent.Circuits();

            // Core singletons
            EnsureSingleton(saveManagerPrefab, "SaveManager");
            EnsureSingleton(audioManagerPrefab, "AudioManager");

            // Performance
            Application.targetFrameRate = targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.vSyncCount = 0;

            Debug.Log("[Bootstrap] Game initialised.");
        }

        void EnsureSingleton(GameObject prefab, string tag)
        {
            if (prefab == null || GameObject.FindWithTag(tag) != null) return;
            var go    = Instantiate(prefab);
            go.tag    = tag;
            go.name   = tag;
            DontDestroyOnLoad(go);
        }

        // ── Scene navigation ──────────────────────────────────────────────
        public void LoadMainMenu()  => LoadScene("MainMenu");
        public void LoadCareer()    => LoadScene("CareerHub");
        public void LoadRace(CircuitData circuit, GameMode mode)
        {
            SelectedCircuit = circuit;
            CurrentMode     = mode;
            LoadScene("RaceScene");
        }
        public void LoadGarage()   => LoadScene("Garage");

        public void LoadScene(string name) =>
            StartCoroutine(LoadSceneAsync(name));

        IEnumerator LoadSceneAsync(string sceneName)
        {
            var op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f) yield return null;

            // Small settle frame
            yield return new WaitForSeconds(0.1f);
            op.allowSceneActivation = true;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  RACE SCENE CONTROLLER  – orchestrates a complete race weekend
    // ═══════════════════════════════════════════════════════════════════════
    public class RaceSceneController : MonoBehaviour
    {
        [Header("Player setup")]
        public PhysicsIntegrator    playerPhysics;
        public PlayerInputHandler   playerInput;
        public HudBuilder           hud;
        public RacingHUD            racingHUD;
        public RacingCameraController camera;
        public TelemetryRecorder    telemetry;
        public VFXManager           vfx;

        [Header("Race systems")]
        public RaceDirector         director;
        public PitStopController    pitController;

        [Header("AI prefab")]
        public GameObject           aiDriverPrefab;

        [Header("Grid spawn points")]
        public Transform[]          gridSlots;

        // Runtime
        List<AIDriver>   _aiDrivers  = new();
        DriverEntry      _playerEntry;
        string           _localId    = "player_0";
        CircuitData      _circuit;
        bool             _raceActive;

        void Start()
        {
            _circuit = GameBootstrap.Instance?.SelectedCircuit
                    ?? GameBootstrap.Instance?.AllCircuits[0];

            if (_circuit == null) { Debug.LogError("No circuit selected!"); return; }

            director.circuit   = _circuit;
            director.totalLaps = _circuit.totalLaps;

            SetupPlayerEntry();
            RegisterPlayerInChampionship();
            SpawnAICars();
            SetupDirectorEvents();
            SetupWeather();
            SetupCamera();

            // Brief countdown then start
            StartCoroutine(RaceCountdown());
        }

        void SetupPlayerEntry()
        {
            var saveCareer = SaveManager.Instance?.Current?.career;
            string driverName = "Player";
            string teamName   = "Apex Racing";

            if (saveCareer != null)
            {
                if (saveCareer.careerType == CareerType.Driver)
                {
                    driverName = saveCareer.driver.driverName;
                    teamName   = saveCareer.activeContract?.teamName ?? teamName;
                }
                else
                {
                    driverName = saveCareer.managerCareer.driver1?.entry.driverName
                              ?? saveCareer.managerCareer.driver2?.entry.driverName
                              ?? saveCareer.managerCareer.teamName
                              ?? driverName;
                    teamName   = saveCareer.managerCareer.teamName ?? teamName;
                }
            }

            _playerEntry = new DriverEntry
            {
                driverId   = _localId,
                driverName = driverName,
                teamName   = teamName,
                isPlayer   = true,
                position   = 1
            };

            var entries = new List<DriverEntry> { _playerEntry };
            director.InitEntries(entries);

            // Link HUD
            if (hud != null) hud.localDriverId = _localId;
        }

        void RegisterPlayerInChampionship()
        {
            var championship = FindObjectOfType<ChampionshipManager>();
            var career = SaveManager.Instance?.Current?.career;
            if (championship == null || career == null) return;

            string driverName = career.careerType == CareerType.Driver
                ? career.driver.driverName
                : career.managerCareer.driver1?.entry.driverName
                  ?? career.managerCareer.driver2?.entry.driverName
                  ?? career.managerCareer.teamName;

            string teamName = career.careerType == CareerType.Driver
                ? career.activeContract?.teamName
                : career.managerCareer.teamName;

            championship.RegisterPlayer(driverName ?? "Player", teamName ?? "Apex Racing", 1);
        }

        void SpawnAICars()
        {
            var drivers = GameBootstrap.Instance?.AllDrivers;
            if (drivers == null || aiDriverPrefab == null) return;

            int aiCount = Mathf.Min(19, gridSlots != null ? gridSlots.Length - 1 : 19);

            for (int i = 0; i < aiCount && i < drivers.Count; i++)
            {
                var pos   = gridSlots != null && i + 1 < gridSlots.Length
                          ? gridSlots[i + 1].position
                          : new Vector3((i + 1) * 8f, 0f, 0f);

                var rot   = gridSlots != null && i + 1 < gridSlots.Length
                          ? gridSlots[i + 1].rotation
                          : Quaternion.identity;

                var go    = Instantiate(aiDriverPrefab, pos, rot);
                var ai    = go.GetComponent<AIDriver>();
                if (ai == null) continue;

                ai.stats        = drivers[i];
                ai.racePosition = i + 2;
                ai.totalLaps    = _circuit.totalLaps;
                ai.SetWeather(director.weather);
                _aiDrivers.Add(ai);

                var entry = new DriverEntry
                {
                    driverId   = $"ai_{i}",
                    driverName = drivers[i].driverName,
                    teamName   = drivers[i].teamName,
                    isPlayer   = false,
                    position   = i + 2
                };
                director.Entries.Add(entry);
            }
        }

        void SetupDirectorEvents()
        {
            director.OnLapCompleted  += OnLapCompleted;
            director.OnRetirement    += OnRetirement;
            director.OnFlagChange    += OnFlagChange;
            director.OnRaceFinished  += OnRaceFinished;
        }

        void SetupWeather()
        {
            var w         = director.weather;
            w.condition   = _circuit.defaultWeather;
            w.ambientTemp = 24f;
            w.trackTemp   = 36f;

            if (vfx != null) vfx.SetRainIntensity(w.rainIntensity);
            AudioManager.Instance?.SetRainIntensity(w.rainIntensity);
        }

        void SetupCamera()
        {
            if (camera == null) return;
            camera.target         = playerPhysics.transform;
            camera.targetPhysics  = playerPhysics;
        }

        IEnumerator RaceCountdown()
        {
            yield return new WaitForSeconds(3f);
            director.StartRace();
            _raceActive = true;
            telemetry?.ClearSession();
        }

        void Update()
        {
            if (!_raceActive) return;

            // Update player entry distance
            _playerEntry.totalRaceTime  = director.RaceTime;
            _playerEntry.currentLap     = Mathf.Max(1,
                Mathf.FloorToInt(playerPhysics.State.speedMs * director.RaceTime
                                 / (_circuit.trackLengthKm * 1000f)) + 1);

            // Sync weather to AI
            foreach (var ai in _aiDrivers)
                ai.SetWeather(director.weather);

            // VFX weather sync
            vfx?.SetRainIntensity(director.weather.rainIntensity);
            AudioManager.Instance?.SetRainIntensity(director.weather.rainIntensity);

            // Camera cycle input
            if (UnityEngine.Input.GetKeyDown(KeyCode.C))
                camera?.CycleCamera();
        }

        void OnLapCompleted(DriverEntry entry)
        {
            if (entry.isPlayer && entry.currentLap > _circuit.totalLaps)
                director.RegisterPlayerFinish(entry);
        }

        void OnRetirement(DriverEntry entry)
        {
            if (!entry.isPlayer) return;
            _raceActive = false;
            Debug.Log($"[Race] Player retired.");
        }

        void OnFlagChange(FlagStatus flag)
        {
            if (flag == FlagStatus.SafetyCar)
            {
                AudioManager.Instance?.PlaySafetyCar();
                foreach (var ai in _aiDrivers)
                    ai.SetState(AIState.SafetyCar);
            }
            else if (flag == FlagStatus.Green)
            {
                foreach (var ai in _aiDrivers)
                    ai.SetState(AIState.FollowLine);
            }
        }

        void OnRaceFinished()
        {
            _raceActive = false;
            var results = RaceResultBuilder.Build(director.Entries);
            var championship = FindObjectOfType<ChampionshipManager>();

            championship?.ProcessRound(results);

            // Apply to career
            var career = SaveManager.Instance?.Current?.career;
            var myResult = results.FirstOrDefault(r => r.playerId == _localId);
            if (career != null && myResult != null)
            {
                if (career.careerType == CareerType.Driver)
                {
                    var weekend = new RaceWeekend
                    { round = career.calendar?.Count + 1 ?? 1, circuitName = _circuit.circuitName };
                    FindObjectOfType<RacingGame.Career.CareerManager>()
                        ?.ProcessRaceResult(myResult, weekend);
                }
                else if (career.careerType == CareerType.Manager)
                {
                    FindObjectOfType<MyTeamManager>()?.ProcessRaceResult(1, myResult);
                }

                SaveManager.Instance?.SaveCareer();
            }

            StartCoroutine(ShowResultsDelay(results));
        }

        IEnumerator ShowResultsDelay(List<RaceResult> results)
        {
            yield return new WaitForSeconds(2f);
            FindObjectOfType<RaceResultScreen>()?.ShowResults(results, _localId);
        }
    }
}
