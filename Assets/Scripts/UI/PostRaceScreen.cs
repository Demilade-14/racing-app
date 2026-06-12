using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RacingGame.Career;

namespace RacingGame.UI
{
    // ═══════════════════════════════════════════════════════════════════════
    //  POST RACE SCREEN  —  2026 Extension
    //  Adds: ERS usage summary, active aero efficiency rating, mandatory
    //        tyre rule compliance badge, overtake mode count, penalty
    //        display, XP gain breakdown, rival reaction panel,
    //        newspaper headline ticker, animated position reveal.
    //  All original fields and methods retained unchanged.
    // ═══════════════════════════════════════════════════════════════════════
    public class PostRaceScreen : MonoBehaviour
    {
        // ── Original fields ───────────────────────────────────────────────
        [Header("Header")]
        [SerializeField] private TextMeshProUGUI circuitNameText;

        [Header("Result Card")]
        [SerializeField] private TextMeshProUGUI positionText;
        [SerializeField] private TextMeshProUGUI pointsText;
        [SerializeField] private TextMeshProUGUI overtakesText;
        [SerializeField] private GameObject      fastestLapIndicator;

        [Header("Podium (Top 3 Only)")]
        [SerializeField] private GameObject      podiumPanel;
        [SerializeField] private TextMeshProUGUI p1Text;
        [SerializeField] private TextMeshProUGUI p2Text;
        [SerializeField] private TextMeshProUGUI p3Text;

        [Header("Rating/Rep Changes")]
        [SerializeField] private TextMeshProUGUI ratingChangeText;
        [SerializeField] private TextMeshProUGUI repChangeText;

        [Header("Objectives")]
        [SerializeField] private Transform  objectivesList;
        [SerializeField] private GameObject objectiveRowPrefab;

        [Header("Championship Standings")]
        [SerializeField] private Transform  standingsList;
        [SerializeField] private GameObject standingRowPrefab;

        [Header("Buttons")]
        [SerializeField] private Button nextEventBtn;
        [SerializeField] private Button returnToHubBtn;

        // ── 2026 new fields ───────────────────────────────────────────────
        [Header("2026 — ERS Summary")]
        [SerializeField] private TextMeshProUGUI ersDeployedText;   // "3820 kJ deployed"
        [SerializeField] private TextMeshProUGUI ersHarvestedText;  // "1640 kJ harvested"
        [SerializeField] private TextMeshProUGUI overtakeModeUsesText; // "Overtake Mode: 3x"

        [Header("2026 — Tyre Rule Badge")]
        [SerializeField] private GameObject tyreRuleCompliedBadge;  // Green tick
        [SerializeField] private GameObject tyreRuleViolatedBadge;  // Red warning
        [SerializeField] private TextMeshProUGUI tyreRuleText;

        [Header("2026 — Penalty Display")]
        [SerializeField] private GameObject      penaltyPanel;
        [SerializeField] private TextMeshProUGUI penaltyText;

        [Header("2026 — XP Breakdown")]
        [SerializeField] private TextMeshProUGUI xpTotalText;
        [SerializeField] private Transform       xpBreakdownList;
        [SerializeField] private GameObject      xpRowPrefab;

        [Header("2026 — Rival Reaction")]
        [SerializeField] private GameObject      rivalReactionPanel;
        [SerializeField] private TextMeshProUGUI rivalReactionText;

        [Header("2026 — Newspaper Headline")]
        [SerializeField] private TextMeshProUGUI headlineText;
        [SerializeField] private GameObject      headlinePanel;

        [Header("2026 — Animated Reveal")]
        [SerializeField] private CanvasGroup     resultCardGroup;
        [SerializeField] private float           revealDuration = 0.6f;

        // ── Events ────────────────────────────────────────────────────────
        public System.Action OnNextEvent;
        public System.Action OnReturnToHub;

        // ── Colors ────────────────────────────────────────────────────────
        private static readonly Color COL_GOLD   = new Color(1f,    0.84f, 0f);
        private static readonly Color COL_GREEN  = new Color(0.13f, 0.77f, 0.37f);
        private static readonly Color COL_ORANGE = new Color(0.91f, 0f,   0.18f);
        private static readonly Color COL_GREY   = new Color(0.4f,  0.4f,  0.4f);
        private static readonly Color COL_BLUE   = new Color(0.38f, 0.65f, 0.98f);

        // ─────────────────────────────────────────────────────────────────
        void Start()
        {
            nextEventBtn?.onClick.AddListener(()   => OnNextEvent?.Invoke());
            returnToHubBtn?.onClick.AddListener(() => OnReturnToHub?.Invoke());
        }

        // ═════════════════════════════════════════════════════════════════
        //  ShowResults  (original signature + 2026 optional overload)
        // ═════════════════════════════════════════════════════════════════

        /// <summary>Original signature — works as before.</summary>
        public void ShowResults(RaceResult result, DriverRaceWeekend weekend,
                                List<ChampionshipEntry> standings, DriverProfile profile)
        {
            ShowResults(result, weekend, standings, profile, xpGained: 0,
                        ersDeployedKJ: 0f, ersHarvestedKJ: 0f,
                        overtakeModeUses: 0, tyreRuleComplied: true,
                        rivalReaction: null, headline: null);
        }

        /// <summary>2026 full overload with ERS/tyre/rival data.</summary>
        public void ShowResults(
            RaceResult result,
            DriverRaceWeekend weekend,
            List<ChampionshipEntry> standings,
            DriverProfile profile,
            int   xpGained,
            float ersDeployedKJ,
            float ersHarvestedKJ,
            int   overtakeModeUses,
            bool  tyreRuleComplied,
            string rivalReaction,
            string headline)
        {
            if (result == null) return;

            // ── Original: header ──────────────────────────────────────────
            if (circuitNameText)
                circuitNameText.text = $"{weekend?.circuitName ?? "Circuit"} Grand Prix";

            // ── Original: position ────────────────────────────────────────
            if (positionText)
            {
                if (result.retired)
                {
                    positionText.text  = "DNF";
                    positionText.color = COL_GREY;
                }
                else
                {
                    positionText.text  = $"P{result.finishPosition}";
                    positionText.color = result.finishPosition == 1 ? COL_GOLD
                                       : result.finishPosition <= 3 ? Color.white
                                       : COL_GREY;
                }
            }

            // ── Original: points / overtakes / FL ────────────────────────
            if (pointsText)   pointsText.text   = $"+{result.pointsEarned} PTS";
            if (overtakesText) overtakesText.text = $"{weekend?.overtakesThisRace ?? 0} OVERTAKES";
            if (fastestLapIndicator) fastestLapIndicator.SetActive(result.hasFastestLap);

            // ── Original: podium ──────────────────────────────────────────
            if (podiumPanel) podiumPanel.SetActive(!result.retired && result.finishPosition <= 3);
            if (!result.retired && result.finishPosition <= 3)
            {
                if (p1Text) p1Text.text = result.finishPosition == 1 ? "YOU" : "P1";
                if (p2Text) p2Text.text = result.finishPosition == 2 ? "YOU" : "P2";
                if (p3Text) p3Text.text = result.finishPosition == 3 ? "YOU" : "P3";
            }

            // ── Original: rating / rep ────────────────────────────────────
            float ratingChange = result.finishPosition <= 3 ? 2f : 0.5f;
            float repChange    = result.finishPosition <= 10 ? 1.5f : -0.5f;

            if (ratingChangeText)
                ratingChangeText.text = $"DRIVER RATING {(ratingChange >= 0 ? "↑" : "↓")} {Mathf.Abs(ratingChange):F1}";
            if (repChangeText)
                repChangeText.text = $"REPUTATION {(repChange >= 0 ? "↑" : "↓")} {Mathf.Abs(repChange):F1}";

            // ── 2026: penalty ─────────────────────────────────────────────
            bool hasPenalty = result.penaltySeconds > 0f || result.penalty != PenaltyType.None;
            if (penaltyPanel) penaltyPanel.SetActive(hasPenalty);
            if (hasPenalty && penaltyText)
                penaltyText.text = result.penalty == PenaltyType.None
                    ? $"+{result.penaltySeconds:F0}s time penalty"
                    : $"{result.penalty} — +{result.penaltySeconds:F0}s";

            // ── 2026: ERS summary ─────────────────────────────────────────
            if (ersDeployedText)
                ersDeployedText.text  = $"{ersDeployedKJ:F0} kJ deployed";
            if (ersHarvestedText)
                ersHarvestedText.text = $"{ersHarvestedKJ:F0} kJ harvested";
            if (overtakeModeUsesText)
                overtakeModeUsesText.text = $"Overtake Mode: {overtakeModeUses}×";

            // ── 2026: tyre rule badge ─────────────────────────────────────
            if (tyreRuleCompliedBadge) tyreRuleCompliedBadge.SetActive(tyreRuleComplied);
            if (tyreRuleViolatedBadge) tyreRuleViolatedBadge.SetActive(!tyreRuleComplied);
            if (tyreRuleText)
                tyreRuleText.text = tyreRuleComplied
                    ? "✓ Mandatory tyre rule satisfied"
                    : "✗ Tyre rule violated — +30s penalty applied";
            if (tyreRuleText)
                tyreRuleText.color = tyreRuleComplied ? COL_GREEN : COL_ORANGE;

            // ── 2026: XP breakdown ────────────────────────────────────────
            if (xpTotalText) xpTotalText.text = $"+{xpGained} XP";
            RenderXPBreakdown(result, weekend, xpGained);

            // ── 2026: rival reaction ──────────────────────────────────────
            bool hasRival = !string.IsNullOrEmpty(rivalReaction);
            if (rivalReactionPanel) rivalReactionPanel.SetActive(hasRival);
            if (hasRival && rivalReactionText) rivalReactionText.text = rivalReaction;

            // ── 2026: headline ────────────────────────────────────────────
            bool hasHeadline = !string.IsNullOrEmpty(headline);
            if (headlinePanel) headlinePanel.SetActive(hasHeadline);
            if (hasHeadline && headlineText) headlineText.text = headline;

            // ── Original: objectives + standings ─────────────────────────
            RenderObjectives(profile);
            RenderStandings(standings);

            // ── 2026: animated reveal ─────────────────────────────────────
            if (resultCardGroup != null) StartCoroutine(RevealResultCard());
        }

        // ── 2026: XP breakdown rows ───────────────────────────────────────
        void RenderXPBreakdown(RaceResult result, DriverRaceWeekend weekend, int total)
        {
            if (xpBreakdownList == null || xpRowPrefab == null) return;
            foreach (Transform child in xpBreakdownList) Destroy(child.gameObject);

            var lines = new Dictionary<string, int>();

            // Position XP
            int posXP = result.finishPosition switch
            {
                1    => 50, 2 => 45, 3 => 40,
                <= 6  => 30, <= 10 => 20,
                _ => result.retired ? 0 : 10
            };
            if (posXP > 0) lines[$"P{result.finishPosition} Finish"] = posXP;

            // Fastest lap
            if (result.hasFastestLap) lines["Fastest Lap"] = 15;

            // Overtakes
            int overtakeXP = Mathf.Min((weekend?.overtakesThisRace ?? 0) * 5, 50);
            if (overtakeXP > 0) lines[$"{weekend?.overtakesThisRace} Overtakes"] = overtakeXP;

            foreach (var line in lines)
            {
                var row   = Instantiate(xpRowPrefab, xpBreakdownList);
                var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 2)
                {
                    texts[0].text = line.Key;
                    texts[1].text = $"+{line.Value}";
                    texts[1].color = COL_BLUE;
                }
            }
        }

        // ── 2026: animated card reveal ────────────────────────────────────
        IEnumerator RevealResultCard()
        {
            if (resultCardGroup == null) yield break;
            resultCardGroup.alpha = 0f;
            float t = 0f;
            while (t < revealDuration)
            {
                t += Time.deltaTime;
                resultCardGroup.alpha = Mathf.Clamp01(t / revealDuration);
                yield return null;
            }
            resultCardGroup.alpha = 1f;
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
                var texts  = row.GetComponentsInChildren<TextMeshProUGUI>();
                var images = row.GetComponentsInChildren<Image>();

                if (texts.Length >= 1)
                {
                    texts[0].text  = obj.description;
                    texts[0].color = obj.status == ObjectiveStatus.Met ? COL_GREEN : COL_GREY;
                }
                if (images.Length >= 2 && obj.targetValue > 0)
                    images[1].fillAmount = Mathf.Clamp01(obj.progressValue / obj.targetValue);
                if (obj.status == ObjectiveStatus.Met && texts.Length >= 2)
                {
                    texts[1].text  = "✓";
                    texts[1].color = COL_GREEN;
                }
            }
        }

        // ── Original: standings (unchanged) ──────────────────────────────
        void RenderStandings(List<ChampionshipEntry> standings)
        {
            if (standingsList == null || standingRowPrefab == null || standings == null) return;
            foreach (Transform child in standingsList) Destroy(child.gameObject);

            int count = Mathf.Min(standings.Count, 8);
            for (int i = 0; i < count; i++)
            {
                var entry = standings[i];
                var row   = Instantiate(standingRowPrefab, standingsList);
                var texts = row.GetComponentsInChildren<TextMeshProUGUI>();

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
    }
}