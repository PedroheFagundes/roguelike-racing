using RoguelikeRacing.Kart;
using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// World-affecting item behaviors that need more than "modify my own kart": dropping
    /// a hazard others can drive over, or hitting every nearby kart at once. Kept
    /// separate from ItemCatalog so the catalog stays pure data (name/description/effect).
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

        static Transform GetRoot()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null) return existing.transform;
            return new GameObject(RootName).transform;
        }
    }
}
