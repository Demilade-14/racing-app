using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RacingGame.Career;

namespace RacingGame.UI
{
    // ═══════════════════════════════════════════════════════════════════════
    //  SEASON END SCREEN
    //  Wires to DriverProfile, SeasonRecord, LegacyTier from DriverCareerData.cs
    //  Assign all SerializeField slots in the Unity Inspector.
    // ═══════════════════════════════════════════════════════════════════════
    public class SeasonEndScreen : MonoBehaviour
    {
        // ── Championship Banner ───────────────────────────────────────────
        [Header("Championship Banner")]
        [SerializeField] TextMeshProUGUI bannerTitleText;
        [SerializeField] TextMeshProUGUI bannerSubtitleText;
        [SerializeField] GameObject      trophyIcon;
        [SerializeField] GameObject      confettiVFX;      // optional particle system

        // ── Season Metrics ────────────────────────────────────────────────
        [Header("Season Metrics")]
        [SerializeField] TextMeshProUGUI pointsText;
        [SerializeField] TextMeshProUGUI winsText;
        [SerializeField] TextMeshProUGUI podiumsText;
        [SerializeField] TextMeshProUGUI polesText;
        [SerializeField] TextMeshProUGUI fastestLapsText;
        [SerializeField] TextMeshProUGUI dnfsText;

        // ── Stat Improvements ─────────────────────────────────────────────
        [Header("Stat Improvement Bars")]
        [SerializeField] Slider paceBar;
        [SerializeField] Slider awarenessBar;
        [SerializeField] Slider racecraftBar;
        [SerializeField] Slider experienceBar;
        [SerializeField] TextMeshProUGUI paceBeforeAfterText;
        [SerializeField] TextMeshProUGUI awarenessBeforeAfterText;
        [SerializeField] TextMeshProUGUI racecraftBeforeAfterText;
        [SerializeField] TextMeshProUGUI experienceBeforeAfterText;

        // ── Reputation ────────────────────────────────────────────────────
        [Header("Reputation")]
        [SerializeField] TextMeshProUGUI reputationText;
        [SerializeField] TextMeshProUGUI reputationTierText;
        [SerializeField] Slider          reputationBar;

        // ── Buttons ───────────────────────────────────────────────────────
        [Header("Buttons")]
        [SerializeField] Button          continueBtn;
        [SerializeField] TextMeshProUGUI continueBtnText;
        [SerializeField] Button          retireBtn;

        // ── Retire Dialog ─────────────────────────────────────────────────
        [Header("Retire Confirmation Dialog")]
        [SerializeField] GameObject      retireDialog;
        [SerializeField] TextMeshProUGUI retireDialogDriverName;
        [SerializeField] Button          cancelRetireBtn;
        [SerializeField] Button          confirmRetireBtn;

        // ── Legacy Screen ─────────────────────────────────────────────────
        [Header("Legacy Screen")]
        [SerializeField] GameObject      legacyScreen;
        [SerializeField] TextMeshProUGUI legacyDriverNameText;
        [SerializeField] TextMeshProUGUI legacyTierText;
        [SerializeField] TextMeshProUGUI legacyTierBadgeText;
        [SerializeField] Transform       legacyStatsContainer;
        [SerializeField] GameObject      legacyStatRowPrefab;  // prefab with 2 TMP labels
        [SerializeField] Transform       legacyAccoladeContainer;
        [SerializeField] GameObject      legacyAccoladePrefab;
        [SerializeField] Button          closeLegacyBtn;

        // ── Public events (wired by UIManager) ────────────────────────────
        public System.Action OnContinue;
        public System.Action OnRetire;

        // ── Private state ─────────────────────────────────────────────────
        DriverProfile _profile;
        int           _champPosition;

        // Legacy tier colors matching LegacyTier enum in DriverCareerData.cs
        static readonly Dictionary<LegacyTier, Color> TIER_COLORS = new()
        {
            { LegacyTier.Rookie,      new Color(0.40f, 0.40f, 0.40f) },
            { LegacyTier.SolidPro,    new Color(0.38f, 0.65f, 0.98f) },
            { LegacyTier.RaceWinner,  new Color(0.13f, 0.77f, 0.37f) },
            { LegacyTier.Champion,    new Color(0.98f, 0.75f, 0.14f) },
            { LegacyTier.Legend,      new Color(0.98f, 0.45f, 0.09f) },
            { LegacyTier.GOAT,        new Color(0.91f, 0.00f, 0.18f) },
        };

        // ═══════════════════════════════════════════════════════════════════
        //  UNITY LIFECYCLE
        // ═══════════════════════════════════════════════════════════════════
        void Start()
        {
            continueBtn?.onClick.AddListener(HandleContinue);
            retireBtn?.onClick.AddListener(ShowRetireDialog);
            cancelRetireBtn?.onClick.AddListener(HideRetireDialog);
            confirmRetireBtn?.onClick.AddListener(HandleConfirmRetire);
            closeLegacyBtn?.onClick.AddListener(HideLegacyScreen);

            HideRetireDialog();
            HideLegacyScreen();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  MAIN ENTRY POINT
        //  Call this from UIManager after EndSeason() runs on DriverCareerManager
        //  prevStats = stats snapshot taken BEFORE EndSeason (for before/after bars)
        // ═══════════════════════════════════════════════════════════════════
        public void ShowSeasonEnd(DriverProfile profile, int champPosition,
                                   DriverStatSnapshot prevStats = null)
        {
            _profile       = profile;
            _champPosition = champPosition;

            gameObject.SetActive(true);

            PopulateBanner();
            PopulateMetrics();
            PopulateStatBars(prevStats);
            PopulateReputation();
            PopulateContinueButton();

            // Championship VFX
            bool isChampion = champPosition == 1;
            trophyIcon?.SetActive(isChampion);
            if (isChampion && confettiVFX != null)
            {
                confettiVFX.SetActive(true);
                var ps = confettiVFX.GetComponent<ParticleSystem>();
                ps?.Play();
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  POPULATE METHODS
        // ═══════════════════════════════════════════════════════════════════
        void PopulateBanner()
        {
            bool isChampion = _champPosition == 1;

            if (bannerTitleText)
                bannerTitleText.text = isChampion ? "WORLD CHAMPION!" : "SEASON COMPLETE";

            if (bannerSubtitleText)
                bannerSubtitleText.text =
                    $"{_profile.driverName}  ·  Season {_profile.season - 1}  ·  P{_champPosition}";
        }

        void PopulateMetrics()
        {
            if (pointsText)      pointsText.text      = _profile.seasonPoints.ToString();
            if (winsText)        winsText.text         = _profile.seasonWins.ToString();
            if (podiumsText)     podiumsText.text      = _profile.seasonPodiums.ToString();
            if (polesText)       polesText.text        = _profile.seasonPoles.ToString();
            if (fastestLapsText) fastestLapsText.text  = _profile.seasonFastestLaps.ToString();
            if (dnfsText)        dnfsText.text         = _profile.seasonDNFs.ToString();
        }

        void PopulateStatBars(DriverStatSnapshot prev)
        {
            // If no snapshot provided, just show current values with no delta
            int prevPace       = prev?.pace       ?? _profile.pace;
            int prevAwareness  = prev?.awareness  ?? _profile.awareness;
            int prevRacecraft  = prev?.racecraft  ?? _profile.racecraft;
            int prevExperience = prev?.experience ?? _profile.experience;

            SetStatBar(paceBar,       paceBeforeAfterText,       "PACE",
                       prevPace,       _profile.pace);
            SetStatBar(awarenessBar,  awarenessBeforeAfterText,  "AWARENESS",
                       prevAwareness,  _profile.awareness);
            SetStatBar(racecraftBar,  racecraftBeforeAfterText,  "RACECRAFT",
                       prevRacecraft,  _profile.racecraft);
            SetStatBar(experienceBar, experienceBeforeAfterText, "EXPERIENCE",
                       prevExperience, _profile.experience);
        }

        void SetStatBar(Slider slider, TextMeshProUGUI label, string statName,
                        int before, int after)
        {
            if (slider != null)
            {
                slider.minValue = 0;
                slider.maxValue = 99;
                slider.value    = after;
            }

            if (label != null)
            {
                int delta = after - before;
                string deltaStr = delta > 0 ? $"<color=#22c55e>+{delta}</color>"
                                : delta < 0 ? $"<color=#E8002D>{delta}</color>"
                                : "<color=#555555>—</color>";
                label.text = $"{statName}  {after}  {deltaStr}";
            }
        }

        void PopulateReputation()
        {
            if (reputationText)
                reputationText.text = $"{_profile.reputation:F0} / 100";

            if (reputationTierText)
            {
                reputationTierText.text  = (_profile.reputationTier ?? "Rookie").ToUpper();
                reputationTierText.color = new Color(0.91f, 0f, 0.18f); // red accent
            }

            if (reputationBar)
            {
                reputationBar.minValue = 0;
                reputationBar.maxValue = 100;
                reputationBar.value    = _profile.reputation;
            }
        }

        void PopulateContinueButton()
        {
            if (continueBtnText)
                continueBtnText.text = $"CONTINUE TO SEASON {_profile.season} →";
        }

        // ═══════════════════════════════════════════════════════════════════
        //  RETIRE DIALOG
        // ═══════════════════════════════════════════════════════════════════
        void ShowRetireDialog()
        {
            if (retireDialog) retireDialog.SetActive(true);
            if (retireDialogDriverName)
                retireDialogDriverName.text = _profile?.driverName ?? "Driver";
        }

        void HideRetireDialog()
        {
            if (retireDialog) retireDialog.SetActive(false);
        }

        void HandleConfirmRetire()
        {
            HideRetireDialog();

            // Calculate final legacy tier before showing legacy screen
            _profile?.UpdateLegacyTier();

            ShowLegacyScreen();
            OnRetire?.Invoke();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  LEGACY SCREEN
        // ═══════════════════════════════════════════════════════════════════
        void ShowLegacyScreen()
        {
            if (legacyScreen == null || _profile == null) return;
            legacyScreen.SetActive(true);

            // Driver name
            if (legacyDriverNameText)
                legacyDriverNameText.text = _profile.driverName;

            // Legacy tier label + color
            LegacyTier tier = _profile.legacyTier;
            Color tierColor = TIER_COLORS.TryGetValue(tier, out var c) ? c : Color.white;

            if (legacyTierText)
            {
                legacyTierText.text  = tier.ToString().ToUpper();
                legacyTierText.color = tierColor;
            }

            if (legacyTierBadgeText)
            {
                legacyTierBadgeText.text  = tier.ToString().ToUpper();
                legacyTierBadgeText.color = tierColor;
            }

            // Career stats list
            BuildLegacyStats();

            // Accolades wall
            BuildAccoladeWall();
        }

        void BuildLegacyStats()
        {
            if (legacyStatsContainer == null || legacyStatRowPrefab == null) return;

            // Clear old rows
            foreach (Transform child in legacyStatsContainer)
                Destroy(child.gameObject);

            var stats = new List<(string label, string value)>
            {
                ("SEASONS",        (_profile.season - 1).ToString()),
                ("RACE WINS",      _profile.careerWins.ToString()),
                ("PODIUMS",        _profile.careerPodiums.ToString()),
                ("CHAMPIONSHIPS",  _profile.careerChampionships.ToString()),
                ("POLE POSITIONS", _profile.careerPoles.ToString()),
                ("FASTEST LAPS",   _profile.careerFastestLaps.ToString()),
                ("CAREER POINTS",  _profile.careerPoints.ToString()),
                ("CAREER RACES",   _profile.careerRaces.ToString()),
            };

            foreach (var (label, value) in stats)
            {
                var row   = Instantiate(legacyStatRowPrefab, legacyStatsContainer);
                var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 2)
                {
                    texts[0].text = label;
                    texts[1].text = value;
                }
            }
        }

        void BuildAccoladeWall()
        {
            if (legacyAccoladeContainer == null || legacyAccoladePrefab == null
                || _profile.accolades == null) return;

            foreach (Transform child in legacyAccoladeContainer)
                Destroy(child.gameObject);

            foreach (var accolade in _profile.accolades)
            {
                var badge  = Instantiate(legacyAccoladePrefab, legacyAccoladeContainer);
                var texts  = badge.GetComponentsInChildren<TextMeshProUGUI>();
                var images = badge.GetComponentsInChildren<Image>();

                if (texts.Length >= 2)
                {
                    texts[0].text = accolade.title;
                    texts[1].text = accolade.description;
                }

                // Tint badge background by rarity
                if (images.Length >= 1)
                {
                    images[0].color = accolade.rarity switch
                    {
                        AccoladeRarity.Bronze    => new Color(0.80f, 0.50f, 0.20f, 0.25f),
                        AccoladeRarity.Silver    => new Color(0.75f, 0.75f, 0.75f, 0.25f),
                        AccoladeRarity.Gold      => new Color(1.00f, 0.84f, 0.00f, 0.25f),
                        AccoladeRarity.Legendary => new Color(0.91f, 0.00f, 0.18f, 0.30f),
                        _ => new Color(1f, 1f, 1f, 0.1f)
                    };
                }
            }
        }

        void HideLegacyScreen()
        {
            if (legacyScreen) legacyScreen.SetActive(false);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  BUTTON HANDLERS
        // ═══════════════════════════════════════════════════════════════════
        void HandleContinue()
        {
            gameObject.SetActive(false);
            OnContinue?.Invoke();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER STAT SNAPSHOT  —  taken before EndSeason() to show before/after
    //  Call DriverStatSnapshot.FromProfile(profile) BEFORE calling EndSeason()
    // ═══════════════════════════════════════════════════════════════════════
    [System.Serializable]
    public class DriverStatSnapshot
    {
        public int pace;
        public int awareness;
        public int racecraft;
        public int experience;

        public static DriverStatSnapshot FromProfile(DriverProfile p) => new()
        {
            pace       = p.pace,
            awareness  = p.awareness,
            racecraft  = p.racecraft,
            experience = p.experience,
        };
    }
}