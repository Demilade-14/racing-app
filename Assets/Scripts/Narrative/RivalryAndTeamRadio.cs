using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RacingGame.Narrative
{
    // ═══════════════════════════════════════════════════════════════════════
    //  RIVALRY SYSTEM – track AI driver tension, aggression
    // ═══════════════════════════════════════════════════════════════════════
    [System.Serializable]
    public class DriverRivalry
    {
        public string aiDriverName;
        public float rivalryTension = 0f;  // 0-100
        public int collisionsWithDriver = 0;
        public int overtakesAgainstDriver = 0;
        public int beatenByDriver = 0;
        public enum RivalryStatus { None, Rival, Bitter, Dangerous }
        public RivalryStatus status = RivalryStatus.None;

        public void UpdateStatus()
        {
            status = rivalryTension switch
            {
                >= 80 => RivalryStatus.Dangerous,
                >= 60 => RivalryStatus.Bitter,
                >= 30 => RivalryStatus.Rival,
                _ => RivalryStatus.None
            };
        }

        public string GetTensionDisplay() => rivalryTension switch
        {
            >= 80 => "🔴🔴🔴 INTENSE",
            >= 60 => "🔴🔴 HIGH",
            >= 30 => "🟡 ELEVATED",
            _ => "🟢 NEUTRAL"
        };
    }

    public class RivalrySystem : MonoBehaviour
    {
        public static RivalrySystem Instance { get; private set; }

        public Dictionary<string, DriverRivalry> rivalries = new();
        public event Action<string> OnRivalryEscalated;  // (aiDriverName)
        public event Action<string, string> OnRivalryRaceEvent;  // (event, driver)

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RegisterAIDriver(string driverName)
        {
            if (!rivalries.ContainsKey(driverName))
            {
                rivalries[driverName] = new DriverRivalry { aiDriverName = driverName };
            }
        }

        public void OnCollisionWithDriver(string driverName)
        {
            if (!rivalries.ContainsKey(driverName)) RegisterAIDriver(driverName);

            var rivalry = rivalries[driverName];
            rivalry.collisionsWithDriver++;
            rivalry.rivalryTension += 15f;  // Collision escalates tension
            rivalry.UpdateStatus();

            OnRivalryRaceEvent?.Invoke($"Collision with {driverName}", driverName);

            if (rivalry.status == DriverRivalry.RivalryStatus.Dangerous)
                OnRivalryEscalated?.Invoke(driverName);

            Debug.Log($"[Rivalry] Collision with {driverName}. Tension: {rivalry.rivalryTension:F0}%");
        }

        public void OnOvertakeDriver(string driverName)
        {
            if (!rivalries.ContainsKey(driverName)) RegisterAIDriver(driverName);

            var rivalry = rivalries[driverName];
            rivalry.overtakesAgainstDriver++;
            rivalry.rivalryTension += 5f;  // Slight tension increase

            OnRivalryRaceEvent?.Invoke($"Overtook {driverName}", driverName);

            Debug.Log($"[Rivalry] Overtook {driverName}. Total overtakes: {rivalry.overtakesAgainstDriver}");
        }

        public void OnLostToDriver(string driverName)
        {
            if (!rivalries.ContainsKey(driverName)) RegisterAIDriver(driverName);

            var rivalry = rivalries[driverName];
            rivalry.beatenByDriver++;
            rivalry.rivalryTension -= 3f;  // Slight respect boost

            Debug.Log($"[Rivalry] Lost to {driverName}. Total losses: {rivalry.beatenByDriver}");
        }

        public float GetAIAggressionMultiplier(string driverName)
        {
            if (!rivalries.ContainsKey(driverName)) 
                return 1f;

            var rivalry = rivalries[driverName];
            return 1f + (rivalry.rivalryTension / 100f) * 0.5f;  // Up to 1.5x aggression
        }

        public DriverRivalry GetRivalryData(string driverName)
        {
            rivalries.TryGetValue(driverName, out var rivalry);
            return rivalry;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TEAM RADIO MANAGER – contextual radio messages & voice lines
    // ═══════════════════════════════════════════════════════════════════════
    public class TeamRadioManager : MonoBehaviour
    {
        public static TeamRadioManager Instance { get; private set; }

        [System.Serializable]
        public class RadioMessage
        {
            public string message;
            public AudioClip audioClip;  // Optional: pre-recorded voice line
            public float displayDuration = 3f;
        }

        public List<RadioMessage> driverMessages = new();
        public List<RadioMessage> engineerMessages = new();
        public event Action<string> OnRadioMessage;  // (message text)

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            InitializeRadioMessages();
        }

        void InitializeRadioMessages()
        {
            // Engineer messages
            engineerMessages.Add(new RadioMessage { message = "Box, box, next lap" });
            engineerMessages.Add(new RadioMessage { message = "Great pace! Keep pushing" });
            engineerMessages.Add(new RadioMessage { message = "Car damage, check temperatures" });
            engineerMessages.Add(new RadioMessage { message = "Tire temps are high" });
            engineerMessages.Add(new RadioMessage { message = "Traffic ahead, p15 ahead" });
            engineerMessages.Add(new RadioMessage { message = "Position p2, gap 1.5 seconds" });
            engineerMessages.Add(new RadioMessage { message = "That overtake was beautiful!" });
            engineerMessages.Add(new RadioMessage { message = "Pit window open next lap" });

            // Driver messages (acknowledgments)
            driverMessages.Add(new RadioMessage { message = "Copy that" });
            driverMessages.Add(new RadioMessage { message = "Understood" });
            driverMessages.Add(new RadioMessage { message = "Push push!" });
        }

        // ── CONTEXT-DRIVEN MESSAGES ───────────────────────────────────────
        public void BroadcastEngineFailure()
        {
            BroadcastRadio("⚠️ Engine failure! Pull over!", true);
        }

        public void BroadcastPitWindow()
        {
            BroadcastRadio("Pit window is open. Come in this lap", true);
        }

        public void BroadcastGreatOvertake()
        {
            BroadcastRadio("Excellent overtake!", true);
        }

        public void BroadcastGapUpdate(float gapSeconds, int position)
        {
            string gap = gapSeconds > 0 ? $"+{gapSeconds:F1}s" : $"{gapSeconds:F1}s";
            BroadcastRadio($"Position P{position}, gap {gap}", false);
        }

        public void BroadcastRivalry(string rivalName)
        {
            BroadcastRadio($"Careful with {rivalName}, he's pushing hard!", false);
        }

        public void BroadcastWeatherAlert(string condition)
        {
            BroadcastRadio($"Weather changing - expect {condition} conditions", true);
        }

        public void BroadcastDamageAlert(string partName)
        {
            BroadcastRadio($"⚠️ {partName} damage detected", true);
        }

        void BroadcastRadio(string message, bool isUrgent)
        {
            OnRadioMessage?.Invoke(message);

            Color urgencyColor = isUrgent ? Color.red : Color.white;
            Debug.Log($"<color={ColorUtility.ToHtmlStringRGB(urgencyColor)}>[Radio] {message}</color>");
        }

        public RadioMessage GetRandomRadioMessage(bool isDriverMessage = false)
        {
            var messages = isDriverMessage ? driverMessages : engineerMessages;
            return messages[Random.Range(0, messages.Count)];
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TEAM RADIO UI – HUD display for team radio messages
    // ═══════════════════════════════════════════════════════════════════════
    public class TeamRadioUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI radioText;
        [SerializeField] Image radioPanel;
        [SerializeField] float messageDuration = 3f;

        private float _messageDismissTime = 0;

        void Start()
        {
            TeamRadioManager.Instance.OnRadioMessage += DisplayMessage;
        }

        void Update()
        {
            // Auto-dismiss messages after duration
            if (Time.time > _messageDismissTime && radioPanel.gameObject.activeSelf)
            {
                radioPanel.gameObject.SetActive(false);
            }
        }

        void DisplayMessage(string message)
        {
            radioText.text = message;
            radioPanel.gameObject.SetActive(true);
            _messageDismissTime = Time.time + messageDuration;

            // Color code based on urgency
            if (message.Contains("⚠️") || message.Contains("🔥"))
                radioPanel.color = new Color(1, 0.3f, 0.3f, 0.9f);  // Red for urgent
            else
                radioPanel.color = new Color(0.2f, 0.5f, 1, 0.8f);   // Blue for normal
        }

        void OnDestroy()
        {
            if (TeamRadioManager.Instance)
                TeamRadioManager.Instance.OnRadioMessage -= DisplayMessage;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TEAM PRINCIPAL MEETING – contract drama event
    // ═══════════════════════════════════════════════════════════════════════
    public class TeamPrincipalMeeting : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI messageText;
        [SerializeField] Button defendButton;
        [SerializeField] Button acceptDemotionButton;
        [SerializeField] Button negotiateButton;

        private int _playerPoints;
        private int _champPoints;

        public void ShowMeeting(int playerPoints, int champPoints)
        {
            _playerPoints = playerPoints;
            _champPoints = champPoints;

            titleText.text = "Team Principal Meeting";

            float performance = ((float)playerPoints / champPoints) * 100f;

            messageText.text = performance < 50f
                ? $"Your performance is unacceptable. ({performance:F0}% of championship leader)\n\nWe're considering replacing you."
                : $"We expected more from you. ({performance:F0}% of leader)\n\nEither step up or step down.";

            defendButton.onClick.AddListener(OnDefend);
            acceptDemotionButton.onClick.AddListener(OnAcceptDemotion);
            negotiateButton.onClick.AddListener(OnNegotiate);

            gameObject.SetActive(true);
        }

        void OnDefend()
        {
            messageText.text = "\"I'll prove myself next race. You won't regret keeping me.\"";
            StartCoroutine(CloseAfterDelay(3f));
        }

        void OnAcceptDemotion()
        {
            messageText.text = "Contract terminated. You've been released to free agency.";
            StartCoroutine(CloseAfterDelay(3f));
        }

        void OnNegotiate()
        {
            messageText.text = "\"Give me 3 races to deliver results. If not, I'll resign.\"";
            StartCoroutine(CloseAfterDelay(3f));
        }

        System.Collections.IEnumerator CloseAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameObject.SetActive(false);
        }
    }
}
