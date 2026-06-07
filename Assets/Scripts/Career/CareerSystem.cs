using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RacingGame.Data;
using RacingGame.Manager;

namespace RacingGame.Career
{
    // ═══════════════════════════════════════════════════════════════════════
    //  ENUMS / STRUCTS
    // ═══════════════════════════════════════════════════════════════════════
    public enum SeriesTier { Formula3, Formula2, Formula1 }

    [Serializable]
    public class Contract
    {
        public string     teamName;
        public SeriesTier tier;
        public float      annualSalary;
        public float      performanceBonus;
        public float      winBonus;
        public int        durationSeasons;
        public float      developmentTokens;  // car upgrade budget
        public int        racesRemaining;
    }

    [Serializable]
    public class Sponsor
    {
        public string sponsorName;
        public string category;
        public float  raceIncome;
        public float  podiumBonus;
        public float  winBonus;
        public float  reputationPerRace;
        public int    remainingRaces;
    }

    [Serializable]
    public class RaceWeekend
    {
        public int         round;
        public string      circuitName;
        public bool        fp1Done;
        public bool        fp2Done;
        public bool        qualiDone;
        public int         gridPosition;
        public RaceResult  result;
        public bool        IsComplete => result != null;
    }

    [Serializable]
    public class Rival
    {
        public string driverName;
        public string teamName;
        public float  relationshipScore;  // -1 hostile → +1 friendly
        public int    headToHeadWins;
        public int    headToHeadLosses;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CAREER SAVE
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class CareerSave
    {
        public string        playerId        = Guid.NewGuid().ToString("N");
        public DriverStats   driver          = new();
        public SeriesTier    tier            = SeriesTier.Formula3;
        public int           season          = 1;
        public float         balance         = 250_000f;
        public float         reputation      = 10f;     // 0–100
        public Contract      activeContract;
        public List<Sponsor>     sponsors    = new();
        public List<RaceWeekend> calendar    = new();
        public List<Rival>       rivals      = new();

        // Season stats
        public int seasonPoints;
        public int seasonWins;
        public int seasonPodiums;
        public int seasonPoles;
        public int seasonFastestLaps;
        public int seasonDNFs;

        // Career totals
        public int careerPoints;
        public int careerWins;
        public int careerPodiums;
        public int careerRaces;

        // Skill economy
        public int  skillPoints;
        public bool hasCustomDriver;
        public int  careerAge = 18;    // driver age advances each season

        public CareerType careerType = CareerType.Driver;
        public string selectedDriverIcon;
        public string ambitionSummary;

        public List<CareerObjective> objectives = new();
        public List<Accolade> accolades = new();
        public List<ScenarioCard> scenarioDeck = new();
        public MyTeamSave managerCareer = new();

        // Upgrades purchased
        public Dictionary<string, int> upgradesPurchased = new();
    }

    [Serializable]
    public class CareerObjective
    {
        public string description;
        public float  completionProgress;
        public float  rewardCash;
        public int    rewardSkillPoints;
        public bool   isCompleted;
    }

    [Serializable]
    public class Accolade
    {
        public string title;
        public string description;
        public int    seasonEarned;
    }

    [Serializable]
    public class ScenarioCard
    {
        public string title;
        public string summary;
        public string choiceA;
        public string choiceB;
        public float  riskFactor;
        public float  rewardMultiplier;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CAREER MANAGER
    // ═══════════════════════════════════════════════════════════════════════
    public class CareerManager : MonoBehaviour
    {
        public CareerSave Save { get; private set; } = new();

        static readonly int[] POINTS_TABLE = { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };

        // ── Race result processing ────────────────────────────────────────
        public void ProcessRaceResult(RaceResult result, RaceWeekend weekend)
        {
            weekend.result = result;

            int pts = PointsFor(result.finishPosition);
            if (result.hasFastestLap && result.finishPosition <= 10) pts += 1;

            Save.seasonPoints      += pts;
            Save.careerPoints      += pts;
            Save.careerRaces++;
            Save.skillPoints       += SkillPointsEarned(result);
            Save.reputation         = Mathf.Clamp(Save.reputation + ReputationGain(result), 0f, 100f);

            if (!result.retired) Save.careerAge = Mathf.Min(Save.careerAge, 45);

            if (result.finishPosition == 1)  { Save.seasonWins++; Save.careerWins++; }
            if (result.finishPosition <= 3)  { Save.seasonPodiums++; Save.careerPodiums++; }
            if (result.hasFastestLap)          Save.seasonFastestLaps++;
            if (result.retired)                { Save.seasonDNFs++; }
            if (weekend.gridPosition == 1)     Save.seasonPoles++;

            // Income
            float income = (Save.activeContract?.annualSalary ?? 0f) / 20f;
            if (result.finishPosition == 1)
                income += Save.activeContract?.winBonus ?? 0f;
            if (result.finishPosition <= 3)
                income += Save.activeContract?.performanceBonus ?? 0f;

            foreach (var s in Save.sponsors)
            {
                income              += s.raceIncome;
                Save.reputation     += s.reputationPerRace;
                if (result.finishPosition <= 3) income += s.podiumBonus;
                if (result.finishPosition == 1) income += s.winBonus;
                s.remainingRaces--;
            }
            Save.sponsors.RemoveAll(s => s.remainingRaces <= 0);
            Save.balance += income;

            // Update rival
            UpdateRivals(result);
        }

        // ── End of season ─────────────────────────────────────────────────
        public void EndSeason(List<TeamData> availableTeams)
        {
            bool promoted = Save.seasonPoints >= PromotionThreshold();

            if (promoted && Save.tier < SeriesTier.Formula1)
            {
                Save.tier = (SeriesTier)((int)Save.tier + 1);
            }

            Save.season++;
            Save.careerAge++;
            Save.seasonPoints      = Save.seasonWins = Save.seasonPodiums = 0;
            Save.seasonPoles       = Save.seasonFastestLaps = Save.seasonDNFs = 0;
            Save.activeContract    = null;
        }

        int PromotionThreshold() => Save.tier switch
        {
            SeriesTier.Formula3 => 110,
            SeriesTier.Formula2 => 165,
            _                   => int.MaxValue
        };

        // ── Contracts ─────────────────────────────────────────────────────
        public List<Contract> GenerateOffers(List<TeamData> teams)
        {
            var offers = new List<Contract>();
            foreach (var team in teams)
            {
                float power = (team.enginePower + team.aeroEfficiency) / 2f;
                // Teams only offer within ±30 reputation band
                if (Mathf.Abs(power - Save.reputation) > 32f) continue;

                float salary = CalculateSalary(power);

                offers.Add(new Contract
                {
                    teamName          = team.teamName,
                    tier              = Save.tier,
                    annualSalary      = salary,
                    performanceBonus  = salary * 0.12f,
                    winBonus          = salary * 0.08f,
                    durationSeasons   = UnityEngine.Random.Range(1, 4),
                    developmentTokens = team.annualBudget * 0.04f,
                    racesRemaining    = 20
                });
            }
            return offers.OrderByDescending(o => o.annualSalary).ToList();
        }

        float CalculateSalary(float teamPower) =>
            60_000f * (Save.reputation / 100f) * (teamPower / 100f)
            * UnityEngine.Random.Range(0.92f, 1.10f);

        public void SignContract(Contract contract) =>
            Save.activeContract = contract;

        // ── Sponsors ──────────────────────────────────────────────────────
        public void AddSponsor(Sponsor sponsor) => Save.sponsors.Add(sponsor);

        public List<Sponsor> GenerateSponsorOffers()
        {
            return new List<Sponsor>
            {
                new() { sponsorName="TechPulse",   category="Technology",  raceIncome=15000, podiumBonus=5000, winBonus=10000, reputationPerRace=0.3f, remainingRaces=10 },
                new() { sponsorName="VeloFuel",    category="Fuel",        raceIncome=12000, podiumBonus=4000, winBonus=8000,  reputationPerRace=0.2f, remainingRaces=20 },
                new() { sponsorName="ApexWear",    category="Apparel",     raceIncome=8000,  podiumBonus=3000, winBonus=6000,  reputationPerRace=0.1f, remainingRaces=15 },
                new() { sponsorName="NovaBanking", category="Finance",     raceIncome=20000, podiumBonus=8000, winBonus=15000, reputationPerRace=0.5f, remainingRaces=5  },
            };
        }

        // ── Skill upgrades ────────────────────────────────────────────────
        public bool UpgradeStat(string stat, int cost = 3)
        {
            if (Save.skillPoints < cost) return false;
            Save.skillPoints -= cost;

            switch (stat)
            {
                case "speed":          Save.driver.speed          = Mathf.Min(100, Save.driver.speed + 1);          break;
                case "braking":        Save.driver.braking        = Mathf.Min(100, Save.driver.braking + 1);        break;
                case "cornering":      Save.driver.cornering      = Mathf.Min(100, Save.driver.cornering + 1);      break;
                case "racecraft":      Save.driver.racecraft      = Mathf.Min(100, Save.driver.racecraft + 1);      break;
                case "consistency":    Save.driver.consistency    = Mathf.Min(100, Save.driver.consistency + 1);    break;
                case "wetWeather":     Save.driver.wetWeather     = Mathf.Min(100, Save.driver.wetWeather + 1);     break;
                case "tireManagement": Save.driver.tireManagement = Mathf.Min(100, Save.driver.tireManagement + 1); break;
            }
            return true;
        }

        // ── Rival system ──────────────────────────────────────────────────
        void UpdateRivals(RaceResult myResult)
        {
            // Find rival who finished just ahead or behind
            // Simplified: nearest rival by position
            foreach (var rival in Save.rivals)
            {
                if (myResult.finishPosition == 1)
                    rival.headToHeadWins++;
                else
                    rival.headToHeadLosses++;
            }
        }

        public void AddRival(string driverName, string teamName)
        {
            if (Save.rivals.Any(r => r.driverName == driverName)) return;
            Save.rivals.Add(new Rival
            { driverName = driverName, teamName = teamName, relationshipScore = 0f });
        }

        // ── Calendar builder ──────────────────────────────────────────────
        public List<RaceWeekend> BuildCalendar(List<CircuitData> circuits)
        {
            var calendar = new List<RaceWeekend>();
            var shuffled = circuits.OrderBy(_ => Guid.NewGuid()).ToList();
            int rounds = Save.tier switch
            {
                SeriesTier.Formula3 => 10,
                SeriesTier.Formula2 => 15,
                _                   => 23
            };

            for (int i = 0; i < Mathf.Min(rounds, shuffled.Count); i++)
                calendar.Add(new RaceWeekend { round = i + 1, circuitName = shuffled[i].circuitName });

            Save.calendar = calendar;
            return calendar;
        }

        // ── Helpers ───────────────────────────────────────────────────────
        static int PointsFor(int pos) =>
            pos >= 1 && pos <= POINTS_TABLE.Length ? POINTS_TABLE[pos - 1] : 0;

        static int SkillPointsEarned(RaceResult r)
        {
            int pts = 1;
            if (r.finishPosition <= 10) pts++;
            if (r.finishPosition <= 3)  pts += 2;
            if (r.finishPosition == 1)  pts += 3;
            if (!r.retired)             pts++;
            if (r.hasFastestLap)        pts++;
            return pts;
        }

        static float ReputationGain(RaceResult r)
        {
            float gain = 0.4f;
            gain += Mathf.Max(0, 10 - r.finishPosition) * 0.25f;
            if (r.finishPosition == 1)  gain += 2.5f;
            if (r.hasFastestLap)        gain += 0.5f;
            if (r.retired)              gain -= 1.5f;
            return gain;
        }
    }
}
