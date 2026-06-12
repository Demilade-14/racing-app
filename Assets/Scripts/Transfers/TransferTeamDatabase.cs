using System.Collections.Generic;
using System;
namespace RacingGame.Transfers
{
    public static class TransferTeamDatabase
    {
        // Made non-static-list to allow persistence saving later
        public static List<TransferTeamData> Teams = new List<TransferTeamData>
        {
            new TransferTeamData { teamName="Red Bull Racing", carPerformance=97, aeroRating=98, engineRating=95, chassisRating=96, budget=500000000, drivers = new List<string>{"Max Verstappen", "Sergio Perez"} },
            new TransferTeamData { teamName="McLaren", carPerformance=95, aeroRating=94, engineRating=93, chassisRating=92, budget=470000000, drivers = new List<string>{"Lando Norris", "Oscar Piastri"} },
            new TransferTeamData { teamName="Ferrari", carPerformance=94, aeroRating=93, engineRating=95, chassisRating=94, budget=490000000, drivers = new List<string>{"Charles Leclerc", "Lewis Hamilton"} },
            new TransferTeamData { teamName="Mercedes", carPerformance=93, aeroRating=92, engineRating=94, chassisRating=93, budget=480000000, drivers = new List<string>{"George Russell", "Kimi Antonelli"} },
            new TransferTeamData { teamName="Aston Martin", carPerformance=85, aeroRating=86, engineRating=87, chassisRating=84, budget=380000000, drivers = new List<string>{"Fernando Alonso", "Lance Stroll"} },
            new TransferTeamData { teamName="Alpine", carPerformance=78, aeroRating=79, engineRating=80, chassisRating=77, budget=320000000, drivers = new List<string>{"Pierre Gasly", "Esteban Ocon"} },
            new TransferTeamData { teamName="Williams", carPerformance=75, aeroRating=76, engineRating=77, chassisRating=74, budget=290000000, drivers = new List<string>{"Alex Albon", "Carlos Sainz"} },
            new TransferTeamData { teamName="Haas", carPerformance=70, aeroRating=71, engineRating=72, chassisRating=69, budget=250000000, drivers = new List<string>{"Nico Hulkenberg", "Oliver Bearman"} },
            new TransferTeamData { teamName="RB", carPerformance=72, aeroRating=73, engineRating=74, chassisRating=71, budget=260000000, drivers = new List<string>{"Yuki Tsunoda", "Isack Hadjar"} },
            new TransferTeamData { teamName="Audi", carPerformance=73, aeroRating=74, engineRating=75, chassisRating=72, budget=350000000, drivers = new List<string>{"Valtteri Bottas", "Zhou Guanyu"} },
            new TransferTeamData { teamName="Cadillac", carPerformance=65, aeroRating=66, engineRating=67, chassisRating=64, budget=280000000, drivers = new List<string>{"Colton Herta", "Andrea Kimi Antonelli"} }
        };
        public static TransferTeamData GetTeamByName(string name)
        {
            return Teams.Find(t => t.teamName.ToLower() == name.ToLower());
        }
    }
}
