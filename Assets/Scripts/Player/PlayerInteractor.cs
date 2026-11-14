using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Voidbound.Core;
using Voidbound.Interactions;

namespace Voidbound.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Raycast Settings")]
        [SerializeField] private float interactRange = 3.5f;
        [SerializeField] private LayerMask interactableLayers = ~0; // Default to all layers
        [SerializeField] private Transform raycastOrigin;

        [Header("Audio Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip interactSound;

        private IInteractable currentInteractable;
        private GameObject currentTargetObject;

        public event Action<string> OnPromptUpdated;

        public IInteractable CurrentInteractable => currentInteractable;

        private void Awake()
        {
            if (raycastOrigin == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) raycastOrigin = cam.transform;
                else raycastOrigin = transform;
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                ClearTarget();
                return;
            }

            CheckForInteractable();
            HandleInteractionInput();
        }

        private void CheckForInteractable()
        {
            Ray ray = new Ray(raycastOrigin.position, raycastOrigin.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayers, QueryTriggerInteraction.Collide))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    if (interactable != currentInteractable)
                    {
                        currentInteractable = interactable;
                        currentTargetObject = hit.collider.gameObject;
                        OnPromptUpdated?.Invoke(currentInteractable.GetInteractionPrompt());
                    }
                    return;
                }
            }

            if (currentInteractable != null)
            {
                ClearTarget();
            }
        }

        private void HandleInteractionInput()
        {
            if (currentInteractable != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (interactSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(interactSound, 0.7f);
                }

                currentInteractable.Interact(gameObject);

                // Refresh prompt text if interaction changed state
                if (currentInteractable != null)
                {
                    OnPromptUpdated?.Invoke(currentInteractable.GetInteractionPrompt());
                }
            }
        }

        private void ClearTarget()
        {
            currentInteractable = null;
            currentTargetObject = null;
            OnPromptUpdated?.Invoke(string.Empty);
        }
    }
}
