using UnityEngine;
using Voidbound.Core;

namespace Voidbound.Player
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerDeathHandler : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float deathFreezeDuration = 0.5f;

        private PlayerController playerController;
        private bool isDead;
        private float respawnTimer;

        public bool IsDead => isDead;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRespawnRequested += OnRespawnRequested;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRespawnRequested -= OnRespawnRequested;
            }
        }

        private void Update()
        {
            if (isDead)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0f)
                {
                    isDead = false;
                    if (playerController != null)
                    {
                        playerController.SetInputEnabled(true);
                    }
                }
            }
        }

        public void HandleDeath(string reason = "Death")
        {
            if (isDead) return;

            isDead = true;
            respawnTimer = deathFreezeDuration;

            if (playerController != null)
            {
                playerController.SetInputEnabled(false);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerDied();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var hazard = other.GetComponent<Voidbound.Obstacles.Hazard>();
            if (hazard == null) hazard = other.GetComponentInParent<Voidbound.Obstacles.Hazard>();
            if (hazard == null)
            {
                var bridge = other.GetComponent<Voidbound.Obstacles.HazardTriggerBridge>();
                if (bridge != null) hazard = bridge.ParentHazard;
            }

            if (hazard != null)
            {
                hazard.ProcessHit(gameObject);
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var hazard = hit.collider.GetComponent<Voidbound.Obstacles.Hazard>();
            if (hazard == null) hazard = hit.collider.GetComponentInParent<Voidbound.Obstacles.Hazard>();
            if (hazard == null)
            {
                var bridge = hit.collider.GetComponent<Voidbound.Obstacles.HazardTriggerBridge>();
                if (bridge != null) hazard = bridge.ParentHazard;
            }

            if (hazard != null)
            {
                hazard.ProcessHit(gameObject);
            }
        }

        private void OnRespawnRequested(Vector3 position, Quaternion rotation)
        {
            isDead = false;
            if (playerController != null)
            {
                playerController.SetInputEnabled(true);
            }
        }
    }
}
