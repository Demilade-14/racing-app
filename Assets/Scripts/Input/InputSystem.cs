using UnityEngine;
using UnityEngine.InputSystem;
using RacingGame.Data;
using RacingGame.Physics;

namespace RacingGame.Input
{
    // ═══════════════════════════════════════════════════════════════════════
    //  INPUT SETTINGS
    // ═══════════════════════════════════════════════════════════════════════
    [System.Serializable]
    public class InputSettings
    {
        [Range(0.1f, 1f)]  public float steeringSensitivity  = 0.6f;
        [Range(0f, 1f)]    public float steeringDeadzone     = 0.05f;
        [Range(0f, 1f)]    public float steeringLinearity    = 0.5f;   // 0=linear, 1=cubic
        public bool               invertSteering             = false;
        public bool               tractionControlOn          = true;
        public bool               absOn                      = true;
        public bool               steeringAssist             = true;
        public EngineMode         defaultEngineMode          = EngineMode.Normal;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  PLAYER INPUT HANDLER
    // ═══════════════════════════════════════════════════════════════════════
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("References")]
        public PhysicsIntegrator physics;
        public InputSettings     settings = new();

        // Touch regions (normalised 0–1 screen coords)
        const float THROTTLE_TOUCH_X = 0.75f;   // right 25% = throttle/brake
        const float BRAKE_TOUCH_Y    = 0.5f;    // bottom half = brake, top = throttle

        // Runtime
        float _rawSteering;
        float _smoothSteering;
        bool  _drsQueued;
        float _drsCooldown;

        // Tilt steering
        bool _useTilt;
        float _tiltNeutral;

        void Start()
        {
            _useTilt     = SystemInfo.supportsGyroscope;
            if (_useTilt && SystemInfo.supportsGyroscope)
            {
                UnityEngine.Input.gyro.enabled = true;
                _tiltNeutral = UnityEngine.Input.acceleration.z;
            }
        }

        void Update()
        {
            if (physics == null) return;

            ReadSteering();
            ReadThrottleBrake();
            ReadDRS();
            ReadEngineMode();

            ApplyInputToPhysics();
        }

        void ReadSteering()
        {
            // Gamepad
            if (Gamepad.current != null)
            {
                _rawSteering = Gamepad.current.leftStick.x.ReadValue();
            }
            // Tilt
            else if (_useTilt)
            {
                float tilt = UnityEngine.Input.acceleration.z - _tiltNeutral;
                _rawSteering = Mathf.Clamp(tilt / 0.4f, -1f, 1f);
            }
            // Keyboard fallback
            else
            {
                float left  = Keyboard.current?.aKey.isPressed == true ? -1f : 0f;
                float right = Keyboard.current?.dKey.isPressed == true ?  1f : 0f;
                _rawSteering = left + right;
            }

            // Deadzone
            if (Mathf.Abs(_rawSteering) < settings.steeringDeadzone)
                _rawSteering = 0f;

            // Linearity
            float linear = _rawSteering;
            float cubic  = _rawSteering * _rawSteering * _rawSteering;
            _rawSteering = Mathf.Lerp(linear, cubic, settings.steeringLinearity)
                         * settings.steeringSensitivity;

            if (settings.invertSteering) _rawSteering = -_rawSteering;

            // Smooth to avoid snap
            float smoothSpeed = physics.State.speed > 150f ? 4f : 6f;
            _smoothSteering = Mathf.Lerp(_smoothSteering, _rawSteering, smoothSpeed * Time.deltaTime);
        }

        void ReadThrottleBrake()
        {
            var state = physics.State;

            // Gamepad triggers
            if (Gamepad.current != null)
            {
                state.throttle = Gamepad.current.rightTrigger.ReadValue();
                state.brake    = Gamepad.current.leftTrigger.ReadValue();
            }
            // Keyboard
            else if (Keyboard.current != null)
            {
                state.throttle = Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed ? 1f : 0f;
                state.brake    = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed ? 1f : 0f;
            }
            // Touch
            else
            {
                state.throttle = 0f;
                state.brake    = 0f;
                for (int i = 0; i < UnityEngine.Input.touchCount; i++)
                {
                    var t = UnityEngine.Input.GetTouch(i);
                    float nx = t.position.x / Screen.width;
                    float ny = t.position.y / Screen.height;
                    if (nx > THROTTLE_TOUCH_X)
                        state.throttle = ny > BRAKE_TOUCH_Y ? 1f : 0f;
                    if (nx > THROTTLE_TOUCH_X && ny <= BRAKE_TOUCH_Y)
                        state.brake = 1f;
                }
            }

            // Traction Control
            if (settings.tractionControlOn)
            {
                float avgGrip = 0f;
                foreach (var tire in state.tires) avgGrip += tire.wearPercent;
                avgGrip = 1f - avgGrip / 400f;
                if (state.throttle > avgGrip * 0.9f && state.speed < 80f)
                    state.throttle *= avgGrip;
            }

            // ABS
            if (settings.absOn && state.brake > 0.8f)
                state.brake = Mathf.Lerp(state.brake, 0.7f, 0.3f);
        }

        void ReadDRS()
        {
            _drsCooldown -= Time.deltaTime;
            var state = physics.State;

            bool drsPress = (Gamepad.current?.buttonEast.wasPressedThisFrame == true)
                          || (Keyboard.current?.spaceKey.wasPressedThisFrame == true);

            if (drsPress && state.drsEligible && _drsCooldown <= 0f)
            {
                state.drsActive = !state.drsActive;
                _drsCooldown    = 0.5f;
            }

            // DRS deactivated on braking
            if (state.brake > 0.1f) state.drsActive = false;
        }

        void ReadEngineMode()
        {
            if (Keyboard.current == null) return;
            var state = physics.State;

            if (Keyboard.current.digit1Key.wasPressedThisFrame) state.engineMode = EngineMode.Eco;
            if (Keyboard.current.digit2Key.wasPressedThisFrame) state.engineMode = EngineMode.Normal;
            if (Keyboard.current.digit3Key.wasPressedThisFrame) state.engineMode = EngineMode.Push;
            if (Keyboard.current.digit4Key.wasPressedThisFrame) state.engineMode = EngineMode.Overtake;
        }

        void ApplyInputToPhysics()
        {
            physics.State.steering = _smoothSteering;
        }

        // Called from touch UI button
        public void OnDRSButtonPressed()  => _drsQueued = true;
        public void OnThrottleDown()      => physics.State.throttle = 1f;
        public void OnThrottleUp()        => physics.State.throttle = 0f;
        public void OnBrakeDown()         => physics.State.brake    = 1f;
        public void OnBrakeUp()           => physics.State.brake    = 0f;
        public void SetSteering(float v)  => _rawSteering = Mathf.Clamp(v, -1f, 1f);
    }
}
