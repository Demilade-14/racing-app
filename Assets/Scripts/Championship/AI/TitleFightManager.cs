using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RacingGame.Drivers;
using RacingGame.Championship;
namespace RacingGame.Championship.AI
{
    /// <summary>
    /// Tracks the state of the championship battle.
    /// Used by UI to show "Title Fight" banners and by Race Engine to apply pressure.
    /// </summary>
    public class TitleFightManager : MonoBehaviour
    {
        public static TitleFightManager Instance { get; private set; }
        [Header("Current Title Fight State")]
        public bool IsTitleFightActive { get; private set; }
        public DriverData Contender1 { get; private set; } // Leader
        public DriverData Contender2 { get; private set; } // Challenger
        public float CurrentPressureLevel { get; private set; }
        public int PointsGap { get; private set; }
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
        /// <summary>
        /// Call this at the end of every race to update the title fight state.
        /// </summary>
        public void EvaluateTitleFight(List<DriverStanding> standings, int racesRemaining)
        {
            if (standings == null || standings.Count < 2)
            {
                IsTitleFightActive = false;
                return;
            }
            // Get top 2 drivers
            Contender1 = standings[0].driver;
            Contender2 = standings[1].driver;
            PointsGap = standings[0].points - standings[1].points;
            // Calculate pressure
            CurrentPressureLevel = ChampionshipAI.GetPressureLevel(PointsGap, racesRemaining);
            // A "Title Fight" is active if the gap is within 2 races' worth of points (52 pts)
            // OR if we are in the final 5 races of the season.
            bool mathematicallyClose = PointsGap <= (racesRemaining * 26);
            bool lateSeason = racesRemaining <= 5;
            IsTitleFightActive = mathematicallyClose || lateSeason;
            if (IsTitleFightActive)
            {
                Debug.Log($"[TitleFight] ACTIVE! {Contender1.fullName} leads {Contender2.fullName} by {PointsGap} pts. Pressure: {CurrentPressureLevel:P0}");
            }
        }
        /// <summary>
        /// Check if a specific driver is involved in the title fight.
        /// </summary>
        public bool IsInTitleFight(DriverData driver)
        {
            if (!IsTitleFightActive) return false;
            return driver == Contender1 || driver == Contender2;
        }
        /// <summary>
        /// Check if a specific driver is leading the championship.
        /// </summary>
        public bool IsLeadingChampionship(DriverData driver)
        {
            return driver == Contender1;
        }
    }
}
