using UnityEngine;
using Voidbound.Core;
using Voidbound.Player;

namespace Voidbound.Obstacles
{
    /// <summary>
    /// Lethal spike hazard component. Immediate death on contact with single-death guard.
    /// Resets cleanly on player respawn and game reset.
    /// </summary>
    public class SpikeHazard : MonoBehaviour
    {
        [Header("Spike Hazard Settings")]
        [SerializeField] private string hazardName = "Impaled by Spikes";
        [SerializeField] private bool instantKill = true;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer spikeRenderer;
        [SerializeField] private Light spikeLight;
        [SerializeField] private Color lethalColor = new Color(1.0f, 0.15f, 0.15f, 1.0f);

        private bool hasKilled;

        public string HazardName => hazardName;
        public bool InstantKill => instantKill;
        public bool HasKilled => hasKilled;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRespawnRequested += OnRespawnRequested;
                GameManager.Instance.OnGameReset += ResetHazard;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRespawnRequested -= OnRespawnRequested;
                GameManager.Instance.OnGameReset -= ResetHazard;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            ProcessHit(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            ProcessHit(collision.gameObject);
        }

        public void ProcessHit(GameObject target)
        {
            if (hasKilled) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            var deathHandler = target.GetComponent<PlayerDeathHandler>() ?? target.GetComponentInParent<PlayerDeathHandler>();
            if (deathHandler != null)
            {
                hasKilled = true;
                deathHandler.HandleDeath(hazardName);
                return;
            }

            var playerController = target.GetComponent<PlayerController>() ?? target.GetComponentInParent<PlayerController>();
            if (playerController != null || target.CompareTag("Player"))
            {
                hasKilled = true;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnPlayerDied();
                }
            }
        }

        private void OnRespawnRequested(Vector3 position, Quaternion rotation)
        {
            ResetHazard();
        }

        public void ResetHazard()
        {
            hasKilled = false;
        }
    }
}
