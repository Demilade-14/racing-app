using System.Collections.Generic;
using UnityEngine;
using RacingGame.Drivers;
using RacingGame.Transfers;
using RacingGame.Career;
namespace RacingGame.Teams.AI
{
    /// <summary>
    /// Global manager that runs AI decisions for ALL teams in the paddock.
    /// Attach to a persistent GameObject in your career scene.
    /// </summary>
    public class PaddockAIManager : MonoBehaviour
    {
        public static PaddockAIManager Instance { get; private set; }
        [Header("Team Principals")]
        public List<TeamPrincipalAI> teams = new List<TeamPrincipalAI>();
        [Header("Player Context")]
        public PlayerCareerContext player;
        [Header("Events")]
        public System.Action<string, string> OnTeamDecision;  // (teamName, decision)
        public System.Action OnPaddockDecisionsComplete;
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
        void Start()
        {
            InitializePaddock();
        }
        /// <summary>
        /// Initialize all team principals from the TransferTeamDatabase.
        /// </summary>
        public void InitializePaddock()
        {
            teams.Clear();
            // Assign personalities based on team characteristics
            foreach (var teamData in TransferTeamDatabase.Teams)
            {
                var principal = new TeamPrincipalAI
                {
                    teamName = teamData.teamName,
                    teamBudget = teamData.budget,
                    performanceExpectation = teamData.carPerformance,
                    driver1Name = teamData.drivers.Count > 0 ? teamData.drivers[0] : null,
                    driver2Name = teamData.drivers.Count > 1 ? teamData.drivers[1] : null
                };
                // Assign personality based on team profile
                principal.personality = DeterminePersonality(teamData);
                // Set favored driver (usually the higher-rated one)
                if (principal.driver1Name != null && principal.driver2Name != null)
                {
                    var d1 = DriverDatabase.GetDriverByName(principal.driver1Name);
                    var d2 = DriverDatabase.GetDriverByName(principal.driver2Name);
                    if (d1 != null && d2 != null)
                    {
                        principal.favoredDriver = d1.GetOverallRating() >= d2.GetOverallRating() 
                            ? principal.driver1Name 
                            : principal.driver2Name;
                    }
                    else
                    {
                        principal.favoredDriver = principal.driver1Name;
                    }
                }
                teams.Add(principal);
            }
            Debug.Log($"[PaddockAI] Initialized {teams.Count} team principals");
        }
        /// <summary>
        /// Determine a team's personality based on their characteristics.
        /// </summary>
        TeamPersonalityType DeterminePersonality(TransferTeamData teamData)
        {
            // Top teams with high budgets tend to be ruthless
            if (teamData.carPerformance > 95f && teamData.budget > 450_000_000f)
                return TeamPersonalityType.Ruthless;
            // Midfield teams with moderate budgets are balanced
            if (teamData.carPerformance > 80f && teamData.carPerformance < 95f)
                return TeamPersonalityType.Balanced;
            // Backmarker teams with low budgets are either conservative or aggressive
            if (teamData.budget < 300_000_000f)
                return Random.value < 0.5f ? TeamPersonalityType.Conservative : TeamPersonalityType.Aggressive;
            // New teams (Audi, Cadillac) are aggressive (trying to prove themselves)
            if (teamData.isNewTeam2026)
                return TeamPersonalityType.Aggressive;
            return TeamPersonalityType.Balanced;
        }
        /// <summary>
        /// Run end-of-season decisions for all teams.
        /// Call this during the season transition.
        /// </summary>
        public void RunEndOfSeasonDecisions()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("  PADDOCK AI: END OF SEASON DECISIONS");
            Debug.Log("═══════════════════════════════════════");
            foreach (var team in teams)
            {
                team.ClearLog();
                // Get driver data
                DriverData d1 = !string.IsNullOrEmpty(team.driver1Name) 
                    ? DriverDatabase.GetDriverByName(team.driver1Name) 
                    : null;
                DriverData d2 = !string.IsNullOrEmpty(team.driver2Name) 
                    ? DriverDatabase.GetDriverByName(team.driver2Name) 
                    : null;
                // Evaluate season performance (based on car performance + random variance)
                float seasonPerformance = team.performanceExpectation + Random.Range(-8f, 8f);
                team.EvaluateSeasonPerformance(seasonPerformance);
                // Make driver decisions
                string decision = team.DecideDriverAction(d1, d2, player);
                OnTeamDecision?.Invoke(team.teamName, decision);
                // Make contract decisions for both drivers
                if (d1 != null) team.MakeContractDecision(d1, d1.GetOverallRating() * 100_000f);
                if (d2 != null) team.MakeContractDecision(d2, d2.GetOverallRating() * 100_000f);
            }
            OnPaddockDecisionsComplete?.Invoke();
        }
        /// <summary>
        /// Get a specific team's principal by team name.
        /// </summary>
        public TeamPrincipalAI GetTeamPrincipal(string teamName)
        {
            return teams.Find(t => t.teamName.ToLower() == teamName.ToLower());
        }
        /// <summary>
        /// Get all decisions made this season (for UI display).
        /// </summary>
        public Dictionary<string, List<string>> GetAllDecisions()
        {
            var allDecisions = new Dictionary<string, List<string>>();
            foreach (var team in teams)
            {
                allDecisions[team.teamName] = new List<string>(team.DecisionLog);
            }
            return allDecisions;
        }
        /// <summary>
        /// Print all team decisions to console (for debugging).
        /// </summary>
        public void PrintAllDecisions()
        {
            foreach (var team in teams)
            {
                Debug.Log($"\n═══ {team.teamName} ({team.personality}) ═══");
                foreach (var decision in team.DecisionLog)
                {
                    Debug.Log($"  {decision}");
                }
            }
        }
    }
}
