using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Voidbound.Core
{
    public enum GameState
    {
        StartMenu,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    /// <summary>
    /// Master coordinator for game states, lives, checkpoints, timing, and run resets.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Configuration")]
        [SerializeField] private int maxLives = 3;
        [SerializeField] private float respawnDelay = 1.0f;

        [Header("State (Read-Only)")]
        [SerializeField] private GameState currentState = GameState.StartMenu;
        [SerializeField] private int currentLives;
        [SerializeField] private float elapsedTime;
        [SerializeField] private string currentSectionName = "Section 1: Movement Tutorial";

        // Checkpoint state
        private Vector3 initialSpawnPosition;
        private Quaternion initialSpawnRotation;
        private Vector3 activeCheckpointPosition;
        private Quaternion activeCheckpointRotation;
        private bool hasCustomCheckpoint;

        // Death guard to prevent overlapping deductions
        private bool isRespawning;
        private float respawnCooldownTimer;

        // Public Events
        public event Action<GameState> OnGameStateChanged;
        public event Action<int, int> OnLivesChanged;
        public event Action<float> OnTimerUpdated;
        public event Action<string> OnSectionChanged;
        public event Action<string, float> OnNotification;
        public event Action<Vector3, Quaternion> OnRespawnRequested;
        public event Action OnGameReset;

        public GameState CurrentState => currentState;
        public int CurrentLives => currentLives;
        public int MaxLives => maxLives;
        public float ElapsedTime => elapsedTime;
        public string CurrentSectionName => currentSectionName;
        public bool IsPlaying => currentState == GameState.Playing;
        public bool HasCustomCheckpoint => hasCustomCheckpoint;
        public Vector3 ActiveCheckpointPosition => activeCheckpointPosition;
        public Quaternion ActiveCheckpointRotation => activeCheckpointRotation;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            currentLives = maxLives;
        }

        private void Start()
        {
            // Set initial state to StartMenu
            SetState(GameState.StartMenu);
            UpdateCursorState();
        }

        private void Update()
        {
            // Handle timer when playing
            if (currentState == GameState.Playing)
            {
                elapsedTime += Time.deltaTime;
                OnTimerUpdated?.Invoke(elapsedTime);
            }

            // Handle pause toggle
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (currentState == GameState.Playing)
                {
                    PauseGame();
                }
                else if (currentState == GameState.Paused)
                {
                    ResumeGame();
                }
            }

            // Update respawn cooldown if active
            if (isRespawning)
            {
                respawnCooldownTimer -= Time.unscaledDeltaTime;
                if (respawnCooldownTimer <= 0f)
                {
                    isRespawning = false;
                }
            }
        }

        public void RegisterSpawnPoint(Vector3 position, Quaternion rotation)
        {
            initialSpawnPosition = position;
            initialSpawnRotation = rotation;
            if (!hasCustomCheckpoint)
            {
                activeCheckpointPosition = position;
                activeCheckpointRotation = rotation;
            }
        }

        public void SetCheckpoint(Vector3 position, Quaternion rotation, string checkpointName = "Checkpoint")
        {
            activeCheckpointPosition = position;
            activeCheckpointRotation = rotation;
            hasCustomCheckpoint = true;
            ShowNotification($"Checkpoint Activated: {checkpointName}", 2.5f);
        }

        public void SetSection(string sectionName)
        {
            currentSectionName = sectionName;
            OnSectionChanged?.Invoke(sectionName);
        }

        public void ShowNotification(string message, float duration = 3.0f)
        {
            OnNotification?.Invoke(message, duration);
        }

        public void StartGame()
        {
            currentLives = maxLives;
            elapsedTime = 0f;
            hasCustomCheckpoint = false;
            activeCheckpointPosition = initialSpawnPosition;
            activeCheckpointRotation = initialSpawnRotation;
            isRespawning = false;

            Time.timeScale = 1.0f;
            SetState(GameState.Playing);
            OnLivesChanged?.Invoke(currentLives, maxLives);
            OnRespawnRequested?.Invoke(initialSpawnPosition, initialSpawnRotation);
            OnGameReset?.Invoke();
            ShowNotification("Expedition Initiated. Reach the Extraction Portal.", 3.0f);
        }

        public void PauseGame()
        {
            if (currentState != GameState.Playing) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (currentState != GameState.Paused) return;
            Time.timeScale = 1.0f;
            SetState(GameState.Playing);
        }

        public void RestartGame()
        {
            Time.timeScale = 1.0f;
            currentLives = maxLives;
            elapsedTime = 0f;
            hasCustomCheckpoint = false;
            activeCheckpointPosition = initialSpawnPosition;
            activeCheckpointRotation = initialSpawnRotation;
            isRespawning = false;

            SetState(GameState.Playing);
            OnLivesChanged?.Invoke(currentLives, maxLives);
            OnRespawnRequested?.Invoke(initialSpawnPosition, initialSpawnRotation);
            OnGameReset?.Invoke();
            ShowNotification("Course Reset. All lives restored.", 2.5f);
        }

        /// <summary>
        /// Consumes one life and triggers respawn at the latest checkpoint.
        /// Protected by a single-death guard to eliminate duplicate triggers.
        /// </summary>
        public void OnPlayerDied()
        {
            if (isRespawning || currentState != GameState.Playing)
            {
                return;
            }

            isRespawning = true;
            respawnCooldownTimer = respawnDelay;

            currentLives = Mathf.Max(0, currentLives - 1);
            OnLivesChanged?.Invoke(currentLives, maxLives);

            if (currentLives <= 0)
            {
                TriggerGameOver();
            }
            else
            {
                ShowNotification($"Vessel Destroyed! {currentLives} {(currentLives == 1 ? "life" : "lives")} remaining.", 2.0f);
                OnRespawnRequested?.Invoke(activeCheckpointPosition, activeCheckpointRotation);
            }
        }

        public void TriggerGameOver()
        {
            Time.timeScale = 0f;
            SetState(GameState.GameOver);
            ShowNotification("Expedition Failed — All Lives Consumed.", 4.0f);
        }

        public void TriggerVictory()
        {
            Time.timeScale = 0f;
            SetState(GameState.Victory);
            ShowNotification($"Extraction Complete! Final Time: {FormatTime(elapsedTime)}", 5.0f);
        }

        public static string FormatTime(float timeInSeconds)
        {
            if (timeInSeconds < 0f) timeInSeconds = 0f;
            int minutes = (int)(timeInSeconds / 60f);
            float seconds = timeInSeconds % 60f;
            int sec = (int)seconds;
            int centis = (int)((seconds - sec) * 100f);
            return $"{minutes:00}:{sec:00}.{centis:00}";
        }

        private void SetState(GameState newState)
        {
            currentState = newState;
            UpdateCursorState();
            OnGameStateChanged?.Invoke(newState);
        }

        private void UpdateCursorState()
        {
            if (currentState == GameState.Playing)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
