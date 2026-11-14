using UnityEngine;

namespace Voidbound.Core
{
    /// <summary>
    /// Lethal boundary that consumes a life and triggers respawn when the player enters.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class KillVolume : MonoBehaviour
    {
        [SerializeField] private string causeOfDeath = "Fell into the Void";

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var deathHandler = other.GetComponent<Voidbound.Player.PlayerDeathHandler>();
            if (deathHandler != null)
            {
                deathHandler.HandleDeath(causeOfDeath);
                return;
            }

            var player = other.GetComponent<Voidbound.Player.PlayerController>();
            if (player != null || other.CompareTag("Player"))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnPlayerDied();
                }
            }
        }
    }
}
