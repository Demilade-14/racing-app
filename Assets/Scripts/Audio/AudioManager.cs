using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RacingGame.Data;
using RacingGame.Physics;

namespace RacingGame.Audio
{
    // ═══════════════════════════════════════════════════════════════════════
    //  AUDIO MANAGER
    // ═══════════════════════════════════════════════════════════════════════
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Engine Clips")]
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

        [Header("Settings")]
        [Range(0f, 1f)] public float masterVolume  = 1f;
        [Range(0f, 1f)] public float engineVolume  = 0.8f;
        [Range(0f, 1f)] public float effectsVolume = 0.7f;
        [Range(0f, 1f)] public float musicVolume   = 0.4f;
        [Range(0f, 1f)] public float radioVolume   = 0.9f;

        // Engine audio sources
        AudioSource _engineSource;
        AudioSource _tireSource;
        AudioSource _rainSource;
        AudioSource _crowdSource;
        AudioSource _radioSource;
        AudioSource _musicSource;

        PhysicsIntegrator _physics;

        // Engine pitch curve (RPM → pitch)
        const float RPM_MIN    = 5000f;
        const float RPM_MAX    = 15000f;
        const float PITCH_MIN  = 0.60f;
        const float PITCH_MAX  = 2.20f;
        const float PITCH_LIMITER = 2.10f;  // cuts at limiter

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance  = this;
            DontDestroyOnLoad(gameObject);

            _engineSource = CreateSource("Engine",  engineVolume,  true);
            _tireSource   = CreateSource("Tires",   effectsVolume, true);
            _rainSource   = CreateSource("Rain",    effectsVolume, true);
            _crowdSource  = CreateSource("Crowd",   effectsVolume, true);
            _radioSource  = CreateSource("Radio",   radioVolume,   false);
            _musicSource  = CreateSource("Music",   musicVolume,   true);
        }

        void Start()
        {
            PlayAmbience();
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
        }

        void UpdateEngine()
        {
            if (_engineSource == null || engineLoop == null) return;

            if (!_engineSource.isPlaying)
            {
                _engineSource.clip = engineLoop;
                _engineSource.Play();
            }

            var s = _physics.State;
            float t     = Mathf.InverseLerp(RPM_MIN, RPM_MAX, s.rpm);
            float pitch = Mathf.Lerp(PITCH_MIN, PITCH_MAX, t);

            // Rev limiter chop
            if (s.rpm >= RPM_MAX * 0.99f) pitch = Mathf.Lerp(pitch, PITCH_LIMITER, 10f * Time.deltaTime);

            _engineSource.pitch  = Mathf.Lerp(_engineSource.pitch, pitch, 8f * Time.deltaTime);
            _engineSource.volume = engineVolume * masterVolume
                                 * Mathf.Lerp(0.4f, 1.0f, s.throttle + 0.1f);
        }

        void UpdateTireSounds()
        {
            if (_tireSource == null || tireScreechClip == null) return;

            var s = _physics.State;
            float screech = (Mathf.Abs(s.steering) * s.speedMs / 80f)
                          + s.brake * 0.5f;
            screech = Mathf.Clamp01(screech);

            if (screech > 0.15f)
            {
                if (!_tireSource.isPlaying)
                {
                    _tireSource.clip = tireScreechClip;
                    _tireSource.Play();
                }
                _tireSource.volume = screech * effectsVolume * masterVolume;
            }
            else
            {
                _tireSource.volume = Mathf.MoveTowards(_tireSource.volume, 0f, 2f * Time.deltaTime);
            }
        }

        void PlayAmbience()
        {
            if (crowdAmbienceClip != null)
            {
                _crowdSource.clip   = crowdAmbienceClip;
                _crowdSource.volume = effectsVolume * masterVolume * 0.3f;
                _crowdSource.Play();
            }
        }

        // ── Public API ────────────────────────────────────────────────────
        public void SetRainIntensity(float intensity)
        {
            if (rainAmbienceClip == null || _rainSource == null) return;
            if (intensity > 0.05f && !_rainSource.isPlaying)
            {
                _rainSource.clip = rainAmbienceClip;
                _rainSource.Play();
            }
            _rainSource.volume = intensity * effectsVolume * masterVolume;
        }

        public void PlayCollision(float force)
        {
            if (force < 5f) return;
            var clip = force > 25f ? collisionClipHeavy : collisionClipLight;
            PlayOneShot(clip, effectsVolume);
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

        // ── Settings ──────────────────────────────────────────────────────
        public void SetMasterVolume(float v)
        {
            masterVolume = Mathf.Clamp01(v);
            AudioListener.volume = masterVolume;
        }

        public void SetEngineVolume(float v)  => engineVolume  = Mathf.Clamp01(v);
        public void SetEffectsVolume(float v) => effectsVolume = Mathf.Clamp01(v);
        public void SetMusicVolume(float v)   => musicVolume   = Mathf.Clamp01(v);

        // ── Helpers ───────────────────────────────────────────────────────
        AudioSource CreateSource(string label, float vol, bool loop)
        {
            var go  = new GameObject($"AudioSource_{label}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.volume = vol;
            src.loop   = loop;
            src.spatialBlend = 0f;  // 2D
            return src;
        }

        void PlayOneShot(AudioClip clip, float volume,
                         AudioSource source = null)
        {
            if (clip == null) return;
            (source ?? _radioSource)?.PlayOneShot(clip, volume * masterVolume);
        }

        IEnumerator FadeOut(AudioSource source, float duration)
        {
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
