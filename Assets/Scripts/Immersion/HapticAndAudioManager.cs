using System.Collections.Generic;
using UnityEngine;

namespace RacingGame.Immersion
{
    // ═══════════════════════════════════════════════════════════════════════
    //  HAPTIC MANAGER – device vibration feedback
    // ═══════════════════════════════════════════════════════════════════════
    public class HapticManager : MonoBehaviour
    {
        public static HapticManager Instance { get; private set; }

        public bool hapticsEnabled = true;
        public float masterHapticIntensity = 1f;  // 0-1 multiplier

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Check if device supports haptics
            #if UNITY_IOS || UNITY_ANDROID
                hapticsEnabled = SystemInfo.supportsVibration;
            #endif
        }

        // ── ABS BRAKING ───────────────────────────────────────────────────
        public void TriggerABSPulse(int pulseCount = 4)
        {
            if (!hapticsEnabled) return;

            StartCoroutine(PulsePattern(pulseCount, 0.05f, 50));
        }

        // ── CURB IMPACT ───────────────────────────────────────────────────
        public void TriggerCurbJolt(float intensity = 1f)
        {
            if (!hapticsEnabled) return;

            // Sharp, single jolt
            Vibrate(150, intensity * 100);
        }

        // ── COLLISION ─────────────────────────────────────────────────────
        public void TriggerCollisionThud(float impactForce)
        {
            if (!hapticsEnabled) return;

            float duration = Mathf.Clamp(impactForce * 50, 50, 300);
            Vibrate((int)duration, 100 * masterHapticIntensity);
        }

        // ── MARBLES / LOSS OF GRIP ──────────────────────────────────────
        public void TriggerTractionLossRumble(float duration = 0.3f)
        {
            if (!hapticsEnabled) return;

            // Continuous rumbling sensation
            Vibrate((int)(duration * 1000), 60);
        }

        // ── ENGINE OVERSTRESS ──────────────────────────────────────────
        public void TriggerEngineFailureShake()
        {
            if (!hapticsEnabled) return;

            StartCoroutine(PulsePattern(3, 0.2f, 100));
        }

        // ── WIND GUST ──────────────────────────────────────────────────
        public void TriggerWindGustJolt(float gustStrength)
        {
            if (!hapticsEnabled) return;

            float intensity = Mathf.Clamp(gustStrength / 40f, 0, 1);  // 0-40 km/h = 0-100%
            Vibrate(100, (int)(intensity * 100));
        }

        // ── TIRE SLIP ──────────────────────────────────────────────────
        public void TriggerTireSlipFeedback(float slipAngle)
        {
            if (!hapticsEnabled) return;

            // Higher slip = more intense haptic
            float intensity = Mathf.Clamp(slipAngle / 30f, 0, 1);
            Vibrate(75, (int)(intensity * 80));
        }

        // ── BASELINE VIBRATION ────────────────────────────────────────
        System.Collections.IEnumerator PulsePattern(int pulseCount, float pulseDuration, float intensity)
        {
            for (int i = 0; i < pulseCount; i++)
            {
                Vibrate((int)(pulseDuration * 1000), (int)intensity);
                yield return new WaitForSeconds(pulseDuration * 1.5f);
            }
        }

        void Vibrate(int durationMilliseconds, float intensity)
        {
            #if UNITY_ANDROID
                Handheld.Vibrate();
            #endif

            #if UNITY_IOS
                // iOS uses different approach - use default haptic
                if (intensity > 50)
                    Handheld.Vibrate();
            #endif

            Debug.Log($"[Haptic] Vibrate: {durationMilliseconds}ms @ {intensity}%");
        }

        public void SetMasterIntensity(float intensity)
        {
            masterHapticIntensity = Mathf.Clamp01(intensity);
        }

        public void ToggleHaptics(bool enabled)
        {
            hapticsEnabled = enabled && SystemInfo.supportsVibration;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  AUDIO ENGINE MANAGER – dynamic audio based on game state
    // ═══════════════════════════════════════════════════════════════════════
    public class AudioEngineManager : MonoBehaviour
    {
        [System.Serializable]
        public class EngineAudioSource
        {
            public AudioSource source;
            public float basePitch = 1f;
            public float pitchVariation = 0.5f;
        }

        public static AudioEngineManager Instance { get; private set; }

        [SerializeField] EngineAudioSource engineAudio;
        [SerializeField] AudioSource tireNoiseSource;
        [SerializeField] AudioSource windNoiseSource;
        [SerializeField] List<AudioSource> aiEngineAudio = new();

        private float _currentEngineRPM = 0;
        private float _currentSpeed = 0;
        private float _currentSlipAngle = 0;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void FixedUpdate()
        {
            UpdateEngineAudio();
            UpdateTireNoise();
            UpdateWindNoise();
        }

        // ── ENGINE AUDIO ───────────────────────────────────────────────────
        public void SetEngineRPM(float rpm)
        {
            _currentEngineRPM = rpm;
        }

        void UpdateEngineAudio()
        {
            if (engineAudio?.source == null) return;

            // RPM to pitch mapping: 0-15000 RPM = 0.5-2.0 pitch
            float rpmPitch = 0.5f + (_currentEngineRPM / 15000f) * 1.5f;
            engineAudio.source.pitch = Mathf.Clamp(rpmPitch, 0.5f, 2.5f);

            // Volume based on throttle/load
            float throttle = Input.GetAxis("Throttle");
            engineAudio.source.volume = 0.3f + throttle * 0.7f;  // 0.3-1.0
        }

        // ── TIRE NOISE ─────────────────────────────────────────────────────
        public void SetTireSlip(float slipAngle)
        {
            _currentSlipAngle = slipAngle;
        }

        void UpdateTireNoise()
        {
            if (tireNoiseSource == null) return;

            // Tire screech volume based on slip angle
            float slipIntensity = Mathf.Clamp(Mathf.Abs(_currentSlipAngle) / 30f, 0, 1);
            tireNoiseSource.volume = slipIntensity * 0.8f;

            // Pitch changes with speed and slip
            tireNoiseSource.pitch = 1f + (slipIntensity * 0.3f);

            if (slipIntensity > 0.3f)
                tireNoiseSource.pitch += Mathf.Sin(Time.time * 5f) * 0.1f;  // Warble effect
        }

        // ── WIND NOISE ─────────────────────────────────────────────────────
        public void SetSpeed(float speed)
        {
            _currentSpeed = speed;
        }

        void UpdateWindNoise()
        {
            if (windNoiseSource == null) return;

            // Wind volume based on speed (200+ km/h = loud wind)
            float windIntensity = Mathf.Clamp(_currentSpeed / 300f, 0, 1);
            windNoiseSource.volume = windIntensity * 0.5f;

            // Wind pitch increases slightly with speed
            windNoiseSource.pitch = 0.9f + windIntensity * 0.2f;
        }

        // ── AI CAR AUDIO ───────────────────────────────────────────────────
        public void UpdateAIEngineAudio(int aiIndex, float rpm, Vector3 aiPosition, Vector3 playerPosition)
        {
            if (aiIndex >= aiEngineAudio.Count || aiEngineAudio[aiIndex] == null)
                return;

            var aiSource = aiEngineAudio[aiIndex];

            // Pan left/right based on X position
            float horizontalDistance = aiPosition.x - playerPosition.x;
            aiSource.panStereo = Mathf.Clamp(horizontalDistance / 50f, -1, 1);

            // Volume based on distance
            float distance = Vector3.Distance(aiPosition, playerPosition);
            aiSource.volume = Mathf.Clamp(1f / (distance / 10f + 1), 0, 1);

            // Pitch based on AI RPM
            aiSource.pitch = 0.5f + (rpm / 15000f) * 1.5f;
        }

        // ── DOPPLER EFFECT ────────────────────────────────────────────────
        public void PlayDopplerEffect(AudioSource source, Vector3 objectPosition, Vector3 objectVelocity, Vector3 listenerPosition)
        {
            // Simplified doppler: approaching = higher pitch, receding = lower pitch
            float approachSpeed = Vector3.Dot(objectVelocity, (listenerPosition - objectPosition).normalized);
            float dopplerShift = 1f + (approachSpeed / 100f) * 0.1f;  // ±10% pitch shift

            source.pitch = Mathf.Clamp(dopplerShift, 0.8f, 1.2f);
        }

        // ── AUDIO MIXER MANAGEMENT ────────────────────────────────────────
        public void PlayEngineSound()
        {
            if (engineAudio?.source != null && !engineAudio.source.isPlaying)
                engineAudio.source.Play();
        }

        public void StopEngineSound()
        {
            if (engineAudio?.source != null)
                engineAudio.source.Stop();
        }

        public float GetEngineVolume() => engineAudio?.source?.volume ?? 0;

        public string GetAudioDebug() => 
            $"Engine: {_currentEngineRPM:F0} RPM | " +
            $"Speed: {_currentSpeed:F0} km/h | " +
            $"Slip: {_currentSlipAngle:F1}°";
    }
}
