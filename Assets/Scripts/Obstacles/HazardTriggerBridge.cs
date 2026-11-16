using UnityEngine;

namespace Voidbound.Obstacles
{
    /// <summary>
    /// Forwards trigger events from child collider objects to the parent Hazard component.
    /// </summary>
    public class HazardTriggerBridge : MonoBehaviour
    {
        [SerializeField] private Hazard parentHazard;

        public Hazard ParentHazard => parentHazard;

        public void Initialize(Hazard hazard)
        {
            parentHazard = hazard;
        }

        private void Awake()
        {
            if (parentHazard == null)
            {
                parentHazard = GetComponentInParent<Hazard>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (parentHazard != null)
            {
                parentHazard.ProcessHit(other.gameObject);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            // A player already overlapping when immunity expires must become vulnerable again.
            if (parentHazard != null) parentHazard.ProcessHit(other.gameObject);
        }
    }
}
