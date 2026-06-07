using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RacingGame.Data;

namespace RacingGame.Manager
{
    // ═══════════════════════════════════════════════════════════════════════
    //  ENUMS
    // ═══════════════════════════════════════════════════════════════════════
    public enum ContractStatus  { Active, Expiring, Expired, Released }
    public enum NegotiationResult { Accepted, Rejected, Countered }
    public enum TransferWindowState { Closed, PreSeason, MidSeason }
    public enum DriverMood { Happy, Neutral, Unhappy, WantsOut }
    public enum TeamRole   { Principal, ChiefEngineer, HeadOfAero, SportingDirector }

    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER MARKET LISTING
    //  Everything a team principal sees on the transfer market screen
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class DriverMarketEntry
    {
        // ── Identity ──────────────────────────────────────────────────────
        public string driverName;
        public string nationality;
        public int    age;
        public string currentTeam;      // "Free Agent" if none
        public bool   isFreeAgent;

        // ── Ratings (0-99 like F1 25) ─────────────────────────────────────
        public int overallRating;       // OVR shown on card
        public int pace;                // raw one-lap speed
        public int racecraft;           // wheel-to-wheel ability
        public int awareness;           // avoiding incidents, spatial sense
        public int experience;          // 0-99 (age + race starts weighted)

        // ── Market value ──────────────────────────────────────────────────
        public float marketValue;       // £ — display as "£42.5M"
        public float askingWage;        // £/season — yearly salary demand
        public float releaseClause;     // 0 = not transferable mid-contract
        public int   contractSeasons;   // years remaining on current deal
        public ContractStatus contractStatus;

        // ── Personality / fit ─────────────────────────────────────────────
        public AIPersonality personality;
        public DriverMood    mood;
        public float         loyaltyFactor;  // 0-1 (1 = very loyal, harder to poach)
        public List<string>  interestedTeams = new(); // AI teams already interested

        // ── Computed helpers ──────────────────────────────────────────────
        public string MarketValueStr  => FormatMoney(marketValue);
        public string AskingWageStr   => FormatMoney(askingWage) + "/season";
        public string OVRBadge        => overallRating.ToString();

        // Star rating 1-5 mapped from OVR
        public int StarRating => overallRating switch
        {
            >= 90 => 5,
            >= 80 => 4,
            >= 70 => 3,
            >= 60 => 2,
            _     => 1
        };

        static string FormatMoney(float v)
        {
            if (v >= 1_000_000f) return $"£{v / 1_000_000f:F1}M";
            if (v >= 1_000f)     return $"£{v / 1_000f:F0}K";
            return $"£{v:F0}";
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  SIGNED DRIVER  —  the two seats in your team
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class SignedDriver
    {
        public DriverMarketEntry entry;
        public DriverStats       stats;

        public float  currentWage;          // negotiated wage per season
        public int    contractSeasonsLeft;
        public int    seatNumber;           // 1 or 2
        public bool   isNumberOne;          // #1 driver status

        public int    seasonPoints;
        public int    seasonWins;
        public int    seasonPodiums;
        public float  driverHappiness;      // 0-100
        public float  performanceMultiplier = 1f;

        // Happiness drivers
        public bool   receivesTeamPriority;
        public bool   hasNewContract;
        public int    racesWithoutPoints;

        public ContractStatus Status =>
            contractSeasonsLeft <= 0 ? ContractStatus.Expired :
            contractSeasonsLeft == 1 ? ContractStatus.Expiring :
            ContractStatus.Active;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CONTRACT OFFER  —  what YOU send to a driver
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class ContractOffer
    {
        public string teamName;
        public float  offeredWage;
        public int    seasons;
        public bool   offersNumberOneStatus;
        public bool   offersCarDevelopmentVote;   // driver gets say in car setup direction
        public float  performanceBonus;           // per win
        public float  signingFee;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TEAM FINANCES
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class TeamFinances
    {
        public float budget;                // total season budget
        public float driverWageBill;        // sum of both driver wages
        public float operatingCosts;        // staff, transport, facilities
        public float carDevelopmentFund;    // spent on upgrades
        public float prizeMoneyEarned;      // from championship position
        public float sponsorIncome;

        public float AvailableCash         => budget - driverWageBill - operatingCosts;
        public float WageBudgetRemaining   => budget * 0.28f - driverWageBill; // 28% of budget for drivers
        public bool  IsOverWageBudget      => driverWageBill > budget * 0.28f;

        public string BudgetStr            => FormatMoney(budget);
        public string WageBillStr          => FormatMoney(driverWageBill);
        public string AvailableCashStr     => FormatMoney(AvailableCash);

        static string FormatMoney(float v) =>
            v >= 1_000_000f ? $"£{v / 1_000_000f:F1}M" : $"£{v / 1_000f:F0}K";
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  MY TEAM SAVE
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class MyTeamSave
    {
        public string        teamName;
        public int           season          = 1;
        public TeamFinances  finances        = new();
        public SignedDriver  driver1;           // seat 1
        public SignedDriver  driver2;           // seat 2
        public TeamData      teamData         = new();

        // Season performance
        public int    constructorPoints;
        public int    constructorPosition;

        // Transfer window
        public TransferWindowState windowState = TransferWindowState.Closed;
        public int    freeAgentSigningsThisSeason;
        public int    maxMidSeasonSignings      = 1;

        // Staff (simplified)
        public int    facilityLevel      = 1;   // 1-5, unlocks better upgrades
        public int    aeroResearchPoints;
        public int    engineResearchPoints;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  NEGOTIATION ENGINE
    //  Simulates driver acceptance/rejection based on:
    //  offer vs asking wage, team prestige, number-one status, driver loyalty
    // ═══════════════════════════════════════════════════════════════════════
    public static class NegotiationEngine
    {
        // Returns result and optionally a counter-offer wage
        public static (NegotiationResult result, float counterWage) Evaluate(
            DriverMarketEntry driver,
            ContractOffer     offer,
            TeamData          team,
            int               teamConstructorPos)   // 1 = leading constructor
        {
            float score = 0f;

            // ── Wage factor (most important) ──────────────────────────────
            float wageRatio = offer.offeredWage / driver.askingWage;
            score += wageRatio >= 1.0f ? 40f
                   : wageRatio >= 0.85f ? 20f
                   : wageRatio >= 0.70f ? 5f
                   : -20f;

            // ── Team prestige ──────────────────────────────────────────────
            float teamStrength = (team.enginePower + team.aeroEfficiency) / 200f;  // 0-1
            score += teamStrength * 25f;

            // ── Constructor position bonus ─────────────────────────────────
            score += Mathf.Clamp(20f - teamConstructorPos * 2f, 0f, 20f);

            // ── Number one status ──────────────────────────────────────────
            if (offer.offersNumberOneStatus && driver.overallRating >= 80)
                score += 15f;

            // ── Contract length preference ─────────────────────────────────
            // Elite drivers prefer short deals (flexibility); young prefer long (security)
            int preferredLength = driver.overallRating >= 85 ? 1 : 3;
            score += offer.seasons == preferredLength ? 10f
                   : Mathf.Abs(offer.seasons - preferredLength) == 1 ? 5f
                   : 0f;

            // ── Loyalty penalty (poaching from existing team) ──────────────
            if (!driver.isFreeAgent)
                score -= driver.loyaltyFactor * 30f;

            // ── Mood modifier ──────────────────────────────────────────────
            score += driver.mood switch
            {
                DriverMood.WantsOut  =>  20f,
                DriverMood.Unhappy   =>  10f,
                DriverMood.Neutral   =>   0f,
                DriverMood.Happy     => -15f,
                _ => 0f
            };

            // ── Competing interest discount (other teams bidding) ──────────
            score -= driver.interestedTeams.Count * 4f;

            // ── Personality tweak ──────────────────────────────────────────
            score += driver.personality switch
            {
                AIPersonality.Aggressive  =>  5f,  // wants top team regardless
                AIPersonality.Conservative => -5f, // risk-averse, prefers stability
                _ => 0f
            };

            // ── Decision ──────────────────────────────────────────────────
            if (score >= 55f) return (NegotiationResult.Accepted, 0f);

            if (score >= 30f)
            {
                // Counter: driver wants wage bump to close the gap
                float counterWage = driver.askingWage * UnityEngine.Random.Range(0.95f, 1.15f);
                return (NegotiationResult.Countered, counterWage);
            }

            return (NegotiationResult.Rejected, 0f);
        }

        // Hard rejection check: driver won't sign below floor wage no matter what
        public static bool IsAbsoluteFloor(DriverMarketEntry driver, float offeredWage) =>
            offeredWage < driver.askingWage * 0.60f;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DRIVER MARKET SERVICE
    //  Generates the full transfer market, tracks AI team bids, ages drivers
    // ═══════════════════════════════════════════════════════════════════════
    public static class DriverMarketService
    {
        // ── Build market from GameContent drivers ─────────────────────────
        public static List<DriverMarketEntry> BuildMarket(
            List<DriverStats> allDrivers,
            List<TeamData>    allTeams,
            int               season)
        {
            var market = new List<DriverMarketEntry>();
            var teamNames = allTeams.Select(t => t.teamName).ToList();

            for (int i = 0; i < allDrivers.Count; i++)
            {
                var d   = allDrivers[i];
                var ovr = ComputeOVR(d);

                // Assign to teams (first 20 drivers fill 10 teams × 2 seats)
                string team   = i < teamNames.Count * 2
                    ? teamNames[i / 2]
                    : "Free Agent";
                bool freeAgent = team == "Free Agent";

                market.Add(new DriverMarketEntry
                {
                    driverName       = d.driverName,
                    nationality      = d.nationality,
                    age              = d.age + (season - 1),   // ages each season
                    currentTeam      = team,
                    isFreeAgent      = freeAgent,
                    overallRating    = ovr,
                    pace             = d.speed,
                    racecraft        = d.racecraft,
                    awareness        = d.consistency,
                    experience       = ComputeExperience(d.age + season - 1),
                    marketValue      = ComputeMarketValue(ovr, d.age + season - 1),
                    askingWage       = ComputeAskingWage(ovr, d.age + season - 1),
                    releaseClause    = freeAgent ? 0f : ComputeMarketValue(ovr, d.age) * 1.4f,
                    contractSeasons  = freeAgent ? 0 : UnityEngine.Random.Range(1, 4),
                    contractStatus   = freeAgent ? ContractStatus.Expired : ContractStatus.Active,
                    personality      = (AIPersonality)(i % 5),
                    mood             = DriverMood.Neutral,
                    loyaltyFactor    = UnityEngine.Random.Range(0.2f, 0.9f),
                });
            }

            return market.OrderByDescending(d => d.overallRating).ToList();
        }

        // ── OVR formula (matches F1 25 style) ────────────────────────────
        // Weighted: pace most important, then racecraft, then consistency
        public static int ComputeOVR(DriverStats d) =>
            Mathf.RoundToInt(
                d.speed         * 0.28f +
                d.racecraft     * 0.22f +
                d.braking       * 0.15f +
                d.cornering     * 0.15f +
                d.consistency   * 0.12f +
                d.wetWeather    * 0.05f +
                d.tireManagement* 0.03f);

        // ── Market value (age peak 24-29, OVR drives base) ─────────────
        public static float ComputeMarketValue(int ovr, int age)
        {
            float ovrBase  = Mathf.Pow(ovr / 99f, 2.5f) * 80_000_000f;
            float ageFactor = AgePeakFactor(age);
            return ovrBase * ageFactor;
        }

        // ── Wage asking price ──────────────────────────────────────────
        public static float ComputeAskingWage(int ovr, int age)
        {
            float wageBase  = Mathf.Pow(ovr / 99f, 2.2f) * 25_000_000f;
            return wageBase * AgePeakFactor(age);
        }

        // Age curve: peaks 24-29, falls off young (<21) and old (>35)
        static float AgePeakFactor(int age)
        {
            if (age < 18) return 0.20f;
            if (age < 21) return Mathf.Lerp(0.20f, 0.70f, (age - 18f) / 3f);
            if (age < 24) return Mathf.Lerp(0.70f, 1.00f, (age - 21f) / 3f);
            if (age <= 29) return 1.00f;
            if (age <= 33) return Mathf.Lerp(1.00f, 0.80f, (age - 29f) / 4f);
            if (age <= 38) return Mathf.Lerp(0.80f, 0.50f, (age - 33f) / 5f);
            return 0.30f;
        }

        static int ComputeExperience(int age) =>
            Mathf.Clamp((age - 17) * 5, 0, 99);

        // ── Simulate AI teams placing bids (call once per transfer window) ─
        public static void SimulateAIBids(
            List<DriverMarketEntry> market,
            List<TeamData>          teams)
        {
            foreach (var driver in market.Where(d => d.isFreeAgent || d.mood >= DriverMood.Unhappy))
            {
                // 1-3 random AI teams show interest
                int interest = UnityEngine.Random.Range(1, 4);
                var candidates = teams
                    .OrderBy(_ => Guid.NewGuid())
                    .Take(interest)
                    .Select(t => t.teamName)
                    .ToList();

                foreach (var t in candidates)
                    if (!driver.interestedTeams.Contains(t))
                        driver.interestedTeams.Add(t);
            }
        }

        // ── Age all drivers between seasons ──────────────────────────────
        public static void AdvanceSeason(List<DriverMarketEntry> market)
        {
            foreach (var d in market)
            {
                d.age++;
                d.contractSeasons = Mathf.Max(0, d.contractSeasons - 1);
                if (d.contractSeasons == 0) d.isFreeAgent = true;

                // Slight OVR progression / decline by age
                int ovrDelta = d.age < 26 ? 1 : d.age > 32 ? -1 : 0;
                d.overallRating = Mathf.Clamp(d.overallRating + ovrDelta, 40, 99);

                // Recalculate financials
                d.marketValue  = ComputeMarketValue(d.overallRating, d.age);
                d.askingWage   = ComputeAskingWage(d.overallRating, d.age);
                d.interestedTeams.Clear();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  MY TEAM MANAGER  —  MonoBehaviour, the main controller
    // ═══════════════════════════════════════════════════════════════════════
    public class MyTeamManager : MonoBehaviour
    {
        // ── State ─────────────────────────────────────────────────────────
        public MyTeamSave Save { get; private set; } = new();

        List<DriverMarketEntry> _market   = new();
        List<TeamData>          _allTeams = new();

        // ── Events (UI listens to these) ───────────────────────────────────
        public event Action<List<DriverMarketEntry>> OnMarketRefreshed;
        public event Action<SignedDriver>            OnDriverSigned;
        public event Action<SignedDriver>            OnDriverReleased;
        public event Action<NegotiationResult, float> OnNegotiationResult;
        public event Action<string>                  OnTransferNews;       // headline text

        // ── Init ──────────────────────────────────────────────────────────
        public void Initialise(string teamName, TeamData teamData,
                               List<DriverStats> allDrivers, List<TeamData> allTeams)
        {
            Save.teamName  = teamName;
            Save.teamData  = teamData;
            _allTeams      = allTeams;

            // Starting budget scales with team power
            float strength = (teamData.enginePower + teamData.aeroEfficiency) / 200f;
            Save.finances.budget         = Mathf.Lerp(40_000_000f, 200_000_000f, strength);
            Save.finances.operatingCosts = Save.finances.budget * 0.30f;

            _market = DriverMarketService.BuildMarket(allDrivers, allTeams, Save.season);
            DriverMarketService.SimulateAIBids(_market, _allTeams);

            Save.windowState = TransferWindowState.PreSeason;
            OnMarketRefreshed?.Invoke(_market);
        }

        // ── Market access ─────────────────────────────────────────────────
        public List<DriverMarketEntry> GetFullMarket()         => _market;
        public List<DriverMarketEntry> GetFreeAgents()         => _market.Where(d => d.isFreeAgent).ToList();
        public List<DriverMarketEntry> GetAvailableDrivers()   =>
            _market.Where(d => d.isFreeAgent
                           || d.contractStatus == ContractStatus.Expiring
                           || d.mood == DriverMood.WantsOut).ToList();

        public List<DriverMarketEntry> GetMarketSorted(string sortBy) => GetMarketSorted(sortBy, false);

        public List<DriverMarketEntry> GetMarketSorted(string sortBy, bool onlyAvailable) 
        {
            var source = onlyAvailable ? GetAvailableDrivers() : _market;
            return sortBy switch
            {
                "ovr"   => source.OrderByDescending(d => d.overallRating).ToList(),
                "pace"  => source.OrderByDescending(d => d.pace).ToList(),
                "value" => source.OrderByDescending(d => d.marketValue).ToList(),
                "wage"  => source.OrderBy(d => d.askingWage).ToList(),
                "age"   => source.OrderBy(d => d.age).ToList(),
                _       => source.ToList()
            };
        }

        public ContractOffer BuildOwnerOffer(DriverMarketEntry driver, bool offersNumberOneStatus = false)
        {
            float wage = Mathf.Max(driver.askingWage * 0.88f, driver.askingWage * 0.98f);
            float signing = driver.isFreeAgent ? driver.marketValue * 0.03f : Mathf.Max(driver.marketValue * 0.06f, 2_000_000f);
            int seasons = Mathf.Clamp(driver.contractSeasons == 0 ? 3 : driver.contractSeasons + 1, 1, 4);

            return new ContractOffer
            {
                teamName = Save.teamName,
                offeredWage = wage,
                seasons = seasons,
                offersNumberOneStatus = offersNumberOneStatus,
                offersCarDevelopmentVote = offersNumberOneStatus,
                performanceBonus = Mathf.Max(100_000f, driver.marketValue * 0.0012f),
                signingFee = signing
            };
        }

        public bool QuickSignByRating(int seat)
        {
            var available = GetMarketSorted("ovr", true);
            if (available.Count == 0) return false;
            var target = available[0];
            return AttemptSign(target, BuildOwnerOffer(target, target.overallRating >= 85), seat);
        }

        public bool QuickSignByValue(int seat)
        {
            var available = GetMarketSorted("value", true);
            if (available.Count == 0) return false;
            var target = available[0];
            return AttemptSign(target, BuildOwnerOffer(target, target.overallRating >= 85), seat);
        }

        // ── Sign driver ───────────────────────────────────────────────────
        public bool AttemptSign(DriverMarketEntry driver, ContractOffer offer, int seat)
        {
            if (Save.windowState == TransferWindowState.Closed)
            {
                OnTransferNews?.Invoke("Transfer window is closed.");
                return false;
            }

            if (Save.windowState == TransferWindowState.MidSeason &&
                Save.freeAgentSigningsThisSeason >= Save.maxMidSeasonSignings)
            {
                OnTransferNews?.Invoke("Mid-season signing limit reached.");
                return false;
            }

            // Absolute floor check first (no negotiation possible)
            if (NegotiationEngine.IsAbsoluteFloor(driver, offer.offeredWage))
            {
                OnNegotiationResult?.Invoke(NegotiationResult.Rejected, 0f);
                OnTransferNews?.Invoke($"{driver.driverName} rejected offer — wage too low.");
                return false;
            }

            var (result, counterWage) = NegotiationEngine.Evaluate(
                driver, offer, Save.teamData,
                Save.constructorPosition == 0 ? 10 : Save.constructorPosition);

            OnNegotiationResult?.Invoke(result, counterWage);

            if (result == NegotiationResult.Accepted)
            {
                CommitSigning(driver, offer, seat);
                return true;
            }

            if (result == NegotiationResult.Countered)
                OnTransferNews?.Invoke(
                    $"{driver.driverName} counters: {FormatMoney(counterWage)}/season.");

            if (result == NegotiationResult.Rejected)
                OnTransferNews?.Invoke($"{driver.driverName} rejected the offer.");

            return false;
        }

        void CommitSigning(DriverMarketEntry driver, ContractOffer offer, int seat)
        {
            // Pay transfer fee / signing fee
            float totalCost = driver.isFreeAgent ? offer.signingFee
                                                 : driver.releaseClause + offer.signingFee;
            Save.finances.budget -= totalCost;

            var signed = new SignedDriver
            {
                entry                = driver,
                stats                = BuildStatsFromEntry(driver),
                currentWage          = offer.offeredWage,
                contractSeasonsLeft  = offer.seasons,
                seatNumber           = seat,
                isNumberOne          = offer.offersNumberOneStatus,
                driverHappiness      = 75f,
            };

            if (seat == 1) Save.driver1 = signed;
            else           Save.driver2 = signed;

            Save.finances.driverWageBill = (Save.driver1?.currentWage ?? 0f)
                                         + (Save.driver2?.currentWage ?? 0f);

            // Remove from market / mark contracted
            driver.isFreeAgent      = false;
            driver.currentTeam      = Save.teamName;
            driver.contractSeasons  = offer.seasons;
            driver.contractStatus   = ContractStatus.Active;

            if (Save.windowState == TransferWindowState.MidSeason)
                Save.freeAgentSigningsThisSeason++;

            OnDriverSigned?.Invoke(signed);
            OnTransferNews?.Invoke($"✅ {driver.driverName} signed! {FormatMoney(offer.offeredWage)}/season.");
        }

        // ── Release driver ────────────────────────────────────────────────
        public void ReleaseDriver(int seat)
        {
            var driver = seat == 1 ? Save.driver1 : Save.driver2;
            if (driver == null) return;

            // Release fee = 20% of remaining contract value
            float penalty = driver.currentWage * driver.contractSeasonsLeft * 0.20f;
            Save.finances.budget -= penalty;

            driver.entry.isFreeAgent     = true;
            driver.entry.currentTeam     = "Free Agent";
            driver.entry.contractSeasons = 0;
            driver.entry.mood            = DriverMood.Unhappy;

            if (seat == 1) Save.driver1 = null;
            else           Save.driver2 = null;

            Save.finances.driverWageBill = (Save.driver1?.currentWage ?? 0f)
                                         + (Save.driver2?.currentWage ?? 0f);

            OnDriverReleased?.Invoke(driver);
            OnTransferNews?.Invoke(
                $"🚨 {driver.entry.driverName} released. Penalty: {FormatMoney(penalty)}");
        }

        // ── Renew contract ────────────────────────────────────────────────
        public bool RenewContract(int seat, ContractOffer offer)
        {
            var signed = seat == 1 ? Save.driver1 : Save.driver2;
            if (signed == null) return false;

            var (result, counter) = NegotiationEngine.Evaluate(
                signed.entry, offer, Save.teamData, Save.constructorPosition);

            OnNegotiationResult?.Invoke(result, counter);

            if (result != NegotiationResult.Accepted) return false;

            signed.currentWage         = offer.offeredWage;
            signed.contractSeasonsLeft = offer.seasons;
            signed.driverHappiness     = Mathf.Min(100f, signed.driverHappiness + 15f);
            signed.entry.contractSeasons = offer.seasons;
            signed.entry.contractStatus  = ContractStatus.Active;

            Save.finances.driverWageBill = (Save.driver1?.currentWage ?? 0f)
                                         + (Save.driver2?.currentWage ?? 0f);

            OnTransferNews?.Invoke($"📋 {signed.entry.driverName} contract renewed.");
            return true;
        }

        // ── Race result processing (updates driver stats & happiness) ─────
        public void ProcessRaceResult(int seat, RaceResult result)
        {
            var driver = seat == 1 ? Save.driver1 : Save.driver2;
            if (driver == null) return;

            driver.seasonPoints  += result.pointsEarned;
            Save.constructorPoints += result.pointsEarned;

            if (result.finishPosition == 1) driver.seasonWins++;
            if (result.finishPosition <= 3) driver.seasonPodiums++;

            // Happiness: good results keep driver happy
            float happinessDelta = result.finishPosition switch
            {
                1 =>  15f,
                2 =>  10f,
                3 =>   7f,
                <= 6 =>  4f,
                <= 10 =>  1f,
                _ => -3f
            };
            if (result.retired) happinessDelta -= 8f;

            driver.driverHappiness = Mathf.Clamp(driver.driverHappiness + happinessDelta, 0f, 100f);
            driver.racesWithoutPoints = result.pointsEarned > 0
                ? 0 : driver.racesWithoutPoints + 1;

            // Mood update
            driver.entry.mood = driver.driverHappiness switch
            {
                >= 70f => DriverMood.Happy,
                >= 45f => DriverMood.Neutral,
                >= 20f => DriverMood.Unhappy,
                _      => DriverMood.WantsOut
            };
        }

        // ── Prize money distribution (end of season) ─────────────────────
        // Mirrors F1 Concorde Agreement pay scale
        public void DistributePrizeMoney(int constructorPos)
        {
            Save.constructorPosition = constructorPos;

            float prize = constructorPos switch
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
                _  =>  20_000_000f
            };

            Save.finances.prizeMoneyEarned  = prize;
            Save.finances.budget           += prize;

            OnTransferNews?.Invoke(
                $"🏆 Constructor P{constructorPos} — Prize money: {FormatMoney(prize)}");
        }

        // ── Season advance ────────────────────────────────────────────────
        public void AdvanceSeason()
        {
            Save.season++;
            Save.constructorPoints = 0;
            Save.freeAgentSigningsThisSeason = 0;

            // Decrement contracts
            foreach (var seat in new[] { Save.driver1, Save.driver2 })
            {
                if (seat == null) continue;
                seat.contractSeasonsLeft--;
                seat.seasonPoints  = 0;
                seat.seasonWins    = 0;
                seat.seasonPodiums = 0;
                seat.entry.contractSeasons = seat.contractSeasonsLeft;
                seat.entry.contractStatus  = seat.Status;
            }

            // Age market and refresh
            DriverMarketService.AdvanceSeason(_market);
            DriverMarketService.SimulateAIBids(_market, _allTeams);

            // Deduct operating costs
            Save.finances.budget -= Save.finances.operatingCosts;
            // Wage bill paid
            Save.finances.budget -= Save.finances.driverWageBill;

            Save.windowState = TransferWindowState.PreSeason;
            OnMarketRefreshed?.Invoke(_market);
        }

        // ── Transfer window control ───────────────────────────────────────
        public void OpenPreSeasonWindow()  => Save.windowState = TransferWindowState.PreSeason;
        public void OpenMidSeasonWindow()  => Save.windowState = TransferWindowState.MidSeason;
        public void CloseTransferWindow()  => Save.windowState = TransferWindowState.Closed;

        // ── Sponsor income ────────────────────────────────────────────────
        public void AddSponsorIncome(float amount)
        {
            Save.finances.sponsorIncome += amount;
            Save.finances.budget        += amount;
        }

        // ── Helpers ───────────────────────────────────────────────────────
        static DriverStats BuildStatsFromEntry(DriverMarketEntry e) => new()
        {
            driverName     = e.driverName,
            nationality    = e.nationality,
            age            = e.age,
            speed          = e.pace,
            racecraft      = e.racecraft,
            braking        = Mathf.Clamp(e.overallRating + UnityEngine.Random.Range(-5, 5), 40, 99),
            cornering      = Mathf.Clamp(e.overallRating + UnityEngine.Random.Range(-5, 5), 40, 99),
            consistency    = e.awareness,
            wetWeather     = Mathf.Clamp(e.overallRating - 5 + UnityEngine.Random.Range(-8, 8), 40, 99),
            tireManagement = Mathf.Clamp(e.overallRating - 3 + UnityEngine.Random.Range(-8, 8), 40, 99),
        };

        static string FormatMoney(float v) =>
            v >= 1_000_000f ? $"£{v / 1_000_000f:F1}M" : $"£{v / 1_000f:F0}K";
    }
}
