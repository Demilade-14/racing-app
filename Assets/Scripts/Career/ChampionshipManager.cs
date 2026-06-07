using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RacingGame.Data;
using RacingGame.Content;

namespace RacingGame.Career
{
    // ═══════════════════════════════════════════════════════════════════════
    //  GRID SLOT  —  who sits in each of the 20 seats this season
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class GridSlot
    {
        public string     teamName;
        public int        seatNumber;       // 1 or 2
        public string     driverName;
        public bool       isPlayer;
        public DriverStats stats;
        public bool       isRetiring;       // set by grid evolution
        public bool       isRookie;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CHAMPIONSHIP MANAGER
    //  Single source of truth for standings — both career modes register here
    // ═══════════════════════════════════════════════════════════════════════
    public class ChampionshipManager : MonoBehaviour
    {
        static readonly int[] POINTS_TABLE = { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };

        public int TotalRounds   { get; private set; }
        public int CurrentRound  { get; private set; }
        public int CurrentSeason { get; private set; } = 1;

        // ── Driver standings (all 20 drivers) ────────────────────────────
        public List<ChampionshipEntry> DriverStandings  { get; } = new();

        // ── Constructor standings (10 teams) ─────────────────────────────
        public List<ChampionshipEntry> TeamStandings    { get; } = new();

        // ── Full grid for the current season ─────────────────────────────
        public List<GridSlot> Grid { get; } = new();

        // ── Events ────────────────────────────────────────────────────────
        public event Action<List<ChampionshipEntry>> OnStandingsUpdated;
        public event Action<int>                     OnSeasonComplete;    // param = season number

        // ─────────────────────────────────────────────────────────────────
        //  INIT
        // ─────────────────────────────────────────────────────────────────
        public void Initialise(int totalRounds, int season = 1)
        {
            TotalRounds   = totalRounds;
            CurrentSeason = season;
            CurrentRound  = 0;

            BuildGrid(GameContent.Teams(), GameContent.Drivers());
            BuildStandings();
        }

        void BuildGrid(List<TeamData> teams, List<DriverStats> drivers)
        {
            Grid.Clear();
            for (int t = 0; t < teams.Count; t++)
            {
                for (int s = 1; s <= 2; s++)
                {
                    int di = t * 2 + (s - 1);
                    if (di >= drivers.Count) break;
                    Grid.Add(new GridSlot
                    {
                        teamName   = teams[t].teamName,
                        seatNumber = s,
                        driverName = drivers[di].driverName,
                        stats      = drivers[di],
                        isPlayer   = false,
                    });
                }
            }
        }

        void BuildStandings()
        {
            DriverStandings.Clear();
            TeamStandings.Clear();

            foreach (var slot in Grid)
            {
                DriverStandings.Add(new ChampionshipEntry
                {
                    driverName = slot.driverName,
                    teamName   = slot.teamName,
                    isPlayer   = slot.isPlayer,
                });
            }

            foreach (var team in GameContent.Teams())
            {
                TeamStandings.Add(new ChampionshipEntry
                {
                    driverName = team.teamName,
                    teamName   = team.teamName,
                });
            }
        }

        // ── Register the player into the grid ────────────────────────────
        public void RegisterPlayer(string driverName, string teamName, int seatNumber)
        {
            // Replace the AI in that seat
            var slot = Grid.FirstOrDefault(g => g.teamName == teamName
                                             && g.seatNumber == seatNumber);
            if (slot != null)
            {
                slot.driverName = driverName;
                slot.isPlayer   = true;
            }
            else
            {
                Grid.Add(new GridSlot
                {
                    teamName   = teamName,
                    seatNumber = seatNumber,
                    driverName = driverName,
                    isPlayer   = true,
                });
            }

            // Ensure championship entry exists
            if (!DriverStandings.Any(e => e.driverName == driverName))
            {
                DriverStandings.Add(new ChampionshipEntry
                {
                    driverName = driverName,
                    teamName   = teamName,
                    isPlayer   = true,
                });
            }
            else
            {
                var e = DriverStandings.First(e => e.driverName == driverName);
                e.teamName = teamName;
                e.isPlayer = true;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  PROCESS RACE RESULTS  (call once per round with all 20 results)
        // ─────────────────────────────────────────────────────────────────
        public void ProcessRound(List<RaceResult> results)
        {
            CurrentRound++;

            foreach (var result in results)
            {
                int pts = PointsFor(result.finishPosition);
                if (result.hasFastestLap && result.finishPosition <= 10) pts++;

                // Driver standing
                var de = DriverStandings.FirstOrDefault(e => e.driverName == result.driverName);
                if (de == null)
                {
                    de = new ChampionshipEntry
                        { driverName = result.driverName, teamName = result.teamName };
                    DriverStandings.Add(de);
                }
                de.points += pts;
                if (result.finishPosition == 1) de.wins++;
                if (result.finishPosition <= 3) de.podiums++;
                if (result.hasFastestLap)       de.fastestLaps++;
                if (CurrentRound - 1 < de.raceResults.Length)
                    de.raceResults[CurrentRound - 1] = result.finishPosition;

                // Constructor standing
                var te = TeamStandings.FirstOrDefault(e => e.teamName == result.teamName);
                if (te != null) te.points += pts;
            }

            ResortStandings();
            OnStandingsUpdated?.Invoke(DriverStandings);

            if (CurrentRound >= TotalRounds)
                OnSeasonComplete?.Invoke(CurrentSeason);
        }

        void ResortStandings()
        {
            DriverStandings.Sort((a, b) =>
            {
                int cmp = b.points.CompareTo(a.points);
                return cmp != 0 ? cmp : b.wins.CompareTo(a.wins);
            });
            for (int i = 0; i < DriverStandings.Count; i++)
                DriverStandings[i].position = i + 1;

            TeamStandings.Sort((a, b) => b.points.CompareTo(a.points));
            for (int i = 0; i < TeamStandings.Count; i++)
                TeamStandings[i].position = i + 1;
        }

        // ── Quick helpers ─────────────────────────────────────────────────
        public int GetPlayerPosition(string driverName) =>
            DriverStandings.FirstOrDefault(e => e.driverName == driverName)?.position ?? 20;

        public int GetPlayerPoints(string driverName) =>
            DriverStandings.FirstOrDefault(e => e.driverName == driverName)?.points ?? 0;

        public int GetTeamPosition(string teamName) =>
            TeamStandings.FirstOrDefault(e => e.teamName == teamName)?.position ?? 10;

        public ChampionshipEntry GetEntry(string driverName) =>
            DriverStandings.FirstOrDefault(e => e.driverName == driverName);

        // Points gap between two drivers (positive = first is ahead)
        public int PointsGap(string driverA, string driverB) =>
            GetPlayerPoints(driverA) - GetPlayerPoints(driverB);

        public bool IsChampionshipDecided(string driverName)
        {
            int  myPts    = GetPlayerPoints(driverName);
            int  roundsLeft = TotalRounds - CurrentRound;
            int  maxGainable = roundsLeft * 26;  // 25 + 1 fastest lap
            var  leader  = DriverStandings.FirstOrDefault();
            if (leader == null || leader.driverName == driverName) return false;
            return (leader.points - myPts) > maxGainable;
        }

        // ─────────────────────────────────────────────────────────────────
        //  PRIZE MONEY  (constructor position → payout)
        // ─────────────────────────────────────────────────────────────────
        public float GetPrizeMoney(string teamName)
        {
            int pos = GetTeamPosition(teamName);
            return pos switch
            {
                1  => 120_000_000f,
                2  =>  95_000_000f,
                3  =>  80_000_000f,
                4  =>  68_000_000f,
                5  =>  58_000_000f,
                6  =>  50_000_000f,
                7  =>  43_000_000f,
                8  =>  37_000_000f,
                9  =>  32_000_000f,
                10 =>  28_000_000f,
                _  =>  20_000_000f,
            };
        }

        // ─────────────────────────────────────────────────────────────────
        //  GRID EVOLUTION  (between seasons)
        // ─────────────────────────────────────────────────────────────────
        public void EvolveGrid()
        {
            var rookieNames = new[]
            {
                "Felix Storm","Mia Rossa","Omar Nasser","Hana Suzuki",
                "Liam Okafor","Petra Vance","Ryo Kimura","Sara Lindqvist"
            };

            int rookieIndex = 0;
            var retireSlots = new List<GridSlot>();

            foreach (var slot in Grid)
            {
                if (slot.isPlayer) continue;
                if (slot.stats == null) continue;

                slot.stats.age++;

                // Retire drivers over 38 with some probability, or random 5% chance
                bool ageRetire   = slot.stats.age > 38 && UnityEngine.Random.value < 0.4f;
                bool randomExit  = UnityEngine.Random.value < 0.05f;

                if (ageRetire || randomExit)
                {
                    slot.isRetiring = true;
                    retireSlots.Add(slot);
                }

                // Young drivers (<25) improve slightly each season
                if (slot.stats.age < 25)
                {
                    slot.stats.speed      = Mathf.Min(99, slot.stats.speed + UnityEngine.Random.Range(0, 3));
                    slot.stats.racecraft  = Mathf.Min(99, slot.stats.racecraft + UnityEngine.Random.Range(0, 2));
                }

                // Veterans (>33) very slightly decline
                if (slot.stats.age > 33)
                    slot.stats.consistency = Mathf.Max(60, slot.stats.consistency - 1);
            }

            // Replace retired slots with rookies
            foreach (var slot in retireSlots)
            {
                slot.isRetiring = false;
                slot.isRookie   = true;
                if (rookieIndex < rookieNames.Length)
                {
                    slot.driverName = rookieNames[rookieIndex++];
                    slot.stats      = new DriverStats
                    {
                        driverName  = slot.driverName,
                        age         = UnityEngine.Random.Range(19, 23),
                        speed       = UnityEngine.Random.Range(72, 84),
                        braking     = UnityEngine.Random.Range(70, 82),
                        cornering   = UnityEngine.Random.Range(70, 83),
                        racecraft   = UnityEngine.Random.Range(68, 80),
                        consistency = UnityEngine.Random.Range(65, 78),
                    };
                }
            }

            // Shuffle team performance slightly (cars get better/worse)
            foreach (var team in GameContent.Teams())
            {
                var te = TeamStandings.FirstOrDefault(t => t.teamName == team.teamName);
                if (te == null) continue;
                team.aeroEfficiency = Mathf.Clamp(
                    team.aeroEfficiency + UnityEngine.Random.Range(-3, 4), 40, 99);
                team.enginePower    = Mathf.Clamp(
                    team.enginePower    + UnityEngine.Random.Range(-2, 3), 40, 99);
            }

            CurrentSeason++;
            CurrentRound = 0;
            BuildStandings();
        }

        // ── AI contract shuffle between seasons ────────────────────────────
        // Moves AI drivers between teams randomly (10% of non-player seats)
        public void SimulateAITransfers()
        {
            var movable = Grid.Where(g => !g.isPlayer && !g.isRookie).ToList();
            int swaps   = Mathf.Max(1, movable.Count / 10);

            for (int i = 0; i < swaps; i++)
            {
                int a = UnityEngine.Random.Range(0, movable.Count);
                int b = UnityEngine.Random.Range(0, movable.Count);
                if (a == b) continue;
                (movable[a].driverName, movable[b].driverName) =
                    (movable[b].driverName, movable[a].driverName);
                (movable[a].stats, movable[b].stats) =
                    (movable[b].stats, movable[a].stats);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  RIVAL DETECTION  (closest driver in championship to the player)
        // ─────────────────────────────────────────────────────────────────
        public DriverRival DetectRival(string playerName)
        {
            var playerEntry = GetEntry(playerName);
            if (playerEntry == null) return null;

            ChampionshipEntry closest = null;
            int minGap = int.MaxValue;

            foreach (var entry in DriverStandings)
            {
                if (entry.driverName == playerName) continue;
                int gap = Math.Abs(entry.points - playerEntry.points);
                if (gap < minGap) { minGap = gap; closest = entry; }
            }

            if (closest == null) return null;

            return new DriverRival
            {
                rivalName      = closest.driverName,
                rivalTeam      = closest.teamName,
                pointsDelta    = closest.points - playerEntry.points,
                intensityScore = Mathf.Clamp01(1f - minGap / 50f),
                isTeammate     = closest.teamName == playerEntry.teamName,
            };
        }

        // ─────────────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────────────
        public static int PointsFor(int pos) =>
            pos >= 1 && pos <= POINTS_TABLE.Length ? POINTS_TABLE[pos - 1] : 0;

        public void ResetForNewSeason()
        {
            foreach (var e in DriverStandings) { e.points = 0; e.wins = 0; e.podiums = 0; e.poles = 0; e.fastestLaps = 0; }
            foreach (var e in TeamStandings)   { e.points = 0; e.wins = 0; }
            CurrentRound = 0;
        }
    }
}
