using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RacingGame.Career;

namespace RacingGame.UI
{
    // ═══════════════════════════════════════════════════════════════════════
    //  CAREER HUB SCREEN  —  2026 Extension
    //  Adds: ERS skill bar, tyre management bar, rival banner,
    //        next race circuit info + Madrid badge, training token counter,
    //        contract expiry warning banner, reputation tier color coding,
    //        season history scroll, accolades panel.
    //  All original fields and methods retained unchanged.
    // ═══════════════════════════════════════════════════════════════════════
    public class CareerHubScreen : MonoBehaviour
    {
        // ── Original fields ───────────────────────────────────────────────
        [Header("Top Bar - Driver Info")]
        [SerializeField] private TMP_Text driverNameText;
        [SerializeField] private TMP_Text driverNumberText;
        [SerializeField] private TMP_Text teamNameText;
        [SerializeField] private TMP_Text seasonRoundText;

        [Header("Stats Section")]
        [SerializeField] private Image    paceBar;
        [SerializeField] private Image    awarenessBar;
        [SerializeField] private Image    racecraftBar;
        [SerializeField] private Image    experienceBar;
        [SerializeField] private Image    reputationBar;
        [SerializeField] private TMP_Text reputationTierText;

        [Header("Action Buttons (Must be large for mobile)")]
        [SerializeField] private Button     raceWeekendBtn;
        [SerializeField] private Button     trainingBtn;
        [SerializeField] private Button     mediaBtn;
        [SerializeField] private Button     contractBtn;
        [SerializeField] private Button     transferBtn;
        [SerializeField] private GameObject raceReadyIndicator;

        [Header("Objectives Section")]
        [SerializeField] private Transform  objectivesList;
        [SerializeField] private GameObject objectiveRowPrefab;

        [Header("Standings Section")]
        [SerializeField] private Transform  standingsList;
        [SerializeField] private GameObject standingRowPrefab;

        // ── 2026 new fields ───────────────────────────────────────────────
        [Header("2026 — Extra Stat Bars")]
        [SerializeField] private Image    ersSkillBar;
        [SerializeField] private Image    tyreManagementBar;
        [SerializeField] private TMP_Text ersSkillValueText;
        [SerializeField] private TMP_Text tyreManagementValueText;

        [Header("2026 — Next Race Info Card")]
        [SerializeField] private TMP_Text   nextCircuitNameText;
        [SerializeField] private TMP_Text   nextCircuitRoundText;
        [SerializeField] private GameObject madridBadge;
        [SerializeField] private TMP_Text   nextCircuitStrategyHintText;

        [Header("2026 — Training Tokens")]
        [SerializeField] private TMP_Text   trainingTokensText;
        [SerializeField] private GameObject trainingLockedOverlay;

        [Header("2026 — Contract Expiry Warning")]
        [SerializeField] private GameObject contractExpiryBanner;
        [SerializeField] private TMP_Text   contractExpiryText;

        [Header("2026 — Rival Banner")]
        [SerializeField] private GameObject rivalBanner;
        [SerializeField] private TMP_Text   rivalNameText;
        [SerializeField] private TMP_Text   rivalStatusText;

        [Header("2026 — Season History")]
        [SerializeField] private Transform  seasonHistoryList;
        [SerializeField] private GameObject seasonHistoryRowPrefab;

        [Header("2026 — Accolades")]
        [SerializeField] private Transform  accoladesList;
        [SerializeField] private GameObject accoladeItemPrefab;

        // ── References ────────────────────────────────────────────────────
        private DriverCareerManager  careerManager;
        private CareerIntegrationLayer integration;

        public System.Action<string> OnActionSelected;

        // ── Colors ────────────────────────────────────────────────────────
        private static readonly Color COL_GREEN  = new Color(0.13f, 0.77f, 0.37f);
        private static readonly Color COL_GOLD   = new Color(0.98f, 0.75f, 0.14f);
        private static readonly Color COL_ORANGE = new Color(0.91f, 0f,   0.18f);
        private static readonly Color COL_BLUE   = new Color(0.38f, 0.65f, 0.98f);
        private static readonly Color COL_GREY   = new Color(0.4f,  0.4f,  0.4f);

        // ─────────────────────────────────────────────────────────────────
        void Start()
        {
            careerManager = DriverCareerManager.Instance;
            integration   = CareerIntegrationLayer.Instance;

            // Original button wiring
            raceWeekendBtn?.onClick.AddListener(() => OnActionSelected?.Invoke("race"));
            trainingBtn?.onClick.AddListener(()    => OnActionSelected?.Invoke("training"));
            mediaBtn?.onClick.AddListener(()       => OnActionSelected?.Invoke("media"));
            contractBtn?.onClick.AddListener(()    => OnActionSelected?.Invoke("contract"));
            transferBtn?.onClick.AddListener(()    => OnActionSelected?.Invoke("transfer"));

            if (careerManager != null)
            {
                careerManager.OnRaceCompleted    += RefreshUI;
                careerManager.OnSeasonComplete   += OnSeasonEnded;
                careerManager.OnContractExpiring += OnContractExpiring;   // 2026 new
                careerManager.OnRivalEvent       += OnRivalEvent;          // 2026 new
            }

            RefreshUI();
        }

        void OnDestroy()
        {
            if (careerManager != null)
            {
                careerManager.OnRaceCompleted    -= RefreshUI;
                careerManager.OnSeasonComplete   -= OnSeasonEnded;
                careerManager.OnContractExpiring -= OnContractExpiring;
                careerManager.OnRivalEvent       -= OnRivalEvent;
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  REFRESH  (original + 2026 extensions)
        // ═════════════════════════════════════════════════════════════════
        public void RefreshUI(RaceResult result = null, int xpGained = 0)
        {
            if (careerManager?.PlayerProfile == null) return;
            var profile = careerManager.PlayerProfile;

            // ── Original: top bar ─────────────────────────────────────────
            if (driverNameText)   driverNameText.text   = profile.driverName   ?? "Driver";
            if (driverNumberText) driverNumberText.text = profile.raceNumber > 0 ? $"#{profile.raceNumber}" : "";
            if (teamNameText)     teamNameText.text      = profile.currentTeam  ?? "Free Agent";
            if (seasonRoundText)  seasonRoundText.text   = $"S{profile.season} · R{integration?.GetCurrentRoundInfo() ?? "0/24"}";

            // ── Original: stats ───────────────────────────────────────────
            if (paceBar)       paceBar.fillAmount       = profile.pace       / 99f;
            if (awarenessBar)  awarenessBar.fillAmount   = profile.awareness  / 99f;
            if (racecraftBar)  racecraftBar.fillAmount   = profile.racecraft  / 99f;
            if (experienceBar) experienceBar.fillAmount  = profile.experience / 99f;
            if (reputationBar) reputationBar.fillAmount  = profile.reputationScore / 100f;

            if (reputationTierText)
            {
                reputationTierText.text  = profile.reputationTier?.ToUpper() ?? "ROOKIE";
                reputationTierText.color = GetReputationTierColor(profile.reputationTier);
            }

            if (raceReadyIndicator) raceReadyIndicator.SetActive(profile.season > 0);

            // ── 2026: ERS & Tyre bars ─────────────────────────────────────
            if (ersSkillBar)
                ersSkillBar.fillAmount = profile.ersManagementSkill / 100f;
            if (ersSkillValueText)
                ersSkillValueText.text = $"{profile.ersManagementSkill:F0}";

            if (tyreManagementBar)
                tyreManagementBar.fillAmount = profile.tyreManagementSkill / 100f;
            if (tyreManagementValueText)
                tyreManagementValueText.text = $"{profile.tyreManagementSkill:F0}";

            // ── 2026: Training tokens ─────────────────────────────────────
            int tokensLeft = careerManager.TrainingTokensRemainingThisWeek;
            if (trainingTokensText)
                trainingTokensText.text = $"TRAINING: {tokensLeft}/2 sessions left";
            if (trainingLockedOverlay)
                trainingLockedOverlay.SetActive(tokensLeft <= 0);

            // ── 2026: Next race info ──────────────────────────────────────
            RefreshNextRaceInfo();

            // ── 2026: Contract expiry ─────────────────────────────────────
            RefreshContractWarning(profile);

            // ── 2026: Season history ──────────────────────────────────────
            RefreshSeasonHistory(profile);

            // ── 2026: Accolades ───────────────────────────────────────────
            RefreshAccolades(profile);

            // Original sections
            RenderObjectives(profile);
            RenderStandings();
        }

        // ── 2026: Next race info ──────────────────────────────────────────
        void RefreshNextRaceInfo()
        {
            var weekend = careerManager?.GetCurrentWeekend();
            if (weekend == null) return;

            if (nextCircuitNameText)  nextCircuitNameText.text  = weekend.circuitName ?? "TBC";
            if (nextCircuitRoundText) nextCircuitRoundText.text = $"Round {weekend.round}";

            bool isMadrid = weekend.circuitName != null &&
                            weekend.circuitName.ToUpper().Contains("MAD");
            if (madridBadge) madridBadge.SetActive(isMadrid);

            if (nextCircuitStrategyHintText)
                nextCircuitStrategyHintText.text = isMadrid
                    ? "High tyre deg · 2-stop recommended · 3 DRS zones"
                    : "Check strategy before heading to race weekend";
        }

        // ── 2026: Contract warning ────────────────────────────────────────
        void RefreshContractWarning(DriverProfile profile)
        {
            bool expiring = profile.activeContract != null &&
                            profile.activeContract.seasonsRemaining == 1;

            if (contractExpiryBanner) contractExpiryBanner.SetActive(expiring);
            if (expiring && contractExpiryText)
                contractExpiryText.text =
                    $"⚠ Contract with {profile.currentTeam} expires after this season!";
        }

        // ── 2026: Rival banner (called from event) ────────────────────────
        void OnRivalEvent(RivalEntry rival)
        {
            if (rival == null) return;
            if (rivalBanner)    rivalBanner.SetActive(true);
            if (rivalNameText)  rivalNameText.text   = rival.rivalName?.ToUpper() ?? "RIVAL";
            if (rivalStatusText)
                rivalStatusText.text = rival.intensity switch
                {
                    RivalIntensity.Nemesis   => "NEMESIS — They're out to get you",
                    RivalIntensity.Primary   => "MAIN RIVAL — Intense battle this season",
                    _                        => "RIVAL — Keeping an eye on you"
                };
        }

        void OnContractExpiring()
        {
            if (contractExpiryBanner) contractExpiryBanner.SetActive(true);
            if (contractExpiryText)
                contractExpiryText.text = "⚠ Your contract expires at season end! Visit the transfer market.";
        }

        // ── 2026: Season history ──────────────────────────────────────────
        void RefreshSeasonHistory(DriverProfile profile)
        {
            if (seasonHistoryList == null || seasonHistoryRowPrefab == null) return;
            foreach (Transform child in seasonHistoryList) Destroy(child.gameObject);

            if (profile.seasonHistory == null || profile.seasonHistory.Count == 0) return;

            // Show last 3 seasons
            int start = Mathf.Max(0, profile.seasonHistory.Count - 3);
            for (int i = start; i < profile.seasonHistory.Count; i++)
            {
                var record = profile.seasonHistory[i];
                var row    = Instantiate(seasonHistoryRowPrefab, seasonHistoryList);
                var texts  = row.GetComponentsInChildren<TMP_Text>();

                if (texts.Length >= 4)
                {
                    texts[0].text  = $"S{record.season}";
                    texts[1].text  = record.teamName ?? "Unknown";
                    texts[2].text  = $"P{record.championshipPos}";
                    texts[3].text  = $"{record.wins}W  {record.podiums}P";

                    // Gold for champion season
                    if (record.isChampionshipYear)
                        texts[2].color = COL_GOLD;
                }
            }
        }

        // ── 2026: Accolades ───────────────────────────────────────────────
        void RefreshAccolades(DriverProfile profile)
        {
            if (accoladesList == null || accoladeItemPrefab == null) return;
            foreach (Transform child in accoladesList) Destroy(child.gameObject);

            if (profile.accolades == null || profile.accolades.Count == 0) return;

            foreach (var accolade in profile.accolades)
            {
                var item  = Instantiate(accoladeItemPrefab, accoladesList);
                var texts = item.GetComponentsInChildren<TMP_Text>();
                if (texts.Length >= 1) texts[0].text = accolade;
            }
        }

        // ── Original: objectives (unchanged) ─────────────────────────────
        void RenderObjectives(DriverProfile profile)
        {
            if (objectivesList == null || objectiveRowPrefab == null) return;
            foreach (Transform child in objectivesList) Destroy(child.gameObject);
            if (profile?.objectives == null) return;

            foreach (var obj in profile.objectives)
            {
                var row    = Instantiate(objectiveRowPrefab, objectivesList);
                var texts  = row.GetComponentsInChildren<TMP_Text>();
                var images = row.GetComponentsInChildren<Image>();

                if (texts.Length >= 1) texts[0].text = obj.description;

                if (images.Length >= 2 && obj.targetValue > 0)
                    images[1].fillAmount = Mathf.Clamp01(obj.progressValue / obj.targetValue);

                if (obj.status == ObjectiveStatus.Met && images.Length >= 1)
                    images[0].color = COL_GREEN;
            }
        }

        // ── Original: standings (unchanged) ──────────────────────────────
        void RenderStandings()
        {
            if (standingsList == null || standingRowPrefab == null) return;
            foreach (Transform child in standingsList) Destroy(child.gameObject);

            var standings = integration?.GetChampionshipStandings();
            if (standings == null) return;

            int count = Mathf.Min(standings.Count, 5);
            for (int i = 0; i < count; i++)
            {
                var entry = standings[i];
                var row   = Instantiate(standingRowPrefab, standingsList);
                var texts = row.GetComponentsInChildren<TMP_Text>();

                if (texts.Length >= 3)
                {
                    texts[0].text = entry.position.ToString();
                    texts[1].text = entry.driverName;
                    texts[2].text = $"{entry.points} PTS";
                }
                if (entry.isPlayer)
                    row.GetComponent<Image>().color = new Color(0.91f, 0f, 0.18f, 0.2f);
            }
        }

        // ── Original: season ended ────────────────────────────────────────
        void OnSeasonEnded(int finalPosition) => RefreshUI();

        // ── Helpers ───────────────────────────────────────────────────────
        Color GetReputationTierColor(string tier)
        {
            if (tier == null) return Color.white;
            return tier.ToLower() switch
            {
                "rookie"     => COL_GREY,
                "contender"  => COL_BLUE,
                "frontrunner"=> COL_GREEN,
                "elite"      => COL_GOLD,
                "legend"     => COL_ORANGE,
                _            => Color.white
            };
        }
    }
}