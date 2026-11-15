using UnityEngine;

namespace Voidbound.Core
{
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Checkpoint Settings")]
        [SerializeField] private string checkpointName = "Checkpoint";
        [SerializeField] private string sectionName = "";
        [SerializeField] private Transform spawnPointOverride;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private Light indicatorLight;
        [SerializeField] private Color inactiveColor = new Color(0.3f, 0.3f, 0.4f, 1f);
        [SerializeField] private Color activeColor = new Color(0.1f, 0.9f, 1f, 1f);

        private bool isActivated;

        public bool IsActivated => isActivated;
        public string CheckpointName => checkpointName;
        public string SectionName => sectionName;
        public Vector3 RespawnPosition => spawnPointOverride != null ? spawnPointOverride.position : transform.position;
        public Quaternion RespawnRotation => spawnPointOverride != null ? spawnPointOverride.rotation : transform.rotation;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
            UpdateVisuals(false);
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameReset += ResetCheckpoint;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameReset -= ResetCheckpoint;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isActivated) return;

            if (other.CompareTag("Player") || other.GetComponent<Voidbound.Player.PlayerController>() != null)
            {
                ActivateCheckpoint();
            }
        }

        public void ActivateCheckpoint()
        {
            isActivated = true;
            UpdateVisuals(true);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetCheckpoint(RespawnPosition, RespawnRotation, checkpointName);
                if (!string.IsNullOrEmpty(sectionName))
                {
                    GameManager.Instance.SetSection(sectionName);
                }
            }
        }

        public void ResetCheckpoint()
        {
            isActivated = false;
            UpdateVisuals(false);
        }

        private void UpdateVisuals(bool active)
        {
            Color targetColor = active ? activeColor : inactiveColor;
            if (indicatorRenderer != null && indicatorRenderer.material != null)
            {
                indicatorRenderer.material.color = targetColor;
                if (indicatorRenderer.material.HasProperty("_EmissionColor"))
                {
                    indicatorRenderer.material.SetColor("_EmissionColor", active ? activeColor * 1.5f : Color.black);
                    if (active) indicatorRenderer.material.EnableKeyword("_EMISSION");
                    else indicatorRenderer.material.DisableKeyword("_EMISSION");
                }
            }
            if (indicatorLight != null)
            {
                indicatorLight.color = targetColor;
                indicatorLight.intensity = active ? 2.5f : 0.5f;
            }
        }
    }
}
