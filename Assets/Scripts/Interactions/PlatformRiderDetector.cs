using UnityEngine;

namespace Voidbound.Interactions
{
    /// <summary>
    /// Trigger volume detector that relays player entry/exit to the MovingPlatform
    /// to enable smooth, unparented riding transport.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlatformRiderDetector : MonoBehaviour
    {
        [SerializeField] private MovingPlatform movingPlatform;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            if (movingPlatform == null)
            {
                movingPlatform = GetComponentInParent<MovingPlatform>();
            }
        }

        public void Initialize(MovingPlatform platform)
        {
            movingPlatform = platform;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (movingPlatform == null) return;

            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc == null) cc = other.GetComponentInParent<CharacterController>();

            if (cc != null)
            {
                movingPlatform.SetRider(cc);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (movingPlatform == null) return;

            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc == null) cc = other.GetComponentInParent<CharacterController>();

            if (cc != null)
            {
                movingPlatform.RemoveRider(cc);
            }
        }
    }
}
