using RoguelikeRacing.Kart;
using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// Ground hazard dropped by the Oil Slick item: slows the first kart that drives
    /// over it (unless shielded, see KartController.ApplySlow), then despawns. Also
    /// despawns on its own after a while so a track doesn't accumulate unused slicks.
    /// </summary>
    public class OilSlickHazard : MonoBehaviour
    {
        public float SlowMultiplier = 0.5f;
        public float SlowDuration = 2f;
        public float LifetimeSeconds = 15f;

        void Start()
        {
            Destroy(gameObject, LifetimeSeconds);
        }

        void OnTriggerEnter(Collider other)
        {
            var kart = other.GetComponentInParent<KartController>();
            if (kart == null) return;

            kart.ApplySlow(SlowMultiplier, SlowDuration);
            Destroy(gameObject);
        }
    }
}
