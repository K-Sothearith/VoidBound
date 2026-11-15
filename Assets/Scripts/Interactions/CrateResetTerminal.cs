using UnityEngine;
using Voidbound.Core;

namespace Voidbound.Interactions
{
    /// <summary>
    /// Interactive terminal inside Section 6 allowing the player to manually reset crates
    /// back to their starting positions if they get misplaced or wedged, preventing softlocks.
    /// Implements IInteractable to hook directly into PlayerInteractor.
    /// </summary>
    public class CrateResetTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] private string promptMessage = "Reset Puzzle Crates";
        [SerializeField] private PushableCrate[] cratesToReset;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip resetSound;

        public string GetInteractionPrompt()
        {
            return $"Press E to {promptMessage}";
        }

        public void SetCrates(PushableCrate[] crates)
        {
            cratesToReset = crates;
        }

        public void Interact(GameObject player)
        {
            ResetCrates();
        }

        public void ResetCrates()
        {
            if (cratesToReset != null && cratesToReset.Length > 0)
            {
                foreach (var crate in cratesToReset)
                {
                    if (crate != null) crate.ResetPosition();
                }
            }
            else
            {
                var allCrates = Object.FindObjectsByType<PushableCrate>(FindObjectsSortMode.None);
                foreach (var crate in allCrates)
                {
                    crate.ResetPosition();
                }
            }

            if (audioSource != null && resetSound != null)
            {
                audioSource.PlayOneShot(resetSound, 0.7f);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ShowNotification("Puzzle Crates Reset to Starting Coordinates.", 2.5f);
            }
        }
    }
}
