using System.Collections.Generic;
using RoguelikeRacing.Kart;
using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// World-affecting item behaviors that need more than "modify my own kart": dropping
    /// a hazard others can drive over, hitting every nearby kart at once, firing a
    /// projectile at whoever's ahead, or swapping places with another kart entirely.
    /// Kept separate from ItemCatalog so the catalog stays pure data
    /// (name/description/effect).
    /// </summary>
    public static class ItemHazards
    {
        const string RootName = "~ItemHazards";

        public static void DropOilSlick(Vector3 position, float slowMultiplier, float slowDuration)
        {
            var hazardGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hazardGO.name = "OilSlick";
            hazardGO.transform.SetParent(GetRoot(), false);
            hazardGO.transform.position = position + Vector3.up * 0.05f;
            hazardGO.transform.localScale = new Vector3(2f, 0.05f, 2f);

            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.08f, 0.06f, 0.05f);
            hazardGO.GetComponent<Renderer>().sharedMaterial = material;

            // Cylinder primitives ship with a CapsuleCollider, which distorts badly under
            // the non-uniform (flat, wide) scale this hazard uses. Swap it for a BoxCollider
            // that actually matches the flattened disc shape.
            Object.Destroy(hazardGO.GetComponent<Collider>());
            var collider = hazardGO.AddComponent<BoxCollider>();
            collider.isTrigger = true;

            var hazard = hazardGO.AddComponent<OilSlickHazard>();
            hazard.SlowMultiplier = slowMultiplier;
            hazard.SlowDuration = slowDuration;
        }

        public static void Shockwave(KartController source, float radius, float slowMultiplier, float slowDuration)
        {
            Collider[] hits = Physics.OverlapSphere(source.transform.position, radius);
            foreach (Collider hit in hits)
            {
                var otherKart = hit.GetComponentInParent<KartController>();
                if (otherKart == null || otherKart == source) continue;

                otherKart.ApplySlow(slowMultiplier, slowDuration);
            }
        }

        /// <summary>
        /// Fires a homing missile at the kart immediately ahead of the source (by
        /// LapTracker.Progress). No-ops if the source is already in the lead --
        /// matches how a shell thrown in 1st place has nothing to chase in most kart
        /// racers, rather than picking an arbitrary target.
        /// </summary>
        public static void FireHomingMissile(KartController source, float speed, float turnRateDegPerSec, float lifetimeSeconds, float slowMultiplier, float slowDuration, float hitRadius)
        {
            KartController target = FindKartAhead(source);
            if (target == null) return;

            var missileGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            missileGO.name = "HomingMissile";
            missileGO.transform.SetParent(GetRoot(), false);
            missileGO.transform.SetPositionAndRotation(
                source.transform.position + source.transform.forward * 2f + Vector3.up * 0.4f,
                source.transform.rotation);
            missileGO.transform.localScale = new Vector3(0.4f, 0.4f, 0.6f);

            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.9f, 0.2f, 0.1f);
            missileGO.GetComponent<Renderer>().sharedMaterial = material;

            Object.Destroy(missileGO.GetComponent<Collider>());
            var trigger = missileGO.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = hitRadius;

            var missile = missileGO.AddComponent<HomingMissile>();
            missile.Initialize(target, source, speed, turnRateDegPerSec, lifetimeSeconds, slowMultiplier, slowDuration);
        }

        /// <summary>Teleport-swaps position and rotation with a random other kart, zeroing both karts' velocity so neither carries over momentum that no longer matches where it ended up.</summary>
        public static void SwapPositions(KartController source)
        {
            KartController other = PickRandomOtherKart(source);
            if (other == null) return;

            (Vector3 position, Quaternion rotation) sourcePose = (source.transform.position, source.transform.rotation);
            (Vector3 position, Quaternion rotation) otherPose = (other.transform.position, other.transform.rotation);

            source.transform.SetPositionAndRotation(otherPose.position, otherPose.rotation);
            other.transform.SetPositionAndRotation(sourcePose.position, sourcePose.rotation);

            source.ResetVelocity();
            other.ResetVelocity();
        }

        static KartController FindKartAhead(KartController source)
        {
            var sourceTracker = source.GetComponent<LapTracker>();
            if (sourceTracker == null) return null;

            KartController best = null;
            int bestProgress = int.MaxValue;

            foreach (KartController candidate in KartController.ActiveKarts)
            {
                if (candidate == source) continue;

                var tracker = candidate.GetComponent<LapTracker>();
                if (tracker == null || tracker.Progress <= sourceTracker.Progress) continue;

                if (tracker.Progress < bestProgress)
                {
                    bestProgress = tracker.Progress;
                    best = candidate;
                }
            }

            return best;
        }

        static KartController PickRandomOtherKart(KartController source)
        {
            var others = new List<KartController>();
            foreach (KartController candidate in KartController.ActiveKarts)
            {
                if (candidate != source) others.Add(candidate);
            }

            return others.Count == 0 ? null : others[Random.Range(0, others.Count)];
        }

        static Transform GetRoot()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null) return existing.transform;
            return new GameObject(RootName).transform;
        }
    }
}
