using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RacingGame.Data;
using RacingGame.Physics;

namespace RacingGame.Audio
{
    // ═══════════════════════════════════════════════════════════════════════
    //  AUDIO MANAGER  –  2026 Extension
    //  Adds: Audi engine sound profile, Cadillac engine sound profile,
    //        ERS harvest/deploy audio, OvertakeMode audio,
    //        multi-manufacturer engine switcher, VSC siren,
    //        Madrid crowd atmosphere, night race ambience layer.
    //  All original methods retained unchanged.
    // ═══════════════════════════════════════════════════════════════════════

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        // ── Original clips ────────────────────────────────────────────────
        [Header("Engine Clips — Generic")]
        public AudioClip engineLoop;
        public AudioClip engineRevClip;

        [Header("Tire Clips")]
        public AudioClip tireScreechClip;
        public AudioClip tirePopClip;

        [Header("Environment")]
        public AudioClip rainAmbienceClip;
        public AudioClip crowdAmbienceClip;

        [Header("Events")]
        public AudioClip collisionClipLight;
        public AudioClip collisionClipHeavy;
        public AudioClip pitConfirmedClip;
        public AudioClip drsActivatedClip;
        public AudioClip overtakeClip;
        public AudioClip safetyCarClip;

        [Header("Radio Messages")]
        public AudioClip[] boxBoxMessages;
        public AudioClip[] overtakeMessages;
        public AudioClip[] damageMessages;

        [Header("Music")]
        public AudioClip mainMenuMusic;
        public AudioClip podiumMusic;

        // ── 2026 new clips ────────────────────────────────────────────────
        [Header("— 2026: Manufacturer Engine Loops —")]
        public AudioClip audiEngineLoop;          // Audi 1.6L V6 turbo-hybrid: high-pitched whine
        public AudioClip cadillacEngineLoop;      // Cadillac V6: deeper American growl
        public AudioClip mercedesEngineLoop;      // baseline reference
        public AudioClip ferrariEngineLoop;
        public AudioClip hondaEngineLoop;
        public AudioClip renaultEngineLoop;

        [Header("— 2026: ERS Audio —")]
        public AudioClip ersHarvestWhine;         // electric wine on braking
        public AudioClip ersDeployHum;            // MGU-K deployment hum
        public AudioClip ersOvertakeActivate;     // activation thud + spool
        public AudioClip ersOvertakeLoop;         // sustained overtake mode tone
        public AudioClip ersOvertakeDeactivate;   // wind-down sound

        [Header("— 2026: VSC / SC —")]
        public AudioClip vscDeployedClip;
        public AudioClip vscEndingClip;

        [Header("— 2026: Madrid Atmosphere —")]
        public AudioClip madridCrowdLoop;         // distinct Spanish crowd atmosphere
        public AudioClip madridAnnouncer;         // intro fanfare / PA sound

        [Header("— 2026: Night Race —")]
        public AudioClip nightAmbienceLayer;      // crickets + distant crowd
        public AudioClip nightCountdownClip;

        [Header("— 2026: Tyre Cliff —")]
        public AudioClip tyreGrainClip;           // graining audio over tyre cliff

        // ── Settings ──────────────────────────────────────────────────────
        [Header("Settings")]
        [Range(0f, 1f)] public float masterVolume  = 1f;
        [Range(0f, 1f)] public float engineVolume  = 0.8f;
        [Range(0f, 1f)] public float effectsVolume = 0.7f;
        [Range(0f, 1f)] public float musicVolume   = 0.4f;
        [Range(0f, 1f)] public float radioVolume   = 0.9f;

        // ── Audio sources ─────────────────────────────────────────────────
        AudioSource _engineSource;
        AudioSource _tireSource;
        AudioSource _rainSource;
        AudioSource _crowdSource;
        AudioSource _radioSource;
        AudioSource _musicSource;

        // 2026 new sources
        AudioSource _ersHarvestSource;
        AudioSource _ersDeploySource;
        AudioSource _overtakeLoopSource;
        AudioSource _nightAmbienceSource;
        AudioSource _madridCrowdSource;
        AudioSource _tyreGrainSource;

        PhysicsIntegrator _physics;

        // ── Engine profile ────────────────────────────────────────────────
        EngineManufacturer _currentManufacturer = EngineManufacturer.Mercedes;
        AudioClip          _activeEngineLoop;

        // ── Engine pitch constants ────────────────────────────────────────
        const float RPM_MIN      = 5000f;
        const float RPM_MAX      = 15000f;
        const float PITCH_MIN    = 0.60f;
        const float PITCH_MAX    = 2.20f;
        const float PITCH_LIMITER = 2.10f;

        // 2026: manufacturer-specific pitch ranges
        // Audi: higher baseline pitch (electric assist dominant)
        // Cadillac: lower, richer pitch (V6 American character)
        static readonly Dictionary<EngineManufacturer, Vector2> PITCH_RANGES = new()
        {
            { EngineManufacturer.Mercedes,  new Vector2(0.60f, 2.20f) },
            { EngineManufacturer.Ferrari,   new Vector2(0.65f, 2.35f) },  // Ferrari revs higher
            { EngineManufacturer.Honda,     new Vector2(0.58f, 2.25f) },
            { EngineManufacturer.Renault,   new Vector2(0.62f, 2.15f) },
            { EngineManufacturer.Audi,      new Vector2(0.70f, 2.50f) },  // higher-pitched whine
            { EngineManufacturer.Cadillac,  new Vector2(0.50f, 1.95f) },  // deeper growl
        };

        // 2026: Audi has a distinct electric whine layered on top
        bool   _audiWhineActive;
        float  _audiWhineFreq = 440f;  // Hz, rises with RPM

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance  = this;
            DontDestroyOnLoad(gameObject);

            _engineSource      = CreateSource("Engine",       engineVolume,  true);
            _tireSource        = CreateSource("Tires",        effectsVolume, true);
            _rainSource        = CreateSource("Rain",         effectsVolume, true);
            _crowdSource       = CreateSource("Crowd",        effectsVolume, true);
            _radioSource       = CreateSource("Radio",        radioVolume,   false);
            _musicSource       = CreateSource("Music",        musicVolume,   true);

            // 2026 sources
            _ersHarvestSource  = CreateSource("ERSHarvest",   effectsVolume * 0.5f, true);
            _ersDeploySource   = CreateSource("ERSDeploy",    effectsVolume * 0.6f, true);
            _overtakeLoopSource = CreateSource("OvertakeLoop", effectsVolume * 0.8f, true);
            _nightAmbienceSource = CreateSource("NightAmbience", effectsVolume * 0.3f, true);
            _madridCrowdSource = CreateSource("MadridCrowd",  effectsVolume * 0.4f, true);
            _tyreGrainSource   = CreateSource("TyreGrain",    effectsVolume * 0.35f, true);
        }

        void Start()
        {
            PlayAmbience();
            _activeEngineLoop = ResolveEngineLoop(_currentManufacturer);
        }

        void Update()
        {
            if (_physics == null)
            {
                _physics = FindObjectOfType<PhysicsIntegrator>();
                return;
            }

            UpdateEngine();
            UpdateTireSounds();
            UpdateERSAudio();
            UpdateTyreGrainAudio();
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: ENGINE MANUFACTURER SYSTEM
        // ═════════════════════════════════════════════════════════════════

        /// <summary>
        /// Call when player joins a team to switch to the correct engine sound.
        /// </summary>
        public void SetEngineManufacturer(EngineManufacturer manufacturer)
        {
            if (_currentManufacturer == manufacturer) return;
            _currentManufacturer = manufacturer;
            _activeEngineLoop    = ResolveEngineLoop(manufacturer);

            if (_engineSource != null)
            {
                _engineSource.clip = _activeEngineLoop;
                _engineSource.Play();
            }

            // Audi: start electric whine layer
            _audiWhineActive = manufacturer == EngineManufacturer.Audi;

            Debug.Log($"[Audio] Engine manufacturer set: {manufacturer}");
        }

        /// <summary>Convenience: set manufacturer from team name string.</summary>
        public void SetEngineManufacturerFromTeam(string teamName)
        {
            var mfr = teamName switch
            {
                "Ferrari"           => EngineManufacturer.Ferrari,
                "Audi"              => EngineManufacturer.Audi,
                "Cadillac"          => EngineManufacturer.Cadillac,
                "Red Bull Racing"   => EngineManufacturer.Honda,
                "RB"                => EngineManufacturer.Honda,
                "Alpine"            => EngineManufacturer.Renault,
                _                   => EngineManufacturer.Mercedes
            };
            SetEngineManufacturer(mfr);
        }

        AudioClip ResolveEngineLoop(EngineManufacturer mfr) => mfr switch
        {
            EngineManufacturer.Audi     => audiEngineLoop     ?? engineLoop,
            EngineManufacturer.Cadillac => cadillacEngineLoop ?? engineLoop,
            EngineManufacturer.Ferrari  => ferrariEngineLoop  ?? engineLoop,
            EngineManufacturer.Honda    => hondaEngineLoop    ?? engineLoop,
            EngineManufacturer.Renault  => renaultEngineLoop  ?? engineLoop,
            _                           => mercedesEngineLoop ?? engineLoop
        };

        void UpdateEngine()
        {
            if (_engineSource == null || _activeEngineLoop == null) return;

            if (!_engineSource.isPlaying)
            {
                _engineSource.clip = _activeEngineLoop;
                _engineSource.Play();
            }

            var s = _physics.State;

            // Manufacturer-specific pitch range
            var pitchRange = PITCH_RANGES.TryGetValue(_currentManufacturer, out var pr)
                ? pr : new Vector2(PITCH_MIN, PITCH_MAX);

            float t     = Mathf.InverseLerp(RPM_MIN, RPM_MAX, s.rpm);
            float pitch = Mathf.Lerp(pitchRange.x, pitchRange.y, t);

            if (s.rpm >= RPM_MAX * 0.99f)
                pitch = Mathf.Lerp(pitch, PITCH_LIMITER, 10f * Time.deltaTime);

            _engineSource.pitch  = Mathf.Lerp(_engineSource.pitch, pitch, 8f * Time.deltaTime);
            _engineSource.volume = engineVolume * masterVolume
                                 * Mathf.Lerp(0.4f, 1.0f, s.throttle + 0.1f);

            // ── Audi electric whine layer ─────────────────────────────────
            // In a full implementation this would drive a procedural synthesiser.
            // Here we shift the pitch of the ERS deploy source to simulate it.
            if (_audiWhineActive && _ersDeploySource != null && ersDeployHum != null)
            {
                float whineT = Mathf.InverseLerp(RPM_MIN, RPM_MAX, s.rpm);
                _ersDeploySource.pitch = Mathf.Lerp(0.8f, 2.8f, whineT);
                _ersDeploySource.volume = 0.15f * masterVolume * engineVolume;

                if (!_ersDeploySource.isPlaying)
                {
                    _ersDeploySource.clip = ersDeployHum;
                    _ersDeploySource.Play();
                }
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: ERS AUDIO
        // ═════════════════════════════════════════════════════════════════

        void UpdateERSAudio()
        {
            if (_physics == null) return;
            var s = _physics.State;

            // ── Harvest whine (braking) ───────────────────────────────────
            if (_ersHarvestSource != null && ersHarvestWhine != null)
            {
                bool harvesting = s.brake > 0.35f && s.speedMs > 30f;
                float harvestVol = harvesting
                    ? s.brake * 0.6f * effectsVolume * masterVolume
                    : 0f;

                if (harvesting && !_ersHarvestSource.isPlaying)
                {
                    _ersHarvestSource.clip = ersHarvestWhine;
                    _ersHarvestSource.Play();
                }
                _ersHarvestSource.volume = Mathf.Lerp(
                    _ersHarvestSource.volume, harvestVol, 8f * Time.deltaTime);

                // Pitch rises as speed drops (electric motor slowing)
                _ersHarvestSource.pitch = Mathf.Lerp(1.8f, 0.9f,
                    Mathf.Clamp01(s.speedMs / 80f));
            }

            // ── Deploy hum (not Audi — handled separately above) ─────────
            if (!_audiWhineActive && _ersDeploySource != null && ersDeployHum != null)
            {
                float deployVol = s.ersBoostForce > 100f
                    ? Mathf.Clamp01(s.ersBoostForce / 8000f) * 0.5f * effectsVolume * masterVolume
                    : 0f;

                if (deployVol > 0.05f && !_ersDeploySource.isPlaying)
                {
                    _ersDeploySource.clip = ersDeployHum;
                    _ersDeploySource.Play();
                }
                _ersDeploySource.volume = Mathf.Lerp(
                    _ersDeploySource.volume, deployVol, 10f * Time.deltaTime);
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: OVERTAKE MODE AUDIO
        // ═════════════════════════════════════════════════════════════════

        public void PlayOvertakeModeActivated()
        {
            PlayOneShot(ersOvertakeActivate, effectsVolume * 1.2f);

            if (_overtakeLoopSource != null && ersOvertakeLoop != null)
            {
                _overtakeLoopSource.clip   = ersOvertakeLoop;
                _overtakeLoopSource.volume = effectsVolume * 0.8f * masterVolume;
                _overtakeLoopSource.Play();
            }
        }

        public void PlayOvertakeModeDeactivated()
        {
            _overtakeLoopSource?.Stop();
            PlayOneShot(ersOvertakeDeactivate, effectsVolume);
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: TYRE GRAIN / CLIFF AUDIO
        // ═════════════════════════════════════════════════════════════════

        public void SetTyreCliffAudio(bool active)
        {
            if (_tyreGrainSource == null || tyreGrainClip == null) return;

            if (active)
            {
                if (!_tyreGrainSource.isPlaying)
                {
                    _tyreGrainSource.clip   = tyreGrainClip;
                    _tyreGrainSource.volume = effectsVolume * 0.35f * masterVolume;
                    _tyreGrainSource.Play();
                }
            }
            else
            {
                _tyreGrainSource.volume = Mathf.MoveTowards(
                    _tyreGrainSource.volume, 0f, 1.5f * Time.deltaTime);
                if (_tyreGrainSource.volume <= 0.01f) _tyreGrainSource.Stop();
            }
        }

        void UpdateTyreGrainAudio()
        {
            // Driven externally by RaceDirector tyre cliff events
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: VSC AUDIO
        // ═════════════════════════════════════════════════════════════════

        public void PlayVSCDeployed()  => PlayOneShot(vscDeployedClip, effectsVolume);
        public void PlayVSCEnding()    => PlayOneShot(vscEndingClip,   effectsVolume);

        // ═════════════════════════════════════════════════════════════════
        //  2026: MADRID ATMOSPHERE
        // ═════════════════════════════════════════════════════════════════

        public void SetMadridAtmosphere(bool active)
        {
            if (_madridCrowdSource == null) return;

            if (active && madridCrowdLoop != null)
            {
                _madridCrowdSource.clip   = madridCrowdLoop;
                _madridCrowdSource.volume = effectsVolume * 0.4f * masterVolume;
                _madridCrowdSource.Play();

                // Stop standard crowd to avoid overlap
                _crowdSource?.Stop();

                PlayOneShot(madridAnnouncer, effectsVolume);
            }
            else
            {
                _madridCrowdSource.Stop();
                // Restore standard crowd
                PlayAmbience();
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  2026: NIGHT RACE AMBIENCE
        // ═════════════════════════════════════════════════════════════════

        public void SetNightAmbience(bool active)
        {
            if (_nightAmbienceSource == null) return;

            if (active && nightAmbienceLayer != null)
            {
                _nightAmbienceSource.clip   = nightAmbienceLayer;
                _nightAmbienceSource.volume = effectsVolume * 0.3f * masterVolume;
                _nightAmbienceSource.Play();
            }
            else
            {
                StartCoroutine(FadeOut(_nightAmbienceSource, 2f));
            }
        }

        public void PlayNightCountdown() => PlayOneShot(nightCountdownClip, effectsVolume);

        // ═════════════════════════════════════════════════════════════════
        //  ORIGINAL METHODS  (unchanged)
        // ═════════════════════════════════════════════════════════════════

        void UpdateTireSounds()
        {
            if (_tireSource == null || tireScreechClip == null) return;
            var s = _physics.State;
            float screech = (Mathf.Abs(s.steering) * s.speedMs / 80f) + s.brake * 0.5f;
            screech = Mathf.Clamp01(screech);

            if (screech > 0.15f)
            {
                if (!_tireSource.isPlaying) { _tireSource.clip = tireScreechClip; _tireSource.Play(); }
                _tireSource.volume = screech * effectsVolume * masterVolume;
            }
            else
            {
                _tireSource.volume = Mathf.MoveTowards(_tireSource.volume, 0f, 2f * Time.deltaTime);
            }
        }

        void PlayAmbience()
        {
            if (crowdAmbienceClip == null || _crowdSource == null) return;
            _crowdSource.clip   = crowdAmbienceClip;
            _crowdSource.volume = effectsVolume * masterVolume * 0.3f;
            _crowdSource.Play();
        }

        public void SetRainIntensity(float intensity)
        {
            if (rainAmbienceClip == null || _rainSource == null) return;
            if (intensity > 0.05f && !_rainSource.isPlaying)
            { _rainSource.clip = rainAmbienceClip; _rainSource.Play(); }
            _rainSource.volume = intensity * effectsVolume * masterVolume;
        }

        public void PlayCollision(float force)
        {
            if (force < 5f) return;
            PlayOneShot(force > 25f ? collisionClipHeavy : collisionClipLight, effectsVolume);
        }

        public void PlayDRSActivated()  => PlayOneShot(drsActivatedClip, effectsVolume);
        public void PlayPitConfirmed()  => PlayOneShot(pitConfirmedClip, effectsVolume);
        public void PlayOvertake()      => PlayOneShot(overtakeClip, effectsVolume);
        public void PlaySafetyCar()     => PlayOneShot(safetyCarClip, effectsVolume);
        public void PlayTirePop()       => PlayOneShot(tirePopClip, effectsVolume);

        public void PlayRadio(AudioClip clip) => PlayOneShot(clip, radioVolume, _radioSource);
        public void PlayBoxBoxRadio()
        {
            if (boxBoxMessages != null && boxBoxMessages.Length > 0)
                PlayRadio(boxBoxMessages[Random.Range(0, boxBoxMessages.Length)]);
        }

        public void PlayMusic(AudioClip clip)
        {
            if (_musicSource == null || clip == null) return;
            _musicSource.clip   = clip;
            _musicSource.volume = musicVolume * masterVolume;
            _musicSource.Play();
        }

        public void FadeOutMusic(float duration) => StartCoroutine(FadeOut(_musicSource, duration));

        public void SetMasterVolume(float v)  { masterVolume = Mathf.Clamp01(v); AudioListener.volume = masterVolume; }
        public void SetEngineVolume(float v)  => engineVolume  = Mathf.Clamp01(v);
        public void SetEffectsVolume(float v) => effectsVolume = Mathf.Clamp01(v);
        public void SetMusicVolume(float v)   => musicVolume   = Mathf.Clamp01(v);

        AudioSource CreateSource(string label, float vol, bool loop)
        {
            var go  = new GameObject($"AudioSource_{label}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.volume       = vol;
            src.loop         = loop;
            src.spatialBlend = 0f;
            return src;
        }

        void PlayOneShot(AudioClip clip, float volume, AudioSource source = null)
        {
            if (clip == null) return;
            (source ?? _radioSource)?.PlayOneShot(clip, volume * masterVolume);
        }

        IEnumerator FadeOut(AudioSource source, float duration)
        {
            if (source == null) yield break;
            float start = source.volume;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                source.volume = Mathf.Lerp(start, 0f, t / duration);
                yield return null;
            }
            source.Stop();
            source.volume = start;
        }
    }
}