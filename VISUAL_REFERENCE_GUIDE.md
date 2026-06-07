# F1 25 Career System – Visual Reference & Architecture Diagrams

## SYSTEM ARCHITECTURE DIAGRAM

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         F1 25 CAREER MODE SYSTEM                        │
└─────────────────────────────────────────────────────────────────────────┘

                              ┌──────────────────┐
                              │  GAME BOOTSTRAP  │
                              │  - SetupRace()   │
                              │  - OnRaceEnd()   │
                              └────────┬─────────┘
                                       │
                ┌──────────────────────┼──────────────────────┐
                │                      │                      │
                ▼                      ▼                      ▼
    ┌─────────────────────┐  ┌──────────────────┐  ┌──────────────────┐
    │  PHYSICS ENGINE     │  │  CHAMPIONSHIP    │  │  SAVE MANAGER    │
    │  - Speed calc       │  │  MANAGER         │  │  - Serialize     │
    │  - R&D multipliers  │  │  - Standings     │  │  - Persist       │
    │  - Driver skill     │  │  - Points award  │  │  - Multi-slot    │
    └─────────────────────┘  └──────────────────┘  └──────────────────┘
                │                      │                      │
                │     ┌────────────────┼────────────────┐     │
                │     │                │                │     │
                ▼     ▼                ▼                ▼     ▼
    ┌──────────────────────────────────────────────────────────────────┐
    │         CAREER INTEGRATION LAYER (Orchestrator)                  │
    │  - Season loop control                                           │
    │  - Manager coordination                                          │
    │  - Event routing                                                 │
    └──────────────────────┬───────────────────────────────────────────┘
                           │
        ┌──────────────────┼──────────────────┬──────────────────┐
        │                  │                  │                  │
        ▼                  ▼                  ▼                  ▼
   ┌─────────────┐  ┌────────────────┐ ┌──────────────┐  ┌────────────┐
   │   DRIVER    │  │  R&D MANAGER   │ │    MEDIA     │  │ CONTRACT   │
   │   CAREER    │  │                │ │    EVENT     │  │ NEGOTIATION│
   │  MANAGER    │  │ - Aerodynamics │ │   SYSTEM     │  │ ENGINE     │
   │             │  │ - PowerUnit    │ │              │  │            │
   │ - Races     │  │ - Chassis      │ │ - Press      │  │ - Scoring  │
   │ - XP        │  │ - Reliability  │ │   Conference │  │ - Counter  │
   │ - Attributes│  │                │ │ - Morale     │  │ - Market   │
   │ - Contracts │  │ - Tokens (500) │ │ - Popularity │  │   Value    │
   │ - Seasons   │  │ - Multipliers  │ │ - Sponsors   │  │            │
   │ - Calendar  │  │                │ │              │  │            │
   └──────┬──────┘  └────────┬───────┘ └────────┬─────┘  └────────────┘
          │                  │                  │
          │    Events        │                  │
          └──────────────────┼──────────────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
        ▼                    ▼                    ▼
   ┌────────────┐     ┌─────────────┐     ┌────────────┐
   │ CAREER HUB │     │  R&D GARAGE │     │  PRESS     │
   │    UI      │     │     UI      │     │ CONFERENCE │
   │ - Stats    │     │ - Upgrades  │     │    UI      │
   │ - Nav      │     │ - Budget    │     │ - Choices  │
   │ - Race Btn │     │ - Perf %    │     │ - Morale   │
   └────────────┘     └─────────────┘     └────────────┘
                              │
                              ▼
                     ┌──────────────────┐
                     │ CONTRACT NEGOTIA─│
                     │ TION UI          │
                     │ - Offers         │
                     │ - Counter        │
                     │ - Accept/Reject  │
                     └──────────────────┘
```

---

## DATA FLOW: SINGLE RACE EXECUTION

```
┌─ RACE START ─────────────────────────────────────────────────────────┐
│                                                                        │
│  1. GameBootstrap.SetupPlayerEntry()                                  │
│     └─ ChampionshipManager.RegisterPlayer(driverName, teamName)       │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
                              ▼
┌─ PHYSICS SETUP ───────────────────────────────────────────────────────┐
│                                                                        │
│  2. PhysicsEngine.CalculateMaxSpeed()                                  │
│     └─ Apply multipliers:                                             │
│        ├─ RnDManager.GetCategoryMultiplier("PowerUnit")              │
│        └─ Driver skill multiplier (OVR-based)                         │
│                                                                        │
│  3. PhysicsEngine.CalculateCorneringGrip()                            │
│     └─ Apply multipliers:                                             │
│        ├─ RnDManager.GetCategoryMultiplier("Aerodynamics")           │
│        ├─ RnDManager.GetCategoryMultiplier("Chassis")                │
│        └─ Driver cornering attribute (0-99)                           │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
                              ▼
┌─ RACE EXECUTION ──────────────────────────────────────────────────────┐
│                                                                        │
│  4. Player drives race (normal physics loop)                          │
│     └─ Input → Physics → Output (with all multipliers applied)       │
│                                                                        │
│  5. Race finishes (P2, 4 overtakes, fastest lap)                     │
│     └─ Generate RaceResult { finishPosition: 2, ... }                 │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
                              ▼
┌─ RACE RESULT PROCESSING ──────────────────────────────────────────────┐
│                                                                        │
│  6. GameBootstrap.OnRaceFinished()                                    │
│     ├─ DriverCareerManager.ProcessRaceWeekend(weekend, result)       │
│     │  ├─ Calculate XP:                                              │
│     │  │  ├─ PositionXP = 45 (P2)                                   │
│     │  │  ├─ QualifyingXP = 20 (P3 grid)                            │
│     │  │  ├─ OvertakeXP = 20 (4×5)                                  │
│     │  │  ├─ FastestLapXP = 0 (didn't get it)                       │
│     │  │  └─ TOTAL = 85 XP                                          │
│     │  │                                                              │
│     │  ├─ Award XP to attributes:                                    │
│     │  │  ├─ Consistency += 42 XP (50% of total)                    │
│     │  │  ├─ Racecraft += 28 XP (33% of total)                      │
│     │  │  ├─ Overtaking += 21 XP (25% of total)                     │
│     │  │  └─ Awareness += 17 XP (20% of total)                      │
│     │  │                                                              │
│     │  ├─ Update player stats:                                       │
│     │  │  ├─ seasonPoints += 18 (F1 points for P2)                  │
│     │  │  ├─ careerRaces++                                          │
│     │  │  └─ seasonDNFs += 0 (didn't retire)                        │
│     │  │                                                              │
│     │  ├─ Teammate dynamics:                                         │
│     │  │  └─ teamRelationship += 5 (beat teammate)                   │
│     │  │                                                              │
│     │  └─ R&D tokens:                                                │
│     │     └─ RnDManager.AwardTokens(6) [2nd place = 6 tokens]       │
│     │                                                                 │
│     ├─ Championship.ProcessRound(result)                             │
│     │  └─ Update standings (P2 now in championship)                  │
│     │                                                                 │
│     ├─ MediaEventSystem.TriggerPostRaceConference(2)                │
│     │  └─ Show press conference modal                                │
│     │                                                                 │
│     └─ SaveCareerProgress()                                          │
│        └─ JSON serialize to persistentDataPath                       │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
                              ▼
┌─ POST-RACE MEDIA ──────────────────────────────────────────────────────┐
│                                                                        │
│  7. PressConferenceUI displays:                                       │
│     Question: "Good pace today. How did you manage the pressure?"    │
│                                                                        │
│  8. Player selects:                                                   │
│     "Great team effort. Can't do it without them."                   │
│                                                                        │
│  9. MediaEventSystem.ProcessMediaChoice(choice, profile)             │
│     ├─ teamRelationship += 15 (team likes humility)                  │
│     ├─ fanPopularity += 5 (modest responses less flashy)             │
│     └─ sponsorHappiness += 5 (team player valued)                    │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
                              ▼
┌─ RETURN TO CAREER HUB ────────────────────────────────────────────────┐
│                                                                        │
│  10. CareerHubUI.RefreshUI() called via OnRaceCompleted event        │
│      ├─ Update stats display                                         │
│      ├─ Show championship standing (now P2 | 68 pts)                 │
│      ├─ Show OVR improvement (50.2 → 50.4)                           │
│      └─ Show R&D tokens (340/500)                                    │
│                                                                        │
│  11. Ready for next race                                             │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## ATTRIBUTE PROGRESSION VISUAL

```
Initial Driver (OVR = 50)
┌─────────────────────────────┐
│ Cornering     50 ███░░░░░░░ │  Awareness    50 ███░░░░░░░
│ Braking       50 ███░░░░░░░ │  Consistency  50 ███░░░░░░░
│ Overtaking    50 ███░░░░░░░ │
│ Defending     50 ███░░░░░░░ │  OVR: 50/99   (50%)
└─────────────────────────────┘

After 10 Races (XP distribution)
┌─────────────────────────────┐
│ Cornering     55 ████░░░░░░ │  Awareness    58 █████░░░░
│ Braking       56 █████░░░░░ │  Consistency  61 ██████░░░
│ Overtaking    62 ██████░░░░ │
│ Defending     54 ████░░░░░░ │  OVR: 57/99   (57%)
└─────────────────────────────┘

After 1 Season (20 Races)
┌─────────────────────────────┐
│ Cornering     72 ███████░░░ │  Awareness    78 ████████░░
│ Braking       75 ███████░░░ │  Consistency  73 ███████░░░
│ Overtaking    68 ██████░░░░ │
│ Defending     70 ███████░░░ │  OVR: 72/99   (73%)
└─────────────────────────────┘
         [Ready for F2 promotion]
```

---

## R&D UPGRADE PROGRESSION

```
Season 1 (500 tokens budget)

Aerodynamics
┌──────────────────────────────┐
│ Level 1 ──── 100 tokens ──→ Level 2  │  +1.5%  (1.015x)
│ Remaining budget: 400 tokens          │
└──────────────────────────────┘
    [UPGRADE]

Aerodynamics
┌──────────────────────────────┐
│ Level 2 ──── 200 tokens ──→ Level 3  │  +3.0%  (1.030x)
│ Remaining budget: 200 tokens          │
└──────────────────────────────┘

PowerUnit
┌──────────────────────────────┐
│ Level 1 ──── 100 tokens ──→ Level 2  │  +1.5%  (1.015x)
│ Remaining budget: 100 tokens          │
└──────────────────────────────┘

Chassis                          Reliability
┌──────────────┐                 ┌──────────────┐
│ Level 1      │                 │ Level 1      │
│ (Not upgraded)                 │ (Not upgraded)
└──────────────┘                 └──────────────┘

Overall Car Performance Multiplier:
= 1.030 (Aero L3) × 1.015 (PowerUnit L2) × 1.0 (Chassis) × 1.0 (Reliability)
= 1.0461 = +4.61% overall pace improvement
```

---

## SEASON PROGRESSION TIMELINE

```
SEASON 1 CALENDAR (24 races)

Week 1-2:  Bahrain      P1 ┐
           Saudi Arabia P2 │ Strong start
           Australia    P1 ┤ Total: 68 pts, 2W
           Japan        P3 ┘

Week 3-4:  Monaco       P4 ┐
           Canada       P2 │ Mid-season slump
           Spain        P5 ┤ Total: +30 pts
           Hungary      P3 ┘

Week 5-6:  Silverstone  P1 ┐
           Austria      P2 │ Recovery mode
           Italy        P3 ┤ Total: +55 pts
           Netherlands  P2 ┘

Week 7-8:  Singapore    P4 ┐
           Japan        P5 │ Tire struggles
           Qatar        P6 ┤ Total: +15 pts
           Abu Dhabi    P2 ┘

                        ───────────────────
                        Final: P2 | 380 pts
                        vs P1: 405 pts
                        ───────────────────
                                  │
                        ┌─────────▼──────────┐
                        │  PROMOTION? NO     │
                        │  (need P1-3, got P2)
                        │  STAY IN SERIES    │
                        │  (qualify for F2!)
                        └────────────────────┘

CONTRACT OFFERS ARRIVE:
┌─────────────────────────────────────────┐
│ Ferrari:  €22M/year, 2yrs, #1 status   │ ← Top offer
│ Red Bull: €20M/year, 2yrs, #2 status   │
│ McLaren:  €18M/year, 1yr, #1 status    │
└─────────────────────────────────────────┘
           ▼
    Player selects Ferrari
           ▼
    SEASON 2 BEGINS with new team!
```

---

## PRESS CONFERENCE DECISION TREE

```
┌─ VICTORY (P1) ──────────────────────────────────────────┐
│                                                           │
│  Q: "Dominant performance. How does it feel?"           │
│  │                                                       │
│  ├─ "Great team effort!"                                │
│  │  ├─ Team: +15 ✓  Fan: +5   Sponsor: +5             │
│  │  └─ Outcome: Team morale boosted                    │
│  │                                                       │
│  ├─ "I drove better than everyone else today."         │
│  │  ├─ Team: -5 ✗   Fan: +15  Sponsor: 0              │
│  │  └─ Outcome: Ego concerns in garage                 │
│  │                                                       │
│  └─ "Competition wasn't very tough."                   │
│     ├─ Team: +2    Fan: -5   Sponsor: -3              │
│     └─ Outcome: Media criticism for arrogance         │
│                                                           │
└──────────────────────────────────────────────────────────┘

┌─ MID-FIELD (P6) ────────────────────────────────────────┐
│                                                           │
│  Q: "Difficult race. What went wrong?"                  │
│  │                                                       │
│  ├─ "Car setup wasn't ideal."                           │
│  │  ├─ Team: +8 ✓   Fan: 0   Sponsor: +5             │
│  │  └─ Outcome: Professional analysis appreciated      │
│  │                                                       │
│  ├─ "I gave it everything; car held me back."          │
│  │  ├─ Team: -10 ✗  Fan: +8  Sponsor: -7             │
│  │  └─ Outcome: Team considers reserve driver         │
│  │                                                       │
│  └─ "Pit stop cost us the position."                   │
│     ├─ Team: -8 ✗   Fan: +5   Sponsor: -5            │
│     └─ Outcome: Friction with pit crew                │
│                                                           │
└──────────────────────────────────────────────────────────┘

CUMULATIVE EFFECT (Season-long):
Fan Popularity:    45 → 65   → able to attract sponsors
Sponsor Happiness: 50 → 71   → unlocks 40 token bonus
Team Relationship: 50 → 82   → preferred garage priority
```

---

## CONTRACT NEGOTIATION SCORING

```
Mercedes Offer Evaluation:

Salary Factor
├─ €25M offered vs. €18M current
├─ Raise: +39%  ──────────────── +40 pts ✓
└─ Subtotal: 40 pts

Team Strength
├─ Mercedes = Top team
├─ Competitiveness ────────────── +30 pts ✓
└─ Subtotal: 70 pts

Number 1 Status
├─ Offered: YES
├─ Player OVR: 78 (> 75 threshold)
├─ Elite driver perk ────────────── +25 pts ✓
└─ Subtotal: 95 pts

Contract Length
├─ Mercedes offers: 2 years
├─ Player prefers: 2 years (flexible at OVR78)
├─ Perfect match ────────────── +15 pts ✓
└─ Subtotal: 110 pts

Series Tier
├─ Offer: Formula 1
├─ Career goal ────────────── +20 pts ✓
└─ Subtotal: 130 pts

Random Factor
├─ Personality variance ────────────── +8 pts
└─ FINAL SCORE: 138 pts

Decision: 138 > 70 ──→ ✅ ACCEPT IMMEDIATELY
          (Wait time: 0s)
```

---

## SAVE STRUCTURE (Simplified View)

```
racing/SaveSlots/slots.json
│
├─ selectedSlotIndex: 0
│
└─ slots: [
    {
      "slotId": "driver_2026_lewis_001",
      "slotName": "Lewis Hamilton F1 2026",
      "slotType": "Driver",
      "savedAt": "2026-06-07T14:32:00Z",
      │
      └─ career: {
          "season": 1,
          "currentRound": 5,
          │
          ├─ profile: {
          │  "driverName": "Lewis",
          │  "tier": "Formula1",
          │  "attributes": {
          │    "cornering": 82,
          │    "braking": 85,
          │    "overtaking": 78,
          │    ...
          │  },
          │  "seasonPoints": 68,
          │  "careerRaces": 45
          │}
          │
          ├─ calendar: [24 races],
          │
          └─ media: {
             "fanPopularity": 65,
             "sponsorHappiness": 71
           }
       }
    }
  ]
```

---

## QUICK REFERENCE: XP FORMULA

```
Total XP per Race = Position XP + Qualifying XP + Lap XP + Overtake XP

Position XP:
  1st:  50 XP  │  2nd:  45 XP  │  3rd:  40 XP
  4-6:  30 XP  │  7-10: 20 XP  │  DNF:  0 XP

Qualifying XP:
  P1-3:  20 XP │  P4-6: 15 XP │  P7-10: 10 XP

Fastest Lap:
  +15 XP (if finish ≤ P10)

Overtakes:
  +5 XP per overtake (max +50 XP total)

Example P2 Finish with 4 Overtakes:
  45 (P2) + 20 (quali) + 0 (FL) + 20 (4×5 overtakes) = 85 XP
```

---

## MANAGER STARTUP SEQUENCE

```
Game Start
│
├─ 1. GameBootstrap.Start()
│  └─ Calls OnRaceFinished() setup
│
├─ 2. DriverCareerManager.Awake()
│  ├─ Sets Instance singleton
│  └─ Awaits Start()
│
├─ 3. DriverCareerManager.Start()
│  ├─ Finds ChampionshipManager
│  ├─ Finds RnDManager
│  ├─ Finds MediaEventSystem
│  └─ Ready for career operations
│
├─ 4. RnDManager.Awake()
│  ├─ Initializes 4 categories
│  └─ Sets 500 token budget
│
├─ 5. MediaEventSystem.Awake()
│  ├─ Loads press conference templates
│  └─ Sets morale baseline (50, 50)
│
├─ 6. CareerIntegrationLayer.Start()
│  ├─ Subscribes to career events
│  └─ Ready to coordinate season loop
│
└─ 7. Ready for gameplay
   └─ StartNewCareer() can be called
```

---

## PERFORMANCE PROFILE

```
Operation                              Time     Complexity
────────────────────────────────────────────────────────────
Calculate race XP                     <1ms      O(1)
Award XP to attributes                <1ms      O(1)
Update season stats                   <1ms      O(1)
Get R&D overall multiplier            <1ms      O(4)
Process press conference choice       <1ms      O(1)
Evaluate contract offer              <1ms      O(1)
Serialize career to JSON             <5ms      O(n) where n=races
Deserialize career from JSON         <5ms      O(n)
────────────────────────────────────────────────────────────
Total per-race cycle                 ~15ms     Mobile-friendly ✓

Save file size:
  - New career:        ~5 KB
  - Mid-season:       ~15 KB
  - End of season:    ~25 KB
  - Max with history: ~50 KB

All operations run in < 16ms frame time
Mobile-optimized for 60 FPS gameplay
```

---

*End of Visual Reference Guide*
