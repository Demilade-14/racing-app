using UnityEngine;
using System.Collections.Generic;
using RacingGame.Race; // References DriverResult from your simulation engine
namespace RacingGame.Broadcast
{
    /// <summary>
    /// Analyzes race results after a session to automatically generate a "Highlights" reel.
    /// </summary>
    public class HighlightSystem
    {
        /// <summary>
        /// Detect key moments from the final race results.
        /// </summary>
        public List<BroadcastEvent> DetectHighlights(List<DriverResult> results)
        {
            List<BroadcastEvent> highlights = new List<BroadcastEvent>();
            if (results == null || results.Count == 0) return highlights;
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                // Race Winner
                if (i == 0)
                {
                    highlights.Add(new BroadcastEvent
                    {
                        type = BroadcastEventType.LeaderChange,
                        driverA = result.driverName,
                        intensity = 1.0f,
                        lap = -1 // Post-race
                    });
                }
                // DNF / Crash
                if (result.dnf)
                {
                    highlights.Add(new BroadcastEvent
                    {
                        type = BroadcastEventType.Crash,
                        driverA = result.driverName,
                        intensity = 0.9f,
                        lap = -1
                    });
                }
            }
            return highlights;
        }
    }
}
