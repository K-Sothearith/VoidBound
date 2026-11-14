using UnityEngine;

namespace Voidbound.Interactions
{
    /// <summary>
    /// Interface for any world object that can be interacted with by the player (e.g. via 'E' key).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Returns the prompt text to display on the HUD when the player looks at this object.
        /// </summary>
        string GetInteractionPrompt();

        /// <summary>
        /// Called when the player presses the interaction key while targeting this object.
        /// </summary>
        void Interact(GameObject interactor);
    }
}
