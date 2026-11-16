using UnityEngine;
using Voidbound.Core;
using Voidbound.Player;

namespace Voidbound.Obstacles
{
    /// <summary>
    /// Lethal spike hazard component. Immediate death on contact with single-death guard.
    /// Resets cleanly on player respawn and game reset.
    /// </summary>
    public class SpikeHazard : Hazard
    {
        [Header("Visual Feedback")]
        [SerializeField] private Renderer spikeRenderer;
        [SerializeField] private Light spikeLight;
        [SerializeField] private Color lethalColor = new Color(1.0f, 0.15f, 0.15f, 1.0f);

        private bool hasKilled;

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

        public override void ProcessHit(GameObject target)
        {
            // Diagnostic history only. The manager owns death debounce, not the hazard.
            if (TryProcessHit(target)) hasKilled = true;
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
