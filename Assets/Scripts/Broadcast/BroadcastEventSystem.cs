using System.Collections.Generic;
using UnityEngine;
namespace RacingGame.Broadcast
{
    /// <summary>
    /// Listens for moments in the race simulation and queues them as broadcast events.
    /// </summary>
    public class BroadcastEventSystem
    {
        public List<BroadcastEvent> eventQueue = new List<BroadcastEvent>();
        public void RegisterOvertake(string overtaker, string target, int lap)
        {
            eventQueue.Add(new BroadcastEvent
            {
                type = BroadcastEventType.Overtake,
                driverA = overtaker,
                driverB = target,
                lap = lap,
                intensity = 0.7f
            });
        }
        public void RegisterCrash(string driver, int lap)
        {
            eventQueue.Add(new BroadcastEvent
            {
                type = BroadcastEventType.Crash,
                driverA = driver,
                lap = lap,
                intensity = 1.0f // Crashes are maximum priority
            });
        }
        public void RegisterPitStop(string driver, int lap)
        {
            eventQueue.Add(new BroadcastEvent
            {
                type = BroadcastEventType.PitStop,
                driverA = driver,
                lap = lap,
                intensity = 0.4f
            });
        }
        public void RegisterSafetyCar(int lap)
        {
            eventQueue.Add(new BroadcastEvent
            {
                type = BroadcastEventType.SafetyCar,
                lap = lap,
                intensity = 0.9f
            });
        }
        public void RegisterFastestLap(string driver, int lap)
        {
            eventQueue.Add(new BroadcastEvent
            {
                type = BroadcastEventType.FastestLap,
                driverA = driver,
                lap = lap,
                intensity = 0.6f
            });
        }
        /// <summary>
        /// Clear the queue (e.g., at the start of a new session).
        /// </summary>
        public void ClearQueue()
        {
            eventQueue.Clear();
        }
    }
}
