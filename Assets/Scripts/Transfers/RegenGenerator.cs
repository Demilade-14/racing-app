using UnityEngine;
using RacingGame.Drivers;
using System.Collections.Generic;
namespace RacingGame.Transfers
{
    public static class RegenGenerator
    {
        static string[] firstNames = { "Alex","Lucas","Oliver","Daniel","Mateo","Leo","Noah","Oscar","Arthur","Victor","Liam","Hugo","Théo","Jules","Finn","Lukas","Kenji","Yuki","Ravi","Diego" };
        static string[] surnames = { "Silva","Costa","Garcia","Muller","Rossi","Novak","Santos","Brown","Wilson","Walker","Dubois","Leroy","Tanaka","Sato","Alonso","Vettel","Schumacher","Prost" };
        static string[] nationalities = { "Brazilian","Spanish","German","Italian","French","British","Dutch","Finnish","Australian","Canadian","American","Japanese","Mexican","Monégasque","Thai","Chinese" };
        public static DriverData GenerateDriver(int currentSeason = 1)
        {
            DriverData d = new DriverData();
            d.fullName = firstNames[Random.Range(0, firstNames.Length)] + " " + surnames[Random.Range(0, surnames.Length)];
            d.age = Random.Range(18, 21);
            d.nationality = nationalities[Random.Range(0, nationalities.Length)];
            // ERA SCALING: Drivers get slightly better every season (simulates evolution of sport)
            float eraBoost = Mathf.Clamp(currentSeason * 0.5f, 0, 15);
            d.rating = new DriverRating
            {
                pace = Mathf.Clamp(Random.Range(70, 90) + eraBoost, 0, 99),
                wetSkill = Random.Range(70, 90),
                tireManagement = Random.Range(70, 90),
                overtaking = Random.Range(70, 90),
                defending = Random.Range(70, 90),
                consistency = Random.Range(70, 90),
                aggression = Random.Range(30, 90)
            };
            // TEAM ENTRY LOGIC: Only assign to a team if they have a seat
            AssignToTeamWithVacancy(d);
            return d;
        }
        static void AssignToTeamWithVacancy(DriverData driver)
        {
            // Find all teams with open seats
            var openTeams = TransferTeamDatabase.Teams.FindAll(t => t.HasSeatAvailable());
            if (openTeams.Count > 0)
            {
                // Sort by performance (worst teams get first pick of rookies)
                openTeams.Sort((a, b) => a.carPerformance.CompareTo(b.carPerformance));
                // 80% chance to join a backmarker, 20% chance for midfield
                int targetIndex = Random.value < 0.8f ? 0 : Random.Range(0, openTeams.Count);
                var chosenTeam = openTeams[targetIndex];
                chosenTeam.AddDriver(driver.fullName);
                driver.teamName = chosenTeam.teamName;
            }
        }
    }
}
