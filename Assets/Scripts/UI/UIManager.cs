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
        private static UIManager instance;
        public static UIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<UIManager>();
                }
                return instance;
            }
            private set => instance = value;
        }

        [Header("Menu Panels")]
        [SerializeField] private GameObject startMenuPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject victoryPanel;

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button gameOverRestartButton;
        [SerializeField] private Button victoryRestartButton;
        [SerializeField] private Button victoryQuitButton;

        [Header("HUD Elements")]
        [SerializeField] private Image crosshairImage;
        [SerializeField] private Text interactionPromptText;
        [SerializeField] private Text livesText;
        [SerializeField] private Text sectionText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text controlsTutorialText;
        [SerializeField] private Text notificationText;

        [Header("Victory Screen Elements")]
        [SerializeField] private Text victoryTitleText;
        [SerializeField] private Text victoryTimeText;
        [SerializeField] private Text victoryDeathsText;

        [Header("Post-Processing Blur Reference")]
        [SerializeField] private Volume postProcessVolume;

        private DepthOfField depthOfField;
        private Coroutine notificationCoroutine;

        public GameObject StartMenuPanel => startMenuPanel;
        public GameObject HudPanel => hudPanel;
        public GameObject PauseMenuPanel => pauseMenuPanel;
        public GameObject GameOverPanel => gameOverPanel;
        public GameObject VictoryPanel => victoryPanel;
        public Button VictoryRestartButton => victoryRestartButton;
        public Button VictoryQuitButton => victoryQuitButton;
        public Text VictoryTitleText => victoryTitleText;
        public Text VictoryTimeText => victoryTimeText;
        public Text VictoryDeathsText => victoryDeathsText;

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

            if (victoryRestartButton != null)
            {
                victoryRestartButton.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.RestartGame();
                });
            }

            if (victoryQuitButton != null)
            {
                victoryQuitButton.onClick.AddListener(() =>
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });
            }
        }

        public void HandleGameStateChanged(GameState state)
        {
            if (startMenuPanel != null) startMenuPanel.SetActive(state == GameState.StartMenu);
            if (hudPanel != null) hudPanel.SetActive(state == GameState.Playing);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(state == GameState.Paused);
            if (gameOverPanel != null) gameOverPanel.SetActive(state == GameState.GameOver);
            if (victoryPanel != null) victoryPanel.SetActive(state == GameState.Victory);

            if (state == GameState.Victory)
            {
                if (victoryTitleText != null) victoryTitleText.text = "EXTRACTION SUCCESSFUL";
                if (victoryTimeText != null && GameManager.Instance != null)
                {
                    victoryTimeText.text = $"COMPLETION TIME: {GameManager.FormatTime(GameManager.Instance.ElapsedTime)}";
                }
                if (victoryDeathsText != null && GameManager.Instance != null)
                {
                    victoryDeathsText.text = $"DEATHS: {GameManager.Instance.DeathCount}";
                }
            }

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
            if (notificationText == null || string.IsNullOrEmpty(message)) return;

            // Only display activity notification text for Checkpoint activation per game design
            if (!message.StartsWith("Checkpoint", System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (notificationCoroutine != null)
            {
                StopCoroutine(notificationCoroutine);
            }
            notificationCoroutine = StartCoroutine(NotificationRoutine(message, duration));
        }

        private IEnumerator NotificationRoutine(string message, float duration)
        {
            notificationText.gameObject.SetActive(true);
            notificationText.text = message;
            yield return new WaitForSecondsRealtime(duration);
            notificationText.text = string.Empty;
            notificationText.gameObject.SetActive(false);
            notificationCoroutine = null;
        }
    }
}
