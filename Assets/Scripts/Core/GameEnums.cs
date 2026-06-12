using UnityEngine;
namespace RacingGame.Core
{
    /// <summary>
    /// Central Enum definitions for the entire game.
    /// ⚠️ IMPORTANT: If you have duplicate enums in DriverCareerData.cs or RaceDirector.cs, 
    /// DELETE them from those old files and use this single source of truth!
    /// </summary>
    // ── Tire & Weather ──────────────────────────────────────────────────────
    public enum TireCompound { Soft, Medium, Hard, Intermediate, Wet }
    public enum WeatherCondition { Clear, Cloudy, LightRain, HeavyRain }
    // ── Car Systems (2026 Regulations) ──────────────────────────────────────
    public enum DeploymentMode { Harvest, Balanced, Attack, Hotlap }
    public enum AeroMode { LowDrag, Balanced, MaxDownforce, Auto }
    public enum EngineMode { Eco, Standard, Push, Overtake }
    public enum DamageLevel { None, Minor, Moderate, Severe, Critical }
    // ── Race Control ────────────────────────────────────────────────────────
    public enum FlagStatus { Green, Yellow, SafetyCar, VirtualSafetyCar, Red, Chequered }
    public enum PenaltyType { None, FiveSeconds, TenSeconds, DriveThrough, StopAndGo, Disqualified }
    // ── Game State ──────────────────────────────────────────────────────────
    public enum GamePhase { MainMenu, CareerHub, RaceWeekend, InRace, PostRace, Settings }
}
