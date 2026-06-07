using System;
using System.Collections.Generic;
using UnityEngine;

namespace RacingGame.Career
{
    // ═══════════════════════════════════════════════════════════════════════
    //  MEDIA EVENT SYSTEM  – press conferences, interviews, media decisions
    // ═══════════════════════════════════════════════════════════════════════
    public class MediaEventSystem : MonoBehaviour
    {
        [System.Serializable]
        public class MediaDecision
        {
            public string choiceText;
            public float teamRelationshipDelta;   // -10 to +15
            public float fanPopularityDelta;      // -5 to +20
            public float sponsorHappinessDelta;   // -15 to +10
            public float reputationDelta;         // -5 to +15
            public string outcomeText;
        }

        [System.Serializable]
        public class PressConference
        {
            public string headline;                  // "Post-Race Interview"
            public string questionText;              // "How do you feel after today?"
            public List<MediaDecision> choices = new();
        }

        public event Action<PressConference> OnPressConferenceTriggered;
        public event Action<string> OnMediaHeadline;

        public float fanPopularity = 50f;           // 0-100
        public float sponsorHappiness = 50f;        // 0-100

        public List<PressConference> postRaceConferences = new();

        void Awake()
        {
            InitializeConferences();
        }

        void InitializeConferences()
        {
            postRaceConferences.Clear();

            // Victory conference
            postRaceConferences.Add(new PressConference
            {
                headline = "Victory Press Conference",
                questionText = "You dominated today. How does it feel?",
                choices = new List<MediaDecision>
                {
                    new MediaDecision
                    {
                        choiceText = "\"The car was perfect. Great work from the team!\"",
                        teamRelationshipDelta = +15f,
                        fanPopularityDelta = +5f,
                        sponsorHappinessDelta = +5f,
                        outcomeText = "Team morale boosted. You are a true team player."
                    },
                    new MediaDecision
                    {
                        choiceText = "\"I just drove hard. I made the difference out there.\"",
                        teamRelationshipDelta = -5f,
                        fanPopularityDelta = +15f,
                        sponsorHappinessDelta = 0f,
                        outcomeText = "Fans love the confidence. Team slightly concerned about ego."
                    },
                    new MediaDecision
                    {
                        choiceText = "\"We had good pace. The competition wasn't tough today.\"",
                        teamRelationshipDelta = +2f,
                        fanPopularityDelta = -5f,
                        sponsorHappinessDelta = -3f,
                        outcomeText = "Humble approach. But media criticizes overconfidence."
                    }
                }
            });

            // Mid-field finish conference
            postRaceConferences.Add(new PressConference
            {
                headline = "Mid-Field Race Interview",
                questionText = "Struggled with pace today. What went wrong?",
                choices = new List<MediaDecision>
                {
                    new MediaDecision
                    {
                        choiceText = "\"The car setup wasn't ideal. We'll improve for next race.\"",
                        teamRelationshipDelta = +8f,
                        fanPopularityDelta = 0f,
                        sponsorHappinessDelta = +5f,
                        outcomeText = "Shows professionalism. Team respects the analysis."
                    },
                    new MediaDecision
                    {
                        choiceText = "\"I gave it everything. The car held me back.\"",
                        teamRelationshipDelta = -10f,
                        fanPopularityDelta = +8f,
                        sponsorHappinessDelta = -7f,
                        outcomeText = "Fans sympathize. Team considers replacing you."
                    },
                    new MediaDecision
                    {
                        choiceText = "\"We need better strategy. That pit stop was too slow.\"",
                        teamRelationshipDelta = -8f,
                        fanPopularityDelta = +5f,
                        sponsorHappinessDelta = -5f,
                        outcomeText = "Blames pit crew. Creates internal friction."
                    }
                }
            });

            // DNF (Did Not Finish) conference
            postRaceConferences.Add(new PressConference
            {
                headline = "Damage Control - DNF",
                questionText = "You retired from the race. How did it happen?",
                choices = new List<MediaDecision>
                {
                    new MediaDecision
                    {
                        choiceText = "\"Mechanical failure. Nothing I could do.\"",
                        teamRelationshipDelta = +10f,
                        fanPopularityDelta = +2f,
                        sponsorHappinessDelta = +3f,
                        outcomeText = "Team appreciated the honesty."
                    },
                    new MediaDecision
                    {
                        choiceText = "\"I was pushing too hard to catch the leader.\"",
                        teamRelationshipDelta = -5f,
                        fanPopularityDelta = +12f,
                        sponsorHappinessDelta = -8f,
                        outcomeText = "Fans like the aggression. Sponsors concerned about reliability."
                    },
                    new MediaDecision
                    {
                        choiceText = "\"Unfortunate. These things happen in racing.\"",
                        teamRelationshipDelta = 0f,
                        fanPopularityDelta = -3f,
                        sponsorHappinessDelta = 0f,
                        outcomeText = "Professional response. Neither helps nor hurts."
                    }
                }
            });
        }

        public void TriggerPostRaceConference(int finishPosition)
        {
            PressConference conf = finishPosition switch
            {
                1 => postRaceConferences[0],     // Victory
                2 or 3 => postRaceConferences[1], // Mid-field (use for podium too)
                _ when finishPosition > 10 => postRaceConferences[1],
                _ => null  // For 4-10, use mid-field
            };

            if (conf != null)
                OnPressConferenceTriggered?.Invoke(conf);
        }

        public void ProcessMediaChoice(MediaDecision choice, DriverProfile driver)
        {
            // Apply deltas
            driver.teamRelationship = Mathf.Clamp(
                driver.teamRelationship + choice.teamRelationshipDelta, 0f, 100f);

            fanPopularity = Mathf.Clamp(
                fanPopularity + choice.fanPopularityDelta, 0f, 100f);

            sponsorHappiness = Mathf.Clamp(
                sponsorHappiness + choice.sponsorHappinessDelta, 0f, 100f);

            // Also factor into reputation if very negative/positive
            if (choice.reputationDelta != 0)
                driver.reputation = Mathf.Clamp(
                    driver.reputation + choice.reputationDelta, 0f, 100f);

            OnMediaHeadline?.Invoke(choice.outcomeText);

            Debug.Log($"[Media] {choice.choiceText}");
            Debug.Log($"  → Team: {choice.teamRelationshipDelta:+0;-0} | Fans: {choice.fanPopularityDelta:+0;-0} | Sponsors: {choice.sponsorHappinessDelta:+0;-0}");
        }

        // ── SPONSOR DEALS ─────────────────────────────────────────────────
        public float CalculateSponsorBudgetBonus()
        {
            // Popularity and sponsor happiness affect R&D budget
            float bonus = (fanPopularity / 100f) * (sponsorHappiness / 100f) * 100f;
            return bonus;  // 0-100 additional tokens per season
        }

        // ── NARRATIVE EVENTS ──────────────────────────────────────────────
        public string GetRandomMediaHeadline()
        {
            var headlines = new[]
            {
                "\"Rising Star: New Driver Impresses in Early Season\"",
                "\"Teammate Drama: Internal Team Tensions Rise\"",
                "\"Sponsor Concerns: Driver Performance Under Scrutiny\"",
                "\"Fans Rally Behind Underdog Driver\"",
                "\"Contract Talks Begin: Driver's Future Uncertain\"",
                "\"Championship Hopes: Can This Driver Challenge for Title?\"",
            };

            return headlines[UnityEngine.Random.Range(0, headlines.Length)];
        }

        public string GetTeamMoraleStatus()
        {
            return fanPopularity > 75f ? "Very Happy" :
                   fanPopularity > 50f ? "Satisfied" :
                   fanPopularity > 25f ? "Unhappy" :
                   "Very Unhappy";
        }

        public string GetSponsorStatus()
        {
            return sponsorHappiness > 75f ? "Excellent Relations" :
                   sponsorHappiness > 50f ? "Good Relations" :
                   sponsorHappiness > 25f ? "Strained Relations" :
                   "Sponsorship at Risk";
        }
    }
}
