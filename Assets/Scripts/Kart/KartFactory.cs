using UnityEngine;

namespace RoguelikeRacing.Kart
{
    /// <summary>
    /// Builds a placeholder kart entirely out of primitives (cube body, capsule cabin,
    /// cylinder wheels) plus the physics it needs to drive. Does NOT attach an input
    /// source: the caller adds either KartInput (local player) or KartAIDriver (AI),
    /// so the same factory serves both without any player/AI branching in here.
    /// </summary>
    public static class KartFactory
    {
        static readonly Color DefaultBodyColor = new Color(0.15f, 0.55f, 0.95f);

        public static GameObject SpawnKart(Vector3 position, Quaternion rotation, Transform parent = null, string name = "Kart", Color? bodyColor = null)
        {
            var kart = new GameObject(name);
            if (parent != null) kart.transform.SetParent(parent, false);
            kart.transform.SetPositionAndRotation(position, rotation);

            BuildVisual(kart.transform, bodyColor ?? DefaultBodyColor);

            var collider = kart.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.5f, 0f);
            collider.size = new Vector3(1.6f, 1f, 2.6f);

            var rb = kart.AddComponent<Rigidbody>();
            rb.mass = 180f;
            rb.drag = 0.5f;
            rb.angularDrag = 4f;
            rb.centerOfMass = new Vector3(0f, -0.3f, 0f);

            kart.AddComponent<KartController>();

            return kart;
        }

        static void BuildVisual(Transform kart, Color bodyColor)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(kart, false);
            body.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            body.transform.localScale = new Vector3(1.6f, 0.6f, 2.6f);
            Object.Destroy(body.GetComponent<Collider>());
            SetColor(body, bodyColor);

            var cabin = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cabin.name = "Cabin";
            cabin.transform.SetParent(kart, false);
            cabin.transform.localPosition = new Vector3(0f, 0.95f, 0.1f);
            cabin.transform.localScale = new Vector3(0.7f, 0.4f, 0.9f);
            Object.Destroy(cabin.GetComponent<Collider>());
            SetColor(cabin, new Color(0.9f, 0.9f, 0.95f));

            SpawnWheel(kart, new Vector3(0.85f, 0.35f, 0.9f));
            SpawnWheel(kart, new Vector3(-0.85f, 0.35f, 0.9f));
            SpawnWheel(kart, new Vector3(0.85f, 0.35f, -0.9f));
            SpawnWheel(kart, new Vector3(-0.85f, 0.35f, -0.9f));
        }

        static void SpawnWheel(Transform parent, Vector3 localPosition)
        {
            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "Wheel";
            wheel.transform.SetParent(parent, false);
            wheel.transform.localPosition = localPosition;
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.transform.localScale = new Vector3(0.35f, 0.25f, 0.35f);
            Object.Destroy(wheel.GetComponent<Collider>());
            SetColor(wheel, new Color(0.05f, 0.05f, 0.05f));
        }

        static void SetColor(GameObject go, Color color)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = color;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
