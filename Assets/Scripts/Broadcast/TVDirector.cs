using UnityEngine;
using System.Collections.Generic;
namespace RacingGame.Broadcast
{
    /// <summary>
    /// Handles the actual camera switching to make the game feel like F1 TV.
    /// Attach this to a persistent GameObject in your race scene.
    /// </summary>
    public class TVDirector : MonoBehaviour
    {
        public static TVDirector Instance { get; private set; }
        [Header("Camera References")]
        public Transform mainCamera;
        public List<Transform> trackCameras = new List<Transform>();
        [Header("Broadcast Settings")]
        public float switchInterval = 8f;
        private float timer;
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
        void Update()
        {
            if (mainCamera == null || trackCameras.Count == 0) return;
            timer += Time.deltaTime;
            if (timer >= switchInterval)
            {
                SwitchRandomCamera();
                timer = 0f;
            }
        }
        /// <summary>
        /// Switch to a random track camera (helicopter, trackside, etc.).
        /// </summary>
        public void SwitchRandomCamera()
        {
            int index = Random.Range(0, trackCameras.Count);
            mainCamera.position = trackCameras[index].position;
            mainCamera.rotation = trackCameras[index].rotation;
        }
        /// <summary>
        /// Focus the main camera on a specific driver (e.g., during an overtake or crash).
        /// </summary>
        public void FocusOnDriver(Transform driver)
        {
            if (driver == null || mainCamera == null) return;
            // Position camera slightly above and behind the driver
            mainCamera.position = driver.position + new Vector3(0, 5, -10);
            mainCamera.LookAt(driver);
        }
        /// <summary>
        /// Force an immediate camera switch to a specific track camera index.
        /// </summary>
        public void ForceCameraSwitch(int index)
        {
            if (index >= 0 && index < trackCameras.Count)
            {
                mainCamera.position = trackCameras[index].position;
                mainCamera.rotation = trackCameras[index].rotation;
                timer = 0f; // Reset timer
            }
        }
    }
}
