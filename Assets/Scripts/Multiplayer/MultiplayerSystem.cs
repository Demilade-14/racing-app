using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RacingGame.Data;

namespace RacingGame.Multiplayer
{
    // ═══════════════════════════════════════════════════════════════════════
    //  ELO RATING
    // ═══════════════════════════════════════════════════════════════════════
    public static class EloRating
    {
        const int K_FACTOR = 32;

        public static (int a, int b) Calculate(int ratingA, int ratingB, float scoreA)
        {
            float expectedA = 1f / (1f + Mathf.Pow(10f, (ratingB - ratingA) / 400f));
            int newA = ratingA + Mathf.RoundToInt(K_FACTOR * (scoreA - expectedA));
            int newB = ratingB + Mathf.RoundToInt(K_FACTOR * ((1f - scoreA) - (1f - expectedA)));
            return (Mathf.Max(0, newA), Mathf.Max(0, newB));
        }

        public static int[] UpdateMultiPlayer(int[] ratings, int[] finishPositions)
        {
            int n       = ratings.Length;
            int[] delta = new int[n];

            for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                float scoreI = finishPositions[i] < finishPositions[j] ? 1f : 0f;
                var (na, nb) = Calculate(ratings[i], ratings[j], scoreI);
                delta[i] += na - ratings[i];
                delta[j] += nb - ratings[j];
            }

            int[] result = new int[n];
            for (int i = 0; i < n; i++)
                result[i] = Mathf.Max(600, ratings[i] + delta[i]);
            return result;
        }

        public static int DecayRating(int rating, int weeksInactive) =>
            Mathf.Max(800, rating - Mathf.Min(weeksInactive * 2, 100));

        public static int SeasonReset(int rating) =>
            Mathf.RoundToInt(rating * 0.80f + 1500f * 0.20f);  // Soft reset to 80% + pull toward 1500
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  MATCHMAKING
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class QueueEntry
    {
        public PlayerNetData player;
        public float         queueTime;
        public bool          ranked;
        public string        preferredCircuit;
    }

    public class MatchmakingService : MonoBehaviour
    {
        const int   WINDOW_BASE   = 200;
        const int   WINDOW_GROW   = 60;    // expand per 10 s
        const int   WINDOW_MAX    = 1000;
        const int   TARGET        = 4;
        const float TICK          = 2f;

        readonly List<QueueEntry> _queue   = new();
        readonly List<LobbyData>  _lobbies = new();

        public event Action<LobbyData> OnMatchFound;

        void Start() => StartCoroutine(MatchLoop());

        public void Enqueue(PlayerNetData player, bool ranked, string circuit = null)
        {
            if (_queue.Any(e => e.player.playerId == player.playerId)) return;
            _queue.Add(new QueueEntry
            { player = player, ranked = ranked, preferredCircuit = circuit });
        }

        public void Dequeue(string playerId) =>
            _queue.RemoveAll(e => e.player.playerId == playerId);

        IEnumerator MatchLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(TICK);
                foreach (var e in _queue) e.queueTime += TICK;
                TryMatch();
            }
        }

        void TryMatch()
        {
            var remaining = new List<QueueEntry>(_queue);
            while (remaining.Count >= 2)
            {
                var anchor = remaining[0];
                int window = Mathf.Min(WINDOW_BASE + (int)(anchor.queueTime / 10f) * WINDOW_GROW,
                                       WINDOW_MAX);

                var group = remaining
                    .Where(e => e.ranked == anchor.ranked &&
                                Mathf.Abs(e.player.skillRating - anchor.player.skillRating) <= window)
                    .Take(TARGET)
                    .ToList();

                if (group.Count < 2) { remaining.RemoveAt(0); continue; }

                string circuit = group.Select(e => e.preferredCircuit)
                                      .GroupBy(c => c)
                                      .OrderByDescending(g => g.Count())
                                      .FirstOrDefault()?.Key ?? "Monza Veloce";

                var lobby = MakeLobby(group.Select(e => e.player).ToList(), circuit);
                _lobbies.Add(lobby);
                OnMatchFound?.Invoke(lobby);
                foreach (var e in group) { _queue.Remove(e); remaining.Remove(e); }
            }
        }

        LobbyData MakeLobby(List<PlayerNetData> players, string circuit) => new()
        {
            lobbyId    = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
            hostId     = players[0].playerId,
            state      = LobbyState.Waiting,
            circuit    = circuit,
            maxPlayers = TARGET,
            raceLaps   = 10,
            players    = players
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  LOBBY CONTROLLER
    // ═══════════════════════════════════════════════════════════════════════
    public class LobbyController : MonoBehaviour
    {
        public  LobbyData Data { get; private set; }

        readonly Dictionary<string, bool> _ready = new();
        float  _countdown;
        const float COUNTDOWN_START = 30f;

        public event Action<LobbyState> OnStateChanged;
        public event Action<float>      OnCountdownTick;

        public void Init(LobbyData data)
        {
            Data = data;
            foreach (var p in data.players) _ready[p.playerId] = false;
        }

        public void SetReady(string playerId, bool ready)
        {
            if (_ready.ContainsKey(playerId)) _ready[playerId] = ready;
            if (_ready.Values.All(r => r) && Data.state == LobbyState.Waiting)
                BeginCountdown();
        }

        void BeginCountdown()
        {
            Transition(LobbyState.Loading);
            _countdown = COUNTDOWN_START;
        }

        void Update()
        {
            if (Data?.state != LobbyState.Loading) return;
            _countdown -= Time.deltaTime;
            OnCountdownTick?.Invoke(_countdown);
            if (_countdown <= 0f) Transition(LobbyState.Racing);
        }

        public void EndRace() => Transition(LobbyState.Results);

        public void AddPlayer(PlayerNetData p)
        {
            if (Data.state != LobbyState.Waiting) return;
            if (Data.players.Count >= Data.maxPlayers) return;
            Data.players.Add(p);
            _ready[p.playerId] = false;
        }

        public void RemovePlayer(string pid)
        {
            Data.players.RemoveAll(p => p.playerId == pid);
            _ready.Remove(pid);
            if (Data.players.Count == 0) Transition(LobbyState.Results);
        }

        void Transition(LobbyState s) { Data.state = s; OnStateChanged?.Invoke(s); }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  NETWORK MANAGER
    // ═══════════════════════════════════════════════════════════════════════
    public class NetworkManager : MonoBehaviour
    {
        const float SEND_RATE  = 0.05f;   // 20 Hz
        const float INTERP_LAG = 0.10f;   // 100 ms interpolation buffer

        public string LocalPlayerId;

        readonly Dictionary<string, Queue<VehicleUpdatePacket>> _buffers  = new();
        readonly Dictionary<string, VehicleState>              _interpolated = new();

        float _sendTimer;
        public RacingGame.Physics.PhysicsIntegrator LocalPhysics;

        public event Action<VehicleUpdatePacket> OnPacketReceived;

        void Update()
        {
            _sendTimer += Time.deltaTime;
            if (_sendTimer >= SEND_RATE)
            {
                _sendTimer = 0f;
                SendLocalState();
            }
            InterpolateAll();
        }

        void SendLocalState()
        {
            if (LocalPhysics == null) return;
            var s = LocalPhysics.State;

            var pkt = new VehicleUpdatePacket
            {
                playerId        = LocalPlayerId,
                position        = s.position,
                velocity        = s.velocity,
                rotation        = s.rotation,
                gear            = (byte)s.gear,
                throttle        = s.throttle,
                brake           = s.brake,
                steering        = s.steering,
                fuelLoad        = s.fuelLoad,
                drsActive       = s.drsActive,
                serverTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            for (int i = 0; i < 4; i++)
            {
                pkt.tireTemps[i] = s.tires[i].temperature;
                pkt.tireWear[i]  = s.tires[i].wearPercent;
            }

            Broadcast(pkt);
        }

        public void ReceivePacket(VehicleUpdatePacket pkt)
        {
            if (pkt.playerId == LocalPlayerId) return;
            if (!AntiCheat.Validate(pkt)) return;
            if (!_buffers.ContainsKey(pkt.playerId))
                _buffers[pkt.playerId] = new Queue<VehicleUpdatePacket>();
            _buffers[pkt.playerId].Enqueue(pkt);
        }

        void InterpolateAll()
        {
            long renderTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                            - (long)(INTERP_LAG * 1000f);

            foreach (var (id, buffer) in _buffers)
            {
                if (!_interpolated.ContainsKey(id))
                    _interpolated[id] = new VehicleState();

                while (buffer.Count > 2 && buffer.Peek().serverTimestamp < renderTime)
                    buffer.Dequeue();

                if (buffer.Count == 0) continue;
                var pkt = buffer.Peek();

                float dt = Time.deltaTime;
                var state = _interpolated[id];
                state.position = Vector3.Lerp(state.position,
                    pkt.position + pkt.velocity * INTERP_LAG, 12f * dt);
                state.rotation = Quaternion.Slerp(state.rotation, pkt.rotation, 10f * dt);
                state.speed    = pkt.velocity.magnitude * 3.6f;
            }
        }

        public VehicleState GetRemoteState(string pid) =>
            _interpolated.TryGetValue(pid, out var s) ? s : null;

        void Broadcast(VehicleUpdatePacket pkt) { /* WebSocket / Mirror send */ }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ANTI-CHEAT
    // ═══════════════════════════════════════════════════════════════════════
    public static class AntiCheat
    {
        const float MAX_SPEED     = 380f;
        const float MAX_JUMP      = 22f;    // m per 50 ms

        static readonly Dictionary<string, Vector3> _lastPos = new();
        static readonly Dictionary<string, int>     _strikes = new();

        public static bool Validate(VehicleUpdatePacket pkt)
        {
            float speedKmh = pkt.velocity.magnitude * 3.6f;
            if (speedKmh > MAX_SPEED) { Strike(pkt.playerId, "speed hack"); return false; }

            if (_lastPos.TryGetValue(pkt.playerId, out var prev))
            {
                float jump = Vector3.Distance(prev, pkt.position);
                if (jump > MAX_JUMP) { Strike(pkt.playerId, "teleport"); return false; }
            }
            _lastPos[pkt.playerId] = pkt.position;
            return true;
        }

        static void Strike(string pid, string reason)
        {
            if (!_strikes.ContainsKey(pid)) _strikes[pid] = 0;
            _strikes[pid]++;
            Debug.LogWarning($"[AntiCheat] {pid} strike {_strikes[pid]}: {reason}");
        }

        public static int GetStrikes(string pid) =>
            _strikes.TryGetValue(pid, out var s) ? s : 0;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  LEADERBOARD
    // ═══════════════════════════════════════════════════════════════════════
    [Serializable]
    public class LeaderboardEntry
    {
        public int    rank;
        public string playerId;
        public string username;
        public int    rating;
        public int    wins;
        public float  bestLapTime;
        public string region;
        public string lastActive;
    }

    public static class Leaderboard
    {
        public static List<LeaderboardEntry> Sort(List<LeaderboardEntry> entries)
        {
            entries.Sort((a, b) => b.rating.CompareTo(a.rating));
            for (int i = 0; i < entries.Count; i++) entries[i].rank = i + 1;
            return entries;
        }

        public static List<LeaderboardEntry> FilterByRegion(
            List<LeaderboardEntry> entries, string region) =>
            entries.Where(e => e.region == region).ToList();

        public static LeaderboardEntry GetEntry(List<LeaderboardEntry> entries, string pid) =>
            entries.FirstOrDefault(e => e.playerId == pid);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  RACE RESULT PROCESSOR
    // ═══════════════════════════════════════════════════════════════════════
    public static class RaceResultProcessor
    {
        public static void ApplyRatingChanges(List<PlayerNetData> players,
                                               List<RaceResult> results)
        {
            int n          = players.Count;
            int[] ratings  = players.Select(p => p.skillRating).ToArray();
            int[] positions = new int[n];

            for (int i = 0; i < n; i++)
            {
                var r = results.FirstOrDefault(x => x.playerId == players[i].playerId);
                positions[i] = r?.finishPosition ?? n + 1;
            }

            int[] newRatings = EloRating.UpdateMultiPlayer(ratings, positions);
            for (int i = 0; i < n; i++)
            {
                players[i].skillRating = newRatings[i];
                players[i].totalRaces++;
                if (positions[i] == 1) players[i].wins++;
            }
        }
    }
}
