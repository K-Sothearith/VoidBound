using UnityEngine;
using Voidbound.Core;
using Voidbound.Player;

namespace Voidbound.Obstacles
{
    /// <summary>
    /// Base hazard component. Any contact with the player causes immediate death and life consumption.
    /// Protected by GameManager and PlayerDeathHandler death guards against multi-frame duplicate deductions.
    /// </summary>
    public class Hazard : MonoBehaviour
    {
        [Header("Hazard Configuration")]
        [SerializeField] private string hazardName = "Lethal Hazard";
        [SerializeField] private bool instantKill = true;
        [SerializeField] private int damageAmount = 1;

        public string HazardName => hazardName;
        public bool InstantKill => instantKill;
        public int DamageAmount => damageAmount;

        private void OnTriggerEnter(Collider other)
        {
            ProcessHit(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            ProcessHit(collision.gameObject);
        }

        public virtual void ProcessHit(GameObject target)
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            var deathHandler = target.GetComponent<PlayerDeathHandler>();
            if (deathHandler == null)
            {
                deathHandler = target.GetComponentInParent<PlayerDeathHandler>();
            }

            if (deathHandler != null)
            {
                deathHandler.HandleDeath(hazardName);
                return;
            }

            var playerController = target.GetComponent<PlayerController>();
            if (playerController == null)
            {
                playerController = target.GetComponentInParent<PlayerController>();
            }

            if (playerController != null || target.CompareTag("Player"))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnPlayerDied();
                }
            }
        }
    }
}
