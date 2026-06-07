using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RacingGame.Career;
using RacingGame.Data;

namespace RacingGame.UI
{
    // ═══════════════════════════════════════════════════════════════════════
    //  CAREER HUB UI  – main career mode interface after race/season
    // ═══════════════════════════════════════════════════════════════════════
    public class CareerHubUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI driverNameText;
        [SerializeField] TextMeshProUGUI championshipText;      // P2 | 68 pts
        [SerializeField] TextMeshProUGUI overallRatingText;     // OVR 82/99
        [SerializeField] TextMeshProUGUI careerStatsText;       // Wins: 2 | Podiums: 4
        
        [SerializeField] Button raceButton;
        [SerializeField] Button rndButton;
        [SerializeField] Button mediaButton;
        [SerializeField] Button contractButton;
        [SerializeField] Button settingsButton;

        [SerializeField] TextMeshProUGUI roundInfoText;         // Round 5/24: Silverstone
        [SerializeField] Image[] attributeBars;                  // cornering, braking, overtaking, etc.

        DriverCareerManager _careerManager;
        CareerIntegrationLayer _integration;

        void Start()
        {
            _careerManager = DriverCareerManager.Instance;
            _integration = CareerIntegrationLayer.Instance;

            if (_careerManager == null || _integration == null)
            {
                Debug.LogError("[CareerHubUI] Career managers not initialized!");
                return;
            }

            // Wire button clicks
            raceButton?.onClick.AddListener(OnRaceButtonClicked);
            rndButton?.onClick.AddListener(OnRnDButtonClicked);
            mediaButton?.onClick.AddListener(OnMediaButtonClicked);
            contractButton?.onClick.AddListener(OnContractButtonClicked);
            settingsButton?.onClick.AddListener(OnSettingsButtonClicked);

            // Listen to career events
            _careerManager.OnRaceCompleted += RefreshUI;
            _careerManager.OnSeasonComplete += OnSeasonEnded;

            RefreshUI(null, 0);
        }

        void RefreshUI(RaceResult result = null, int xpGained = 0)
        {
            if (_careerManager?.PlayerProfile == null) return;

            var profile = _careerManager.PlayerProfile;

            // Driver info
            driverNameText.text = profile.driverName;
            overallRatingText.text = $"OVR {profile.attributes.OverallRating}/99";
            careerStatsText.text = 
                $"Races: {profile.careerRaces} | " +
                $"Wins: {profile.careerWins} | " +
                $"Podiums: {profile.careerPodiums}";

            // Championship standing
            championshipText.text = _integration.GetChampionshipStanding();

            // Round info
            roundInfoText.text = _integration.GetCurrentRoundInfo();

            // Attribute bars (visualize 0-99 scale)
            UpdateAttributeBars(profile);

            Debug.Log("[CareerHubUI] UI refreshed");
        }

        void UpdateAttributeBars(DriverProfile profile)
        {
            var attrs = profile.attributes;
            int[] values = { attrs.cornering, attrs.braking, attrs.overtaking, 
                            attrs.defending, attrs.awareness, attrs.consistency };

            for (int i = 0; i < attributeBars.Length && i < values.Length; i++)
            {
                attributeBars[i].fillAmount = values[i] / 99f;
            }
        }

        void OnRaceButtonClicked()
        {
            _integration.AdvanceToRaceWeekend();
            Debug.Log("[CareerHubUI] Starting race...");
            // Load race scene here
        }

        void OnRnDButtonClicked()
        {
            Debug.Log("[CareerHubUI] Opening R&D garage");
            // Open R&D Garage UI panel
        }

        void OnMediaButtonClicked()
        {
            Debug.Log("[CareerHubUI] Opening media panel");
            // Show team morale, fan popularity, sponsor status
        }

        void OnContractButtonClicked()
        {
            Debug.Log("[CareerHubUI] Opening contract offers");
            // Show pending contract offers with negotiate buttons
        }

        void OnSettingsButtonClicked()
        {
            Debug.Log("[CareerHubUI] Opening settings");
            // Show settings menu
        }

        void OnSeasonEnded(int finalPosition)
        {
            Debug.Log($"[CareerHubUI] Season ended! Position: {finalPosition}");
            // Show season summary screen
            RefreshUI();
        }

        void OnDestroy()
        {
            if (raceButton) raceButton.onClick.RemoveListener(OnRaceButtonClicked);
            if (rndButton) rndButton.onClick.RemoveListener(OnRnDButtonClicked);
            if (mediaButton) mediaButton.onClick.RemoveListener(OnMediaButtonClicked);
            if (contractButton) contractButton.onClick.RemoveListener(OnContractButtonClicked);
            if (settingsButton) settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);

            if (_careerManager)
            {
                _careerManager.OnRaceCompleted -= RefreshUI;
                _careerManager.OnSeasonComplete -= OnSeasonEnded;
            }
        }
    }
}
