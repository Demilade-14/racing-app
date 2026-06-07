using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using RacingGame.Data;
using RacingGame.Physics;

namespace RacingGame.UI
{
    public enum SteeringMode { Tilt, TouchSlider }

    // ═══════════════════════════════════════════════════════════════════════
    //  INPUT STATE  –  plain data read by PhysicsIntegrator
    // ═══════════════════════════════════════════════════════════════════════
    public class MobileInputState
    {
        public float  Steering;       // -1 to +1
        public float  Throttle;       // 0 to 1
        public float  Brake;          // 0 to 1
        public bool   DRSPressed;
        public bool   PitRequested;
        public ERSMode  ERSMode      = ERSMode.Balanced;
        public float  BrakeBias      = 0.57f;   // 0.48 – 0.70

        public void ConsumeOneShots()
        {
            DRSPressed   = false;
            PitRequested = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  MOBILE INPUT MANAGER
    // ═══════════════════════════════════════════════════════════════════════
    public class MobileInputManager : MonoBehaviour
    {
        // ── Inspector refs ────────────────────────────────────────────────
        [Header("Mode")]
        public SteeringMode steeringMode = SteeringMode.Tilt;

        [Header("Steering")]
        public RectTransform steeringSliderArea;   // left-side touch zone
        [Range(10f, 60f)] public float tiltSensitivity = 30f;
        [Range(2f, 15f)]  public float steeringReturnSpeed = 6f;

        [Header("Pedals")]
        public RectTransform throttleButton;
        public RectTransform brakeButton;

        [Header("D-Pad Buttons")]
        public Button drsButton;
        public Button ersUpButton;
        public Button ersDownButton;
        public Button brakeBiasUpButton;
        public Button brakeBiasDownButton;
        public Button pitRequestButton;

        [Header("Feedback Labels")]
        public TextMeshProUGUI ersModeLabel;
        public TextMeshProUGUI brakeBiasLabel;
        public TextMeshProUGUI drsStatusLabel;
        public TextMeshProUGUI pitStatusLabel;

        // ── Public read-only state ─────────────────────────────────────────
        public MobileInputState State { get; } = new();

        // ── Internals ─────────────────────────────────────────────────────
        static readonly ERSMode[] ERS_CYCLE =
            { ERSMode.Harvest, ERSMode.Balanced, ERSMode.Overtake, ERSMode.Qualifying };

        int   _ersIndex  = 1;   // start at Balanced
        bool  _throttleHeld;
        bool  _brakeHeld;
        float _sliderStartX;
        bool  _sliderActive;
        int   _sliderFingerId = -1;

        const float BRAKE_BIAS_STEP = 0.01f;
        const float BRAKE_BIAS_MIN  = 0.48f;
        const float BRAKE_BIAS_MAX  = 0.70f;

        // ── PhysicsIntegrator to push inputs into ─────────────────────────
        PhysicsIntegrator _physics;

        void Awake()
        {
            _physics = FindFirstObjectByType<PhysicsIntegrator>();
            WireButtons();
        }

        void WireButtons()
        {
            drsButton          ?.onClick.AddListener(OnDRS);
            ersUpButton        ?.onClick.AddListener(OnERSUp);
            ersDownButton      ?.onClick.AddListener(OnERSDown);
            brakeBiasUpButton  ?.onClick.AddListener(OnBBUp);
            brakeBiasDownButton?.onClick.AddListener(OnBBDown);
            pitRequestButton   ?.onClick.AddListener(OnPitRequest);

            WirePedal(throttleButton, onDown: () => _throttleHeld = true,
                                      onUp:   () => _throttleHeld = false);
            WirePedal(brakeButton,    onDown: () => _brakeHeld    = true,
                                      onUp:   () => _brakeHeld    = false);

            if (steeringMode == SteeringMode.TouchSlider)
                WireSteeringSlider();
        }

        static void WirePedal(RectTransform rt, Action onDown, Action onUp)
        {
            if (rt == null) return;
            var trigger = rt.gameObject.AddComponent<EventTrigger>();
            AddEntry(trigger, EventTriggerType.PointerDown, _ => onDown());
            AddEntry(trigger, EventTriggerType.PointerUp,   _ => onUp());
        }

        void WireSteeringSlider()
        {
            if (steeringSliderArea == null) return;
            var trigger = steeringSliderArea.gameObject.AddComponent<EventTrigger>();
            AddEntry(trigger, EventTriggerType.BeginDrag, OnSliderBegin);
            AddEntry(trigger, EventTriggerType.Drag,      OnSliderDrag);
            AddEntry(trigger, EventTriggerType.EndDrag,   OnSliderEnd);
        }

        void OnSliderBegin(BaseEventData data)
        {
            var pd = (PointerEventData)data;
            _sliderStartX  = pd.position.x;
            _sliderFingerId = pd.pointerId;
            _sliderActive   = true;
        }

        void OnSliderDrag(BaseEventData data)
        {
            var pd = (PointerEventData)data;
            if (pd.pointerId != _sliderFingerId) return;
            float w   = steeringSliderArea.rect.width * 0.5f;
            float delta = pd.position.x - _sliderStartX;
            State.Steering = Mathf.Clamp(delta / w, -1f, 1f);
        }

        void OnSliderEnd(BaseEventData data)
        {
            _sliderActive = false;
            _sliderFingerId = -1;
        }

        static void AddEntry(EventTrigger t, EventTriggerType type, Action<BaseEventData> cb)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(d => cb(d));
            t.triggers.Add(entry);
        }

        // ── Update loop ───────────────────────────────────────────────────
        void Update()
        {
            UpdateSteering();
            UpdatePedals();
            PushToPhysics();
            UpdateLabels();
            State.ConsumeOneShots();
        }

        void UpdateSteering()
        {
            if (steeringMode == SteeringMode.Tilt)
            {
                float tilt   = Input.acceleration.x;
                float target = Mathf.Clamp(tilt * tiltSensitivity / 30f, -1f, 1f);
                State.Steering = Mathf.MoveTowards(State.Steering, target,
                                     Time.deltaTime * steeringReturnSpeed);
            }
            else if (!_sliderActive)
            {
                // Return to centre when finger lifted
                State.Steering = Mathf.MoveTowards(State.Steering, 0f,
                                     Time.deltaTime * steeringReturnSpeed);
            }
        }

        void UpdatePedals()
        {
            State.Throttle = _throttleHeld ? 1f
                : Mathf.MoveTowards(State.Throttle, 0f, Time.deltaTime * 4f);
            State.Brake    = _brakeHeld    ? 1f
                : Mathf.MoveTowards(State.Brake,    0f, Time.deltaTime * 8f);
        }

        void PushToPhysics()
        {
            if (_physics == null) return;
            var s              = _physics.State;
            s.steering         = State.Steering;
            s.throttle         = State.Throttle;
            s.brake            = State.Brake;
            s.ersMode          = State.ERSMode;
            if (State.DRSPressed) s.drsActive = !s.drsActive;
        }

        // ── Button callbacks ──────────────────────────────────────────────
        void OnDRS()
        {
            State.DRSPressed = true;
        }

        void OnERSUp()
        {
            _ersIndex         = Mathf.Min(_ersIndex + 1, ERS_CYCLE.Length - 1);
            State.ERSMode     = ERS_CYCLE[_ersIndex];
        }

        void OnERSDown()
        {
            _ersIndex         = Mathf.Max(_ersIndex - 1, 0);
            State.ERSMode     = ERS_CYCLE[_ersIndex];
        }

        void OnBBUp()   =>
            State.BrakeBias   = Mathf.Clamp(State.BrakeBias + BRAKE_BIAS_STEP, BRAKE_BIAS_MIN, BRAKE_BIAS_MAX);

        void OnBBDown() =>
            State.BrakeBias   = Mathf.Clamp(State.BrakeBias - BRAKE_BIAS_STEP, BRAKE_BIAS_MIN, BRAKE_BIAS_MAX);

        void OnPitRequest() => State.PitRequested = true;

        // ── Label updates ─────────────────────────────────────────────────
        void UpdateLabels()
        {
            if (ersModeLabel)   ersModeLabel.text   = State.ERSMode.ToString().ToUpper();
            if (brakeBiasLabel) brakeBiasLabel.text = $"BB {State.BrakeBias * 100f:F0}%";

            if (drsStatusLabel && _physics != null)
            {
                bool eligible = _physics.State.drsEligible;
                bool active   = _physics.State.drsActive;
                drsStatusLabel.text  = active ? "DRS ON" : eligible ? "DRS RDY" : "DRS";
                drsStatusLabel.color = active ? Color.green : eligible ? Color.yellow : Color.grey;
            }

            if (pitStatusLabel)
            {
                pitStatusLabel.text  = State.PitRequested ? "PIT ✓" : "PIT";
                pitStatusLabel.color = State.PitRequested ? Color.yellow : Color.white;
            }
        }
    }
}
