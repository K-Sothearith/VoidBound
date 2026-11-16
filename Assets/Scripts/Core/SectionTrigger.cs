using UnityEngine;
using Voidbound.Player;

namespace Voidbound.Core
{
    [RequireComponent(typeof(Collider))]
    public class SectionTrigger : MonoBehaviour
    {
        [Header("Section Configuration")]
        [SerializeField] private string sectionName = "Section 2: Hammer Swing";
        [SerializeField] private bool triggerOnce = true;

        private bool hasTriggered;

        public string SectionName => sectionName;

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
            var manager = GameManager.Instance;
            if (manager == null || !manager.IsPlaying) return;
            // A previously visited entrance must still restore the label after respawn/backtracking.
            if (triggerOnce && hasTriggered && manager.CurrentSectionName == sectionName) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                hasTriggered = true;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetSection(sectionName);
                }
            }
        }

        public void ResetTrigger()
        {
            hasTriggered = false;
        }

        private void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnGameReset += ResetTrigger;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnGameReset -= ResetTrigger;
        }
    }
}
