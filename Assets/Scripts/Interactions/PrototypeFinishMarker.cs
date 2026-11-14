using UnityEngine;
using Voidbound.Core;

namespace Voidbound.Interactions
{
    /// <summary>
    /// Milestone marker for the completion of the Phase 01 Tutorial Platforming Area.
    /// Note: Per specification, this does NOT trigger full game victory.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PrototypeFinishMarker : MonoBehaviour
    {
        [Header("Marker Settings")]
        [SerializeField] private string markerTitle = "Tutorial Platforming Area";
        [SerializeField] private Renderer beaconRenderer;
        [SerializeField] private Light beaconLight;
        [SerializeField] private Color beaconColor = new Color(0.2f, 1f, 0.8f, 1f);

        [Header("Animation")]
        [SerializeField] private float rotationSpeed = 45f;
        [SerializeField] private float bobbingSpeed = 2f;
        [SerializeField] private float bobbingHeight = 0.25f;

        private Vector3 initialPosition;
        private bool hasTriggered;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
            initialPosition = transform.position;
        }

        private void Start()
        {
            if (beaconRenderer != null && beaconRenderer.material != null)
            {
                beaconRenderer.material.color = beaconColor;
                if (beaconRenderer.material.HasProperty("_EmissionColor"))
                {
                    beaconRenderer.material.SetColor("_EmissionColor", beaconColor * 2.0f);
                    beaconRenderer.material.EnableKeyword("_EMISSION");
                }
            }
            if (beaconLight != null)
            {
                beaconLight.color = beaconColor;
            }
        }

        private void Update()
        {
            // Idle hover and spin
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            float newY = initialPosition.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered) return;

            if (other.CompareTag("Player") || other.GetComponent<Voidbound.Player.PlayerController>() != null)
            {
                hasTriggered = true;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ShowNotification(
                        $"[PHASE 01 COMPLETE] {markerTitle} reached! Foundations verified.", 5.0f);
                }
            }
        }
    }
}
