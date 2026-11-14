using UnityEngine;
using Voidbound.Core;

namespace Voidbound.Interactions
{
    public class PrototypeTerminal : MonoBehaviour, IInteractable
    {
        [Header("Terminal Configuration")]
        [SerializeField] private string terminalName = "Void Terminal Alpha";
        [SerializeField] private bool allowToggle = true;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer screenRenderer;
        [SerializeField] private Light statusLight;
        [SerializeField] private Color offlineColor = new Color(1f, 0.5f, 0.1f, 1f); // Amber
        [SerializeField] private Color onlineColor = new Color(0.1f, 0.95f, 1f, 1f); // Neon Cyan

        [Header("Audio Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip activateSound;

        private bool isActivated;

        public bool IsActivated => isActivated;

        private void Start()
        {
            UpdateVisuals();
        }

        public string GetInteractionPrompt()
        {
            if (!isActivated)
            {
                return $"Press [E] to Access {terminalName}";
            }
            return allowToggle ? $"Press [E] to Deactivate {terminalName}" : $"{terminalName} [ONLINE]";
        }

        public void Interact(GameObject interactor)
        {
            if (isActivated && !allowToggle)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ShowNotification($"{terminalName} is already fully synchronized.", 2.0f);
                }
                return;
            }

            isActivated = !isActivated;
            UpdateVisuals();

            if (audioSource != null && activateSound != null)
            {
                audioSource.PlayOneShot(activateSound, 0.8f);
            }

            if (GameManager.Instance != null)
            {
                string statusMsg = isActivated
                    ? $"[SYSTEM] {terminalName} Activated. Neural Link Established."
                    : $"[SYSTEM] {terminalName} Deactivated.";
                GameManager.Instance.ShowNotification(statusMsg, 2.5f);
            }
        }

        private void UpdateVisuals()
        {
            Color currentColor = isActivated ? onlineColor : offlineColor;

            if (screenRenderer != null && screenRenderer.material != null)
            {
                screenRenderer.material.color = currentColor;
                if (screenRenderer.material.HasProperty("_EmissionColor"))
                {
                    screenRenderer.material.SetColor("_EmissionColor", currentColor * 2.0f);
                    screenRenderer.material.EnableKeyword("_EMISSION");
                }
            }

            if (statusLight != null)
            {
                statusLight.color = currentColor;
                statusLight.intensity = isActivated ? 3.0f : 1.0f;
            }
        }
    }
}
