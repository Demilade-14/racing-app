using UnityEngine;
using System.Collections.Generic;
namespace RacingGame.Broadcast
{
    /// <summary>
    /// The "Brain" of the broadcast. Evaluates queued events and decides what the TV should show.
    /// </summary>
    public class RaceDirectorAI
    {
        /// <summary>
        /// Evaluate the current queue of events and trigger the most dramatic one.
        /// </summary>
        public void EvaluateBroadcast(List<BroadcastEvent> events)
        {
            if (events == null || events.Count == 0)
                return;
            BroadcastEvent bestEvent = null;
            // Find the event with the highest intensity
            foreach (var e in events)
            {
                if (bestEvent == null || e.intensity > bestEvent.intensity)
                    bestEvent = e;
            }
            if (bestEvent == null)
                return;
            HandleEvent(bestEvent);
            // Remove the handled event from the queue so it doesn't trigger again
            events.Remove(bestEvent);
        }
        void HandleEvent(BroadcastEvent e)
        {
            switch (e.type)
            {
                case BroadcastEventType.Overtake:
                    Debug.Log($"📺 BROADCAST OVERTAKE: {e.driverA} vs {e.driverB} on Lap {e.lap}");
                    // Trigger camera to focus on the battling drivers
                    break;
                case BroadcastEventType.Crash:
                    Debug.Log($"🚨 BROADCAST CRASH: {e.driverA} on Lap {e.lap}");
                    // Trigger slow-motion replay camera
                    break;
                case BroadcastEventType.PitStop:
                    Debug.Log($"🛞 BROADCAST PIT STOP: {e.driverA} on Lap {e.lap}");
                    // Switch to pit lane camera
                    break;
                case BroadcastEventType.SafetyCar:
                    Debug.Log($"🚨 BROADCAST SAFETY CAR DEPLOYED on Lap {e.lap}");
                    // Switch to wide helicopter shot
                    break;
                case BroadcastEventType.FastestLap:
                    Debug.Log($"🟣 BROADCAST FASTEST LAP: {e.driverA} on Lap {e.lap}");
                    break;
            }
        }
    }
}
