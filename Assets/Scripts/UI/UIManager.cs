using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Voidbound.Core;
using Voidbound.Player;

namespace Voidbound.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Menu Panels")]
        [SerializeField] private GameObject startMenuPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button gameOverRestartButton;

        [Header("HUD Elements")]
        [SerializeField] private Image crosshairImage;
        [SerializeField] private Text interactionPromptText;
        [SerializeField] private Text livesText;
        [SerializeField] private Text sectionText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text controlsTutorialText;
        [SerializeField] private Text notificationText;

        [Header("Post-Processing Blur Reference")]
        [SerializeField] private Volume postProcessVolume;

        private DepthOfField depthOfField;
        private Coroutine notificationCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Cache Depth of Field from Volume if present
            if (postProcessVolume != null && postProcessVolume.profile != null)
            {
                postProcessVolume.profile.TryGet(out depthOfField);
            }

            SetupButtons();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                GameManager.Instance.OnLivesChanged += UpdateLivesDisplay;
                GameManager.Instance.OnTimerUpdated += UpdateTimerDisplay;
                GameManager.Instance.OnSectionChanged += UpdateSectionDisplay;
                GameManager.Instance.OnNotification += DisplayNotification;

                // Initialize displays
                UpdateLivesDisplay(GameManager.Instance.CurrentLives, GameManager.Instance.MaxLives);
                UpdateSectionDisplay(GameManager.Instance.CurrentSectionName);
                UpdateTimerDisplay(GameManager.Instance.ElapsedTime);
                HandleGameStateChanged(GameManager.Instance.CurrentState);
            }

            // Find player interactor to bind prompts
            PlayerInteractor interactor = FindFirstObjectByType<PlayerInteractor>();
            if (interactor != null)
            {
                interactor.OnPromptUpdated += SetInteractionPrompt;
            }

            if (interactionPromptText != null)
            {
                interactionPromptText.text = string.Empty;
            }

            if (notificationText != null)
            {
                notificationText.text = string.Empty;
                notificationText.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnLivesChanged -= UpdateLivesDisplay;
                GameManager.Instance.OnTimerUpdated -= UpdateTimerDisplay;
                GameManager.Instance.OnSectionChanged -= UpdateSectionDisplay;
                GameManager.Instance.OnNotification -= DisplayNotification;
            }

            PlayerInteractor interactor = FindFirstObjectByType<PlayerInteractor>();
            if (interactor != null)
            {
                interactor.OnPromptUpdated -= SetInteractionPrompt;
            }
        }

        private void SetupButtons()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.StartGame();
                });
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
                });
            }

            if (pauseRestartButton != null)
            {
                pauseRestartButton.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.RestartGame();
                });
            }

            if (gameOverRestartButton != null)
            {
                gameOverRestartButton.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.RestartGame();
                });
            }
        }

        public void HandleGameStateChanged(GameState state)
        {
            if (startMenuPanel != null) startMenuPanel.SetActive(state == GameState.StartMenu);
            if (hudPanel != null) hudPanel.SetActive(state == GameState.Playing);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(state == GameState.Paused);
            if (gameOverPanel != null) gameOverPanel.SetActive(state == GameState.GameOver);

            // Toggle background depth of field blur for Start Menu
            if (depthOfField != null)
            {
                depthOfField.active = (state == GameState.StartMenu);
            }
        }

        public void UpdateLivesDisplay(int current, int max)
        {
            if (livesText == null) return;
            string hearts = "";
            for (int i = 0; i < max; i++)
            {
                hearts += (i < current) ? "❤️ " : "🖤 ";
            }
            livesText.text = $"LIVES: {hearts.Trim()}";
        }

        public void UpdateTimerDisplay(float timeInSeconds)
        {
            if (timerText != null)
            {
                timerText.text = $"TIME: {GameManager.FormatTime(timeInSeconds)}";
            }
        }

        public void UpdateSectionDisplay(string sectionName)
        {
            if (sectionText != null)
            {
                sectionText.text = sectionName.ToUpper();
            }
        }

        public void SetInteractionPrompt(string prompt)
        {
            if (interactionPromptText != null)
            {
                interactionPromptText.text = prompt;
            }
        }

        public void DisplayNotification(string message, float duration)
        {
            // Event reporting text notifications disabled per game design request
        }

        private IEnumerator NotificationRoutine(string message, float duration)
        {
            notificationText.text = message;
            yield return new WaitForSecondsRealtime(duration);
            notificationText.text = string.Empty;
            notificationCoroutine = null;
        }
    }
}
