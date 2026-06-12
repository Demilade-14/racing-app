namespace RacingGame.Broadcast
{
    /// <summary>
    /// Defines the types of dramatic moments that can occur during a broadcast.
    /// </summary>
    public enum BroadcastEventType
    {
        Overtake,
        Crash,
        PitStop,
        SafetyCar,
        FastestLap,
        LeaderChange,
        ChampionshipUpdate
    }
    /// <summary>
    /// Represents a single broadcast-worthy event.
    /// </summary>
    public class BroadcastEvent
    {
        public BroadcastEventType type;
        public string driverA;
        public string driverB;
        /// <summary>
        /// How "important" or dramatic this event is (0.0 to 1.0).
        /// Used by the Race Director AI to decide what to show on TV.
        /// </summary>
        public float intensity; 
        public int lap;
    }
}
