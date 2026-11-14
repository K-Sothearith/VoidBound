using UnityEngine;
using Voidbound.Player;

namespace Voidbound.Core
{
    [RequireComponent(typeof(Collider))]
    public class SectionTrigger : MonoBehaviour
    {
        [Header("Section Configuration")]
        [SerializeField] private string sectionName = "Section 2: Hammer Swing";
        [SerializeField] private string notificationText = "Entering Section 2: Hammer Swing";
        [SerializeField] private float notificationDuration = 3.0f;
        [SerializeField] private bool triggerOnce = true;

        private bool hasTriggered;

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
            if (triggerOnce && hasTriggered) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                hasTriggered = true;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetSection(sectionName);
                    if (!string.IsNullOrEmpty(notificationText))
                    {
                        GameManager.Instance.ShowNotification(notificationText, notificationDuration);
                    }
                }
            }
        }

        public void ResetTrigger()
        {
            hasTriggered = false;
        }
    }
}
