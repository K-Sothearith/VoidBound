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
            TriggerKill(other.gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            TriggerKill(other.gameObject);
        }

        public void TriggerKill(GameObject target)
        {
            var deathHandler = target.GetComponent<Voidbound.Player.PlayerDeathHandler>();
            if (deathHandler != null)
            {
                deathHandler.HandleDeath(causeOfDeath);
                return;
            }

            var player = target.GetComponent<Voidbound.Player.PlayerController>();
            if (player != null || target.CompareTag("Player"))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnPlayerDied();
                }
            }
        }
    }
}
