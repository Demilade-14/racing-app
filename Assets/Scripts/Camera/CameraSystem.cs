using UnityEngine;
using RacingGame.Data;
using RacingGame.Physics;

namespace RacingGame.Camera
{
    // ═══════════════════════════════════════════════════════════════════════
    //  CAMERA CONTROLLER
    // ═══════════════════════════════════════════════════════════════════════
    public class RacingCameraController : MonoBehaviour
    {
        [Header("Target")]
        public Transform       target;
        public PhysicsIntegrator targetPhysics;

        [Header("Mode")]
        public CameraMode currentMode = CameraMode.Chase;

        [Header("Chase Cam")]
        public float chaseDistance  = 8f;
        public float chaseHeight    = 2.5f;
        public float chaseLookAhead = 4f;
        public float chaseDamping   = 5f;

        [Header("Cockpit Cam")]
        public Vector3 cockpitOffset = new(0f, 0.75f, 0.4f);

        [Header("TV Cam")]
        public float tvDistance     = 12f;
        public float tvHeight       = 3.5f;

        [Header("Helicopter")]
        public float heliAltitude   = 30f;
        public float heliDistance   = 20f;

        [Header("FOV")]
        public float baseFOV        = 65f;
        public float maxSpeedFOV    = 85f;
        public float speedFOVTarget = 350f;

        UnityEngine.Camera _cam;
        Vector3   _velocity;
        float     _transitionTime;
        CameraMode _previousMode;
        float     _shakeIntensity;
        float     _shakeDuration;

        void Awake()
        {
            _cam = GetComponent<UnityEngine.Camera>();
            if (_cam == null) _cam = gameObject.AddComponent<UnityEngine.Camera>();
        }

        void LateUpdate()
        {
            if (target == null) return;

            UpdateFOV();
            UpdateShake();

            switch (currentMode)
            {
                case CameraMode.Chase:       UpdateChaseCamera();      break;
                case CameraMode.Cockpit:     UpdateCockpitCamera();    break;
                case CameraMode.TV:          UpdateTVCamera();         break;
                case CameraMode.Helicopter:  UpdateHelicopterCamera(); break;
                case CameraMode.Track:       UpdateTrackCamera();      break;
            }
        }

        void UpdateChaseCamera()
        {
            float speed = targetPhysics != null ? targetPhysics.State.speed : 0f;

            // Adaptive distance at high speed
            float dynDistance = Mathf.Lerp(chaseDistance, chaseDistance * 1.3f,
                                           speed / 350f);

            Vector3 desiredPos = target.position
                               - target.forward * dynDistance
                               + Vector3.up * chaseHeight;

            transform.position = Vector3.SmoothDamp(transform.position, desiredPos,
                                    ref _velocity, 1f / chaseDamping);

            Vector3 lookTarget = target.position
                               + target.forward * chaseLookAhead
                               + Vector3.up * 0.5f;
            transform.LookAt(lookTarget);
        }

        void UpdateCockpitCamera()
        {
            transform.position = target.TransformPoint(cockpitOffset);
            transform.rotation = target.rotation;

            // Slight head shake at high speed
            if (targetPhysics != null)
            {
                float gLat = targetPhysics.State.gLateral;
                transform.rotation *= Quaternion.Euler(0f, 0f, -gLat * 2.5f);
            }
        }

        void UpdateTVCamera()
        {
            Vector3 desired = target.position
                            - target.forward * tvDistance
                            + Vector3.up     * tvHeight
                            + target.right   * 3f;

            transform.position = Vector3.SmoothDamp(transform.position, desired,
                                    ref _velocity, 0.15f);
            transform.LookAt(target.position + Vector3.up);
        }

        void UpdateHelicopterCamera()
        {
            Vector3 desired = target.position
                            + Vector3.up     * heliAltitude
                            + target.forward * -heliDistance;

            transform.position = Vector3.SmoothDamp(transform.position, desired,
                                    ref _velocity, 0.30f);
            transform.LookAt(target.position);
        }

        void UpdateTrackCamera()
        {
            // Static trackside — driven by external TrackCameraSelector
        }

        void UpdateFOV()
        {
            if (targetPhysics == null) return;
            float t = Mathf.Clamp01(targetPhysics.State.speed / speedFOVTarget);
            float targetFOV = Mathf.Lerp(baseFOV, maxSpeedFOV, t);
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFOV, 3f * Time.deltaTime);
        }

        void UpdateShake()
        {
            if (_shakeDuration <= 0f) return;
            _shakeDuration -= Time.deltaTime;
            float x = Random.Range(-_shakeIntensity, _shakeIntensity);
            float y = Random.Range(-_shakeIntensity, _shakeIntensity);
            transform.position += new Vector3(x, y, 0f);
        }

        public void TriggerShake(float intensity, float duration)
        {
            _shakeIntensity = intensity;
            _shakeDuration  = duration;
        }

        public void SwitchMode(CameraMode mode) => currentMode = mode;

        public void CycleCamera()
        {
            currentMode = (CameraMode)(((int)currentMode + 1) % System.Enum.GetValues(typeof(CameraMode)).Length);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TRACKSIDE CAMERA SELECTOR  (rotates through fixed broadcast positions)
    // ═══════════════════════════════════════════════════════════════════════
    public class TracksideCameraSelector : MonoBehaviour
    {
        public Transform[]             trackCamPositions;
        public RacingCameraController  mainCamera;
        public float                   autoSwitchInterval = 8f;

        int   _currentCam;
        float _timer;

        void Update()
        {
            if (mainCamera.currentMode != CameraMode.Track) return;

            _timer += Time.deltaTime;
            if (_timer >= autoSwitchInterval)
            {
                _timer = 0f;
                NextCamera();
            }

            if (trackCamPositions != null && _currentCam < trackCamPositions.Length)
            {
                var tc = trackCamPositions[_currentCam];
                mainCamera.transform.position = Vector3.Lerp(
                    mainCamera.transform.position, tc.position, 5f * Time.deltaTime);
                mainCamera.transform.LookAt(mainCamera.target);
            }
        }

        public void NextCamera()
        {
            if (trackCamPositions == null || trackCamPositions.Length == 0) return;
            _currentCam = (_currentCam + 1) % trackCamPositions.Length;
        }
    }
}
