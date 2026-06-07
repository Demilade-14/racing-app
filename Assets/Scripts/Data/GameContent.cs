using System.Collections.Generic;
using UnityEngine;
using RacingGame.Data;

namespace RacingGame.Content
{
    public static class GameContent
    {
        // ── Teams ─────────────────────────────────────────────────────────
        public static List<TeamData> Teams() => new()
        {
            new() { teamName="Apex Racing",     enginePower=95, reliability=88, aeroEfficiency=92, annualBudget=200_000_000f, primaryColor=Color.red    },
            new() { teamName="Quantum Motors",  enginePower=90, reliability=91, aeroEfficiency=89, annualBudget=180_000_000f, primaryColor=Color.blue   },
            new() { teamName="Nova Dynamics",   enginePower=85, reliability=85, aeroEfficiency=87, annualBudget=150_000_000f, primaryColor=Color.yellow  },
            new() { teamName="Vortex F1",       enginePower=80, reliability=82, aeroEfficiency=83, annualBudget=120_000_000f, primaryColor=Color.green  },
            new() { teamName="Stellar Speed",   enginePower=76, reliability=78, aeroEfficiency=80, annualBudget=100_000_000f, primaryColor=Color.cyan   },
            new() { teamName="Titan Motorsport",enginePower=72, reliability=75, aeroEfficiency=74, annualBudget=80_000_000f,  primaryColor=Color.magenta },
            new() { teamName="Eclipse Racing",  enginePower=68, reliability=72, aeroEfficiency=70, annualBudget=70_000_000f,  primaryColor=Color.white  },
            new() { teamName="Zenith Auto",     enginePower=64, reliability=70, aeroEfficiency=66, annualBudget=60_000_000f,  primaryColor=new Color(1f,0.5f,0f) },
            new() { teamName="Fusion GP",       enginePower=60, reliability=68, aeroEfficiency=62, annualBudget=50_000_000f,  primaryColor=new Color(0.5f,0f,1f) },
            new() { teamName="Orbit Racing",    enginePower=56, reliability=65, aeroEfficiency=58, annualBudget=40_000_000f,  primaryColor=new Color(0f,0.8f,0.4f) },
        };

        // ── Drivers ───────────────────────────────────────────────────────
        public static List<DriverStats> Drivers() => new()
        {
            new() { driverName="Marco Veltri",    age=28, nationality="Italian",    speed=96, braking=94, cornering=95, racecraft=93, consistency=91 },
            new() { driverName="Lena Hartmann",   age=25, nationality="German",     speed=93, braking=91, cornering=90, racecraft=88, consistency=92 },
            new() { driverName="Kieran Walsh",    age=30, nationality="Irish",      speed=89, braking=92, cornering=87, racecraft=94, consistency=90 },
            new() { driverName="Yuki Tanaka",     age=22, nationality="Japanese",   speed=91, braking=88, cornering=92, racecraft=82, consistency=85 },
            new() { driverName="Carlos Reyes",    age=27, nationality="Mexican",    speed=88, braking=87, cornering=89, racecraft=90, consistency=88 },
            new() { driverName="Priya Mehta",     age=24, nationality="Indian",     speed=86, braking=89, cornering=85, racecraft=84, consistency=87 },
            new() { driverName="Alex Novak",      age=31, nationality="Czech",      speed=84, braking=86, cornering=83, racecraft=92, consistency=93 },
            new() { driverName="Sophie Laurent",  age=23, nationality="French",     speed=87, braking=84, cornering=88, racecraft=80, consistency=83 },
            new() { driverName="Tobias Müller",   age=29, nationality="German",     speed=82, braking=85, cornering=81, racecraft=86, consistency=89 },
            new() { driverName="Jin-Ho Park",     age=26, nationality="Korean",     speed=85, braking=83, cornering=86, racecraft=81, consistency=84 },
            new() { driverName="Riku Saarinen",   age=21, nationality="Finnish",    speed=90, braking=82, cornering=91, racecraft=75, consistency=78 },
            new() { driverName="Diego Ferreira",  age=33, nationality="Brazilian",  speed=80, braking=88, cornering=79, racecraft=95, consistency=94 },
            new() { driverName="Amir Khalid",     age=28, nationality="Emirati",    speed=78, braking=80, cornering=77, racecraft=82, consistency=80 },
            new() { driverName="Isla MacLeod",    age=25, nationality="Scottish",   speed=82, braking=79, cornering=83, racecraft=78, consistency=82 },
            new() { driverName="Nico Bauer",      age=20, nationality="Austrian",   speed=88, braking=78, cornering=89, racecraft=72, consistency=74 },
            new() { driverName="Valentina Cruz",  age=27, nationality="Argentine",  speed=76, braking=81, cornering=75, racecraft=80, consistency=81 },
            new() { driverName="Tom Fletcher",    age=32, nationality="British",    speed=74, braking=83, cornering=73, racecraft=88, consistency=90 },
            new() { driverName="Kai Andersen",    age=24, nationality="Danish",     speed=79, braking=77, cornering=80, racecraft=75, consistency=77 },
            new() { driverName="Luca Moretti",    age=22, nationality="Italian",    speed=83, braking=76, cornering=84, racecraft=71, consistency=73 },
            new() { driverName="Zara Okafor",     age=26, nationality="Nigerian",   speed=77, braking=79, cornering=78, racecraft=76, consistency=79 },
        };

        // ── Circuits ──────────────────────────────────────────────────────
        public static List<CircuitData> Circuits() => new()
        {
            new() { circuitName="Silverstone Heights",  location="UK",           trackLengthKm=5.89f, baseLapTimeSeconds=88f,  totalLaps=52, drsZones=2, defaultWeather=WeatherCondition.LightRain },
            new() { circuitName="Monza Veloce",         location="Italy",        trackLengthKm=5.79f, baseLapTimeSeconds=82f,  totalLaps=53, drsZones=2, defaultWeather=WeatherCondition.Dry },
            new() { circuitName="Monte Cristal",        location="Monaco",       trackLengthKm=3.34f, baseLapTimeSeconds=74f,  totalLaps=78, drsZones=1, defaultWeather=WeatherCondition.Dry },
            new() { circuitName="Desert Storm Circuit", location="UAE",          trackLengthKm=5.55f, baseLapTimeSeconds=93f,  totalLaps=55, drsZones=3, defaultWeather=WeatherCondition.Dry },
            new() { circuitName="Sakura Ring",          location="Japan",        trackLengthKm=5.81f, baseLapTimeSeconds=91f,  totalLaps=53, drsZones=1, defaultWeather=WeatherCondition.LightRain },
            new() { circuitName="Amazonia Raceway",     location="Brazil",       trackLengthKm=4.31f, baseLapTimeSeconds=71f,  totalLaps=71, drsZones=2, defaultWeather=WeatherCondition.HeavyRain },
            new() { circuitName="Nordic Ice Park",      location="Finland",      trackLengthKm=5.20f, baseLapTimeSeconds=96f,  totalLaps=59, drsZones=1, defaultWeather=WeatherCondition.LightRain },
            new() { circuitName="Pacific Shores",       location="Australia",    trackLengthKm=5.30f, baseLapTimeSeconds=89f,  totalLaps=58, drsZones=2, defaultWeather=WeatherCondition.Dry },
            new() { circuitName="Neon City Street",     location="Singapore",    trackLengthKm=5.06f, baseLapTimeSeconds=101f, totalLaps=61, drsZones=3, defaultWeather=WeatherCondition.LightRain },
            new() { circuitName="Altiplano Circuit",    location="Mexico",       trackLengthKm=4.30f, baseLapTimeSeconds=80f,  totalLaps=71, drsZones=3, defaultWeather=WeatherCondition.Dry },
        };
    }
}
