using UnityEngine;
using System.Collections.Generic;
using RacingGame.Career;
namespace RacingGame.Career
{
    /// <summary>
    /// Manages Free Practice sessions (FP1, FP2, FP3).
    /// Handles program completion, XP rewards, and stat progression.
    /// </summary>
    public static class PracticeManager
    {
        /// <summary>
        /// Complete a practice program and award XP to the driver.
        /// </summary>
        public static void CompleteProgram(DriverProfile profile, DriverRaceWeekend weekend, PracticeProgram program)
        {
            if (program == null || program.completed) return;
            program.completed = true;
            program.score = CalculateProgramScore(profile, program);
            // Award XP based on the stat rewarded
            AwardXP(profile, program.statRewarded, program.xpReward);
            // Award skill points if applicable
            if (program.skillPointReward > 0)
            {
                profile.skillPoints += program.skillPointReward;
            }
            Debug.Log($"[PracticeManager] Completed {program.name}. Awarded {program.xpReward} XP to {program.statRewarded}.");
        }
        /// <summary>
        /// Simulate a full practice session (FP1/FP2/FP3) and mark it as done.
        /// </summary>
        public static void SimulateSession(DriverRaceWeekend weekend, string sessionName)
        {
            switch (sessionName.ToLower())
            {
                case "fp1": weekend.fp1Done = true; break;
                case "fp2": weekend.fp2Done = true; break;
                case "fp3": weekend.fp3Done = true; break;
            }
            Debug.Log($"[PracticeManager] Simulated {sessionName} for {weekend.circuitName}.");
        }
        static float CalculateProgramScore(DriverProfile profile, PracticeProgram program)
        {
            // Score based on relevant driver stat
            float baseStat = program.statRewarded.ToLower() switch
            {
                "pace" => profile.pace,
                "awareness" => profile.awareness,
                "racecraft" => profile.racecraft,
                "experience" => profile.experience,
                _ => 50f
            };
            // Add some randomness (80% - 120% of base performance)
            float variance = Random.Range(0.8f, 1.2f);
            return (baseStat / 99f) * 100f * variance;
        }
        static void AwardXP(DriverProfile profile, string stat, float xp)
        {
            // Apply tyre management skill bonus if relevant
            float multiplier = 1f;
            if (stat.ToLower() == "racecraft" || stat.ToLower() == "awareness")
            {
                multiplier += (profile.tyreManagementSkill / 100f) * 0.1f; // Up to 10% bonus
            }
            float finalXP = xp * multiplier;
            switch (stat.ToLower())
            {
                case "pace": profile.paceXP += finalXP; break;
                case "awareness": profile.awarenessXP += finalXP; break;
                case "racecraft": profile.racecraftXP += finalXP; break;
                case "experience": profile.experienceXP += finalXP; break;
            }
            // Check for stat level up (simplified: every 100 XP = 1 stat point)
            CheckStatLevelUp(profile, stat, finalXP);
        }
        static void CheckStatLevelUp(DriverProfile profile, string stat, float xpGained)
        {
            // Simple level up logic: if XP crosses a threshold, increase stat
            // In a full game, this would be more complex
            int currentStat = stat.ToLower() switch
            {
                "pace" => profile.pace,
                "awareness" => profile.awareness,
                "racecraft" => profile.racecraft,
                "experience" => profile.experience,
                _ => 0
            };
            if (currentStat < 99 && xpGained >= 100f)
            {
                // Level up logic would go here
                Debug.Log($"[PracticeManager] Potential level up for {stat}!");
            }
        }
    }
}
