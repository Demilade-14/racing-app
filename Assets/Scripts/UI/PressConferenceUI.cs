using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RacingGame.Career;

namespace RacingGame.UI
{
    // ═══════════════════════════════════════════════════════════════════════
    //  PRESS CONFERENCE UI  – post-race media decision system
    // ═══════════════════════════════════════════════════════════════════════
    public class PressConferenceUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI headlineText;           // "Victory Press Conference"
        [SerializeField] TextMeshProUGUI questionText;           // "How do you feel?"
        [SerializeField] Transform choicesContainer;
        [SerializeField] GameObject choiceButtonPrefab;
        [SerializeField] CanvasGroup canvasGroup;

        MediaEventSystem _media;
        DriverCareerManager _career;
        MediaEventSystem.PressConference _currentConference;
        bool _answered = false;

        void Start()
        {
            _media = MediaEventSystem.Instance;
            _career = DriverCareerManager.Instance;

            if (_media == null || _career == null)
            {
                Debug.LogError("[PressConferenceUI] Required managers not found!");
                gameObject.SetActive(false);
                return;
            }

            _media.OnPressConferenceTriggered += Show;
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }

        void Show(MediaEventSystem.PressConference conference)
        {
            _currentConference = conference;
            _answered = false;

            headlineText.text = conference.headline;
            questionText.text = conference.questionText;

            // Clear old buttons
            foreach (Transform child in choicesContainer)
                Destroy(child.gameObject);

            // Create choice buttons
            int choiceIndex = 0;
            foreach (var choice in conference.choices)
            {
                var buttonObj = Instantiate(choiceButtonPrefab, choicesContainer);
                var button = buttonObj.GetComponent<Button>();
                var buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

                buttonText.text = choice.choiceText;

                int index = choiceIndex;  // Capture for closure
                button.onClick.AddListener(() => OnChoiceSelected(index, choice));

                choiceIndex++;
            }

            // Show with animation
            gameObject.SetActive(true);
            CanvasGroupFade(0, 1, 0.3f);

            Debug.Log("[PressConferenceUI] Conference shown with " + conference.choices.Count + " choices");
        }

        void OnChoiceSelected(int choiceIndex, MediaEventSystem.MediaDecision choice)
        {
            if (_answered) return;  // Prevent double-clicking
            _answered = true;

            // Process the choice
            _media.ProcessMediaChoice(choice, _career.PlayerProfile);

            // Show outcome
            ShowOutcome(choice);
        }

        void ShowOutcome(MediaEventSystem.MediaDecision choice)
        {
            // Display outcome modal
            questionText.text = choice.outcomeText;
            headlineText.text = "📰 " + headlineText.text;

            // Disable all choice buttons
            foreach (Transform child in choicesContainer)
            {
                var btn = child.GetComponent<Button>();
                if (btn) btn.interactable = false;
            }

            // Auto-close after 3 seconds
            Invoke(nameof(Close), 3f);
        }

        public void Close()
        {
            CanvasGroupFade(1, 0, 0.3f);
            Invoke(nameof(DisablePanel), 0.3f);
        }

        void DisablePanel()
        {
            gameObject.SetActive(false);
        }

        void CanvasGroupFade(float from, float to, float duration)
        {
            StartCoroutine(FadeCoroutine(from, to, duration));
        }

        System.Collections.IEnumerator FadeCoroutine(float from, float to, float duration)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = to;
        }

        void OnDestroy()
        {
            if (_media)
                _media.OnPressConferenceTriggered -= Show;
        }
    }
}
