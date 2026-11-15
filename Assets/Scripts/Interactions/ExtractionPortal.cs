using UnityEngine;
using Voidbound.Core;
using Voidbound.Player;

namespace Voidbound.Interactions
{
    /// <summary>
    /// Destination extraction portal at the terminus of Section 7.
    /// Entering the portal activates the victory sequence, stopping the timer,
    /// locking player controls, and displaying the mission summary.
    /// </summary>
    public class ExtractionPortal : MonoBehaviour
    {
        [Header("Visual Effects")]
        [SerializeField] private Transform portalCore;
        [SerializeField] private Light portalLight;
        [SerializeField] private float rotationSpeed = 60f;
        [SerializeField] private float pulseFrequency = 2.0f;
        [SerializeField] private float baseIntensity = 3.0f;
        [SerializeField] private float pulseAmplitude = 1.0f;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip victorySound;

        private bool hasTriggeredVictory;

        public bool HasTriggeredVictory => hasTriggeredVictory;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameReset += ResetPortal;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameReset -= ResetPortal;
            }
        }

        private void Update()
        {
            // Rotate the glowing energy core
            if (portalCore != null)
            {
                portalCore.Rotate(Vector3.forward * (rotationSpeed * Time.unscaledDeltaTime));
            }

            // Pulse the emission light
            if (portalLight != null)
            {
                float pulse = Mathf.Sin(Time.unscaledTime * pulseFrequency * Mathf.PI);
                portalLight.intensity = baseIntensity + pulse * pulseAmplitude;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggeredVictory) return;

            if (IsPlayer(other))
            {
                TriggerExtraction();
            }
        }

        private bool IsPlayer(Collider col)
        {
            return col.CompareTag("Player") ||
                   col.GetComponent<CharacterController>() != null ||
                   col.GetComponentInParent<PlayerController>() != null;
        }

        public void TriggerExtraction()
        {
            if (hasTriggeredVictory) return;
            hasTriggeredVictory = true;

            if (audioSource != null && victorySound != null)
            {
                audioSource.PlayOneShot(victorySound);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerVictory();
            }
        }

        public void ResetPortal()
        {
            hasTriggeredVictory = false;
        }

        public void Configure(Transform core, Light light)
        {
            portalCore = core;
            portalLight = light;
        }
    }
}
