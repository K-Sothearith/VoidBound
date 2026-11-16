using UnityEngine;
using UnityEngine.EventSystems;
using Voidbound.Core;

namespace Voidbound.UI
{
    /// <summary>Presentation only: observes existing game events; never delays or changes game state.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class UIVisualFeedback : MonoBehaviour
    {
        [SerializeField] private CanvasGroup[] menuGroups;
        [SerializeField] private UnityEngine.UI.Image feedbackWash;
        [SerializeField] private UnityEngine.UI.Text respawnText;
        [SerializeField] private CanvasGroup checkpointNotice;
        [SerializeField] private UnityEngine.UI.Text notificationText;
        [SerializeField] private UnityEngine.UI.Image interactionBackdrop;
        [SerializeField] private UnityEngine.UI.Text interactionText;
        [SerializeField, Min(0.01f)] private float menuFadeDuration = .22f;

        private GameManager manager;
        private CanvasGroup fadingMenu;
        private int previousLives;
        private float washRemaining;
        private float statusRemaining;
        private Color washColor;
        private bool awaitingRespawn;
        private Vector2 checkpointPosition;

        private void Start()
        {
            manager = GameManager.Instance;
            if (manager == null) return;
            previousLives = manager.CurrentLives;
            if (checkpointNotice != null) checkpointPosition = ((RectTransform)checkpointNotice.transform).anchoredPosition;
            manager.OnGameStateChanged += HandleState;
            manager.OnLivesChanged += HandleLives;
            manager.OnRespawnRequested += HandleRespawn;
            manager.OnNotification += HandleNotification;
            manager.OnGameReset += ResetFeedback;
            ResetFeedback();
            HandleState(manager.CurrentState);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (fadingMenu != null && fadingMenu.gameObject.activeInHierarchy)
                fadingMenu.alpha = Mathf.MoveTowards(fadingMenu.alpha, 1f, dt / menuFadeDuration);
            washRemaining = Mathf.Max(0, washRemaining - dt);
            if (feedbackWash != null)
            {
                Color color = washColor;
                color.a *= Mathf.Clamp01(washRemaining / .45f);
                feedbackWash.color = color;
            }
            statusRemaining = Mathf.Max(0, statusRemaining - dt);
            if (respawnText != null)
            {
                Color color = respawnText.color;
                color.a = Mathf.Clamp01(statusRemaining / .25f);
                respawnText.color = color;
            }
            if (checkpointNotice != null && notificationText != null)
            {
                bool visible = notificationText.gameObject.activeSelf;
                checkpointNotice.gameObject.SetActive(visible);
                if (visible)
                {
                    checkpointNotice.alpha = Mathf.MoveTowards(checkpointNotice.alpha, 1f, dt / .16f);
                    var rect = (RectTransform)checkpointNotice.transform;
                    rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, checkpointPosition, Mathf.Clamp01(dt * 18f));
                }
            }
            if (interactionBackdrop != null && interactionText != null)
                interactionBackdrop.enabled = interactionText.gameObject.activeInHierarchy && !string.IsNullOrEmpty(interactionText.text);
        }

        private void HandleState(GameState state)
        {
            if (state == GameState.StartMenu || state == GameState.GameOver || state == GameState.Victory)
                ResetFeedback();
            fadingMenu = null;
            if (menuGroups != null)
                foreach (var group in menuGroups)
                {
                    if (group == null) continue;
                    if (group.gameObject.activeInHierarchy)
                    {
                        group.alpha = .05f;
                        fadingMenu = group;
                    }
                    else group.alpha = 1f;
                }
            // Retain the existing callbacks and activation semantics; only select the visible menu's control.
            if (EventSystem.current != null)
            {
                var button = fadingMenu != null ? fadingMenu.GetComponentInChildren<UnityEngine.UI.Button>() : null;
                EventSystem.current.SetSelectedGameObject(button != null ? button.gameObject : null);
            }
        }

        private void HandleLives(int current, int max)
        {
            if (current < previousLives)
            {
                washColor = new Color(.85f, .12f, .18f, .12f);
                washRemaining = .45f;
                awaitingRespawn = current > 0;
            }
            previousLives = current;
        }

        private void HandleRespawn(Vector3 position, Quaternion rotation)
        {
            if (!awaitingRespawn) return;
            awaitingRespawn = false;
            if (respawnText != null)
            {
                respawnText.text = "VESSEL LOST  /  SIGNAL REACQUIRED";
                statusRemaining = 1.2f;
            }
        }

        private void HandleNotification(string message, float duration)
        {
            if (checkpointNotice == null || string.IsNullOrEmpty(message) ||
                !message.StartsWith("Checkpoint", System.StringComparison.OrdinalIgnoreCase)) return;
            checkpointNotice.alpha = 0;
            ((RectTransform)checkpointNotice.transform).anchoredPosition = checkpointPosition + Vector2.up * 8f;
        }

        private void ResetFeedback()
        {
            awaitingRespawn = false;
            washRemaining = statusRemaining = 0;
            if (manager != null) previousLives = manager.CurrentLives;
            if (feedbackWash != null) feedbackWash.color = Color.clear;
            if (respawnText != null) respawnText.color = new Color(1f, .65f, .6f, 0);
            if (notificationText != null)
            {
                notificationText.text = string.Empty;
                notificationText.gameObject.SetActive(false);
            }
            if (checkpointNotice != null) checkpointNotice.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (manager == null) return;
            manager.OnGameStateChanged -= HandleState;
            manager.OnLivesChanged -= HandleLives;
            manager.OnRespawnRequested -= HandleRespawn;
            manager.OnNotification -= HandleNotification;
            manager.OnGameReset -= ResetFeedback;
        }
    }
}
