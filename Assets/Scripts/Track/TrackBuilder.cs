using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeRacing.Track
{
    /// <summary>
    /// Result of building a track: the geometry is instantiated directly into the scene,
    /// this struct just carries the data other systems need (spawn point, lap/AI
    /// waypoints via CenterlinePoints, road width for placing checkpoints/item boxes).
    /// </summary>
    public class TrackData
    {
        public List<Vector3> CenterlinePoints;
        public Vector3 StartPosition;
        public Quaternion StartRotation;
        public float RoadWidth;
    }

    /// <summary>
    /// Builds a closed track entirely out of primitives (cubes for road and walls) from
    /// any closed polyline of centerline points, so the prototype needs no external art
    /// or hand-authored scene content. Centerline shape and "build road from centerline"
    /// are deliberately separate: TrackCatalog picks which generator to use, this class
    /// only cares that the points form a closed, non-self-intersecting loop.
    /// </summary>
    public static class TrackBuilder
    {
        public static TrackData Build(
            List<Vector3> centerlinePoints,
            Transform parent,
            float roadWidth = 8f,
            float wallHeight = 1.2f,
            float wallThickness = 0.5f)
        {
            var trackRoot = new GameObject("Track").transform;
            trackRoot.SetParent(parent, false);

            Material roadMaterial = CreateColorMaterial(new Color(0.25f, 0.25f, 0.28f));
            Material wallMaterial = CreateColorMaterial(new Color(0.75f, 0.15f, 0.15f));
            Material groundMaterial = CreateColorMaterial(new Color(0.10f, 0.45f, 0.15f));
            PhysicsMaterial lowFriction = CreateLowFrictionMaterial();

            BuildGroundPlane(trackRoot, centerlinePoints, groundMaterial);

            for (int i = 0; i < centerlinePoints.Count; i++)
            {
                Vector3 a = centerlinePoints[i];
                Vector3 b = centerlinePoints[(i + 1) % centerlinePoints.Count];

                Vector3 segmentDir = (b - a).normalized;
                float segmentLength = Vector3.Distance(a, b);
                Vector3 mid = (a + b) * 0.5f;
                Quaternion rot = Quaternion.LookRotation(segmentDir, Vector3.up);

                BuildRoadSegment(trackRoot, mid, rot, segmentLength, roadWidth, roadMaterial, i);

                float halfSpan = roadWidth * 0.5f + wallThickness * 0.5f;
                BuildWall(trackRoot, mid + rot * new Vector3(halfSpan, wallHeight * 0.5f, 0f), rot,
                    segmentLength, wallHeight, wallThickness, wallMaterial, lowFriction, $"WallOuter_{i}");
                BuildWall(trackRoot, mid + rot * new Vector3(-halfSpan, wallHeight * 0.5f, 0f), rot,
                    segmentLength, wallHeight, wallThickness, wallMaterial, lowFriction, $"WallInner_{i}");
            }

            Vector3 startDir = (centerlinePoints[1] - centerlinePoints[0]).normalized;
            Vector3 startPos = centerlinePoints[0] - startDir * 2f + Vector3.up * 0.4f;

            return new TrackData
            {
                CenterlinePoints = centerlinePoints,
                StartPosition = startPos,
                StartRotation = Quaternion.LookRotation(startDir, Vector3.up),
                RoadWidth = roadWidth
            };
        }

        /// <summary>Elongated ellipse — wide, continuous curves, no sharp corners.</summary>
        public static List<Vector3> GenerateOvalCenterline(float radiusX = 34f, float radiusZ = 22f, int segments = 40)
        {
            var points = new List<Vector3>(segments);
            for (int i = 0; i < segments; i++)
            {
                float t = (i / (float)segments) * Mathf.PI * 2f;
                points.Add(new Vector3(Mathf.Cos(t) * radiusX, 0f, Mathf.Sin(t) * radiusZ));
            }
            return points;
        }

        /// <summary>
        /// "Discorectangle" (running-track) shape: two long straights joined by a
        /// semicircular hairpin at each end. Built as two explicit straight points plus
        /// two arcs, then deduplicated where they meet.
        /// </summary>
        public static List<Vector3> GenerateStadiumCenterline(float straightLength = 44f, float turnRadius = 16f, int segmentsPerTurn = 14)
        {
            float halfStraight = straightLength * 0.5f;
            var points = new List<Vector3>();

            points.Add(new Vector3(-halfStraight, 0f, turnRadius));
            points.Add(new Vector3(halfStraight, 0f, turnRadius));
            AppendArc(points, new Vector3(halfStraight, 0f, 0f), turnRadius, 90f, -90f, segmentsPerTurn);
            points.Add(new Vector3(-halfStraight, 0f, -turnRadius));
            AppendArc(points, new Vector3(-halfStraight, 0f, 0f), turnRadius, -90f, -270f, segmentsPerTurn);

            return DedupeClosedLoop(points);
        }

        /// <summary>
        /// Technical/zigzag loop: radius alternates sharply between corners while the
        /// angle around the center still increases monotonically once around a full
        /// circle, which guarantees a closed, non-self-intersecting loop regardless of
        /// how much the radius jumps between neighbours (no arc math or hand-placed
        /// coordinates to get wrong).
        /// </summary>
        public static List<Vector3> GenerateTechnicalCenterline()
        {
            float[] radii = { 30f, 24f, 30f, 18f, 26f, 14f, 22f, 30f, 20f, 12f, 24f, 30f, 16f, 26f, 22f, 30f };

            var points = new List<Vector3>(radii.Length);
            for (int i = 0; i < radii.Length; i++)
            {
                float angle = (i / (float)radii.Length) * Mathf.PI * 2f;
                points.Add(new Vector3(Mathf.Cos(angle) * radii[i], 0f, Mathf.Sin(angle) * radii[i]));
            }
            return points;
        }

        static void AppendArc(List<Vector3> points, Vector3 center, float radius, float startAngleDeg, float endAngleDeg, int segments)
        {
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angleRad = Mathf.Deg2Rad * Mathf.Lerp(startAngleDeg, endAngleDeg, t);
                points.Add(center + new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad)) * radius);
            }
        }

        static List<Vector3> DedupeClosedLoop(List<Vector3> points)
        {
            var result = new List<Vector3>();
            foreach (Vector3 p in points)
            {
                if (result.Count == 0 || Vector3.Distance(result[result.Count - 1], p) > 0.05f)
                {
                    result.Add(p);
                }
            }
            if (result.Count > 1 && Vector3.Distance(result[result.Count - 1], result[0]) < 0.05f)
            {
                result.RemoveAt(result.Count - 1);
            }
            return result;
        }

        static void BuildGroundPlane(Transform parent, List<Vector3> points, Material material)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (Vector3 p in points)
            {
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.z);
                maxZ = Mathf.Max(maxZ, p.z);
            }

            const float padding = 40f;
            Vector3 center = new Vector3((minX + maxX) * 0.5f, -0.5f, (minZ + maxZ) * 0.5f);
            Vector3 size = new Vector3((maxX - minX) + padding, 1f, (maxZ - minZ) + padding);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(parent, false);
            ground.transform.localPosition = center;
            ground.transform.localScale = size;
            ground.GetComponent<Renderer>().sharedMaterial = material;
        }

        static void BuildRoadSegment(Transform parent, Vector3 position, Quaternion rotation, float length, float width, Material material, int index)
        {
            var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = $"Road_{index}";
            road.transform.SetParent(parent, false);
            road.transform.SetPositionAndRotation(position, rotation);
            road.transform.localScale = new Vector3(width, 0.2f, length + 0.1f);
            road.GetComponent<Renderer>().sharedMaterial = material;
        }

        static void BuildWall(Transform parent, Vector3 position, Quaternion rotation, float length, float height, float thickness, Material material, PhysicsMaterial physicMaterial, string name)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.SetPositionAndRotation(position, rotation);
            wall.transform.localScale = new Vector3(thickness, height, length + thickness);
            wall.GetComponent<Renderer>().sharedMaterial = material;

            var collider = wall.GetComponent<BoxCollider>();
            collider.sharedMaterial = physicMaterial;
        }

        static Material CreateColorMaterial(Color color)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = color;
            return material;
        }

        static PhysicsMaterial CreateLowFrictionMaterial()
        {
            var material = new PhysicsMaterial("TrackWall_LowFriction");
            material.dynamicFriction = 0.05f;
            material.staticFriction = 0.05f;
            material.bounciness = 0.05f;
            material.frictionCombine = PhysicsMaterialCombine.Minimum;
            material.bounceCombine = PhysicsMaterialCombine.Minimum;
            return material;
        }
    }
}
