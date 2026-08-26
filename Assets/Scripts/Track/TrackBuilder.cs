using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeRacing.Track
{
    /// <summary>
    /// Result of building a track: the geometry is instantiated directly into the scene,
    /// this struct just carries the data other systems need (spawn point today; lap/AI
    /// waypoints later reuse CenterlinePoints).
    /// </summary>
    public class TrackData
    {
        public List<Vector3> CenterlinePoints;
        public Vector3 StartPosition;
        public Quaternion StartRotation;
    }

    /// <summary>
    /// Builds a minimal closed oval track entirely out of primitives (cubes for road and
    /// walls) so the prototype needs no external art or hand-authored scene content.
    /// </summary>
    public static class TrackBuilder
    {
        public static TrackData BuildOvalTrack(
            Transform parent,
            float radiusX = 34f,
            float radiusZ = 22f,
            int segments = 40,
            float roadWidth = 8f,
            float wallHeight = 1.2f,
            float wallThickness = 0.5f)
        {
            List<Vector3> points = GenerateOvalCenterline(radiusX, radiusZ, segments);

            var trackRoot = new GameObject("Track").transform;
            trackRoot.SetParent(parent, false);

            Material roadMaterial = CreateColorMaterial(new Color(0.25f, 0.25f, 0.28f));
            Material wallMaterial = CreateColorMaterial(new Color(0.75f, 0.15f, 0.15f));
            Material groundMaterial = CreateColorMaterial(new Color(0.10f, 0.45f, 0.15f));
            PhysicMaterial lowFriction = CreateLowFrictionMaterial();

            BuildGroundPlane(trackRoot, radiusX, radiusZ, groundMaterial);

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 a = points[i];
                Vector3 b = points[(i + 1) % points.Count];

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

            Vector3 startDir = (points[1] - points[0]).normalized;
            Vector3 startPos = points[0] - startDir * 2f + Vector3.up * 0.4f;

            return new TrackData
            {
                CenterlinePoints = points,
                StartPosition = startPos,
                StartRotation = Quaternion.LookRotation(startDir, Vector3.up)
            };
        }

        static List<Vector3> GenerateOvalCenterline(float radiusX, float radiusZ, int segments)
        {
            var points = new List<Vector3>(segments);
            for (int i = 0; i < segments; i++)
            {
                float t = (i / (float)segments) * Mathf.PI * 2f;
                points.Add(new Vector3(Mathf.Cos(t) * radiusX, 0f, Mathf.Sin(t) * radiusZ));
            }
            return points;
        }

        static void BuildGroundPlane(Transform parent, float radiusX, float radiusZ, Material material)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(parent, false);
            ground.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(radiusX * 2f + 40f, 1f, radiusZ * 2f + 40f);
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

        static void BuildWall(Transform parent, Vector3 position, Quaternion rotation, float length, float height, float thickness, Material material, PhysicMaterial physicMaterial, string name)
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

        static PhysicMaterial CreateLowFrictionMaterial()
        {
            var material = new PhysicMaterial("TrackWall_LowFriction");
            material.dynamicFriction = 0.05f;
            material.staticFriction = 0.05f;
            material.bounciness = 0.05f;
            material.frictionCombine = PhysicMaterialCombine.Minimum;
            material.bounceCombine = PhysicMaterialCombine.Minimum;
            return material;
        }
    }
}
