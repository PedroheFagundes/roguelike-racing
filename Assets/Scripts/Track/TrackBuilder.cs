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
            float roadWidth = 12f,
            float wallHeight = 1.2f,
            float wallThickness = 0.8f)
        {
            var trackRoot = new GameObject("Track").transform;
            trackRoot.SetParent(parent, false);

            Material roadMaterial = CreateColorMaterial(new Color(0.25f, 0.25f, 0.28f));
            Material wallMaterial = CreateColorMaterial(new Color(0.75f, 0.15f, 0.15f));
            Material groundMaterial = CreateColorMaterial(new Color(0.10f, 0.45f, 0.15f));
            PhysicsMaterial lowFriction = CreateLowFrictionMaterial();

            BuildGroundPlane(trackRoot, centerlinePoints, groundMaterial);
            BuildRoadMesh(trackRoot, centerlinePoints, roadWidth, roadMaterial);

            int count = centerlinePoints.Count;
            float halfSpan = roadWidth * 0.5f + wallThickness * 0.5f;

            for (int i = 0; i < count; i++)
            {
                Vector3 a = centerlinePoints[i];
                Vector3 b = centerlinePoints[(i + 1) % count];

                Vector3 segmentDir = (b - a).normalized;
                float segmentLength = Vector3.Distance(a, b);
                Vector3 mid = (a + b) * 0.5f;
                Quaternion rot = Quaternion.LookRotation(segmentDir, Vector3.up);

                BuildWall(trackRoot, mid + rot * new Vector3(halfSpan, wallHeight * 0.5f, 0f), rot,
                    segmentLength, wallHeight, wallThickness, wallMaterial, lowFriction, $"WallOuter_{i}");
                BuildWall(trackRoot, mid + rot * new Vector3(-halfSpan, wallHeight * 0.5f, 0f), rot,
                    segmentLength, wallHeight, wallThickness, wallMaterial, lowFriction, $"WallInner_{i}");
            }

            // Two straight wall segments meeting at a vertex leave a gap on sharper
            // corners (each is oriented to its own segment's direction, so their ends
            // don't line up) -- this is what let karts "leak" off track on Estadio's
            // hairpins and the Tecnica track. A round post at every vertex, on both
            // sides, bridges the gap regardless of how sharp the corner is, without
            // needing to miter the wall boxes.
            for (int i = 0; i < count; i++)
            {
                Vector3 prev = centerlinePoints[(i - 1 + count) % count];
                Vector3 curr = centerlinePoints[i];
                Vector3 next = centerlinePoints[(i + 1) % count];

                Vector3 dirIn = (curr - prev).normalized;
                Vector3 dirOut = (next - curr).normalized;
                Vector3 bisector = (dirIn + dirOut).normalized;
                if (bisector.sqrMagnitude < 0.0001f) bisector = dirOut;

                Quaternion rot = Quaternion.LookRotation(bisector, Vector3.up);

                BuildCornerPost(trackRoot, curr + rot * new Vector3(halfSpan, wallHeight * 0.5f, 0f),
                    wallHeight, wallThickness, wallMaterial, lowFriction, $"CornerOuter_{i}");
                BuildCornerPost(trackRoot, curr - rot * new Vector3(halfSpan, wallHeight * 0.5f, 0f),
                    wallHeight, wallThickness, wallMaterial, lowFriction, $"CornerInner_{i}");
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
        public static List<Vector3> GenerateOvalCenterline(float radiusX = 50f, float radiusZ = 34f, int segments = 48)
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
        public static List<Vector3> GenerateStadiumCenterline(float straightLength = 70f, float turnRadius = 22f, int segmentsPerTurn = 16)
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
            float[] radii = { 45f, 36f, 45f, 27f, 39f, 21f, 33f, 45f, 30f, 18f, 36f, 45f, 24f, 39f, 33f, 45f };

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

        // Ground plane's top face sits at Y=0 (see BuildGroundPlane); lifting the road
        // mesh slightly above it avoids the two coplanar surfaces z-fighting/flickering.
        const float RoadSurfaceHeight = 0.08f;

        /// <summary>
        /// Builds the driving surface as a single continuous ribbon mesh along the
        /// centerline, instead of one independent flat box per segment. Independent
        /// boxes each face their own segment's direction, so on a sharp corner their
        /// edges don't line up -- visible gaps/overlaps in the surface, and real gaps in
        /// the collider a kart can catch on while cornering (this is what made corners
        /// "pessimo de dirigir" even after the wall-slide fix). A shared-vertex mesh
        /// strip has no seams by construction, at any corner sharpness -- the standard
        /// technique for spline-following race track geometry.
        /// </summary>
        static void BuildRoadMesh(Transform parent, List<Vector3> centerlinePoints, float roadWidth, Material material)
        {
            int count = centerlinePoints.Count;
            float halfWidth = roadWidth * 0.5f;
            Vector3 heightOffset = Vector3.up * RoadSurfaceHeight;

            var vertices = new Vector3[count * 2];
            var uvs = new Vector2[count * 2];

            for (int i = 0; i < count; i++)
            {
                Vector3 prev = centerlinePoints[(i - 1 + count) % count];
                Vector3 curr = centerlinePoints[i] + heightOffset;
                Vector3 next = centerlinePoints[(i + 1) % count];

                Vector3 dirIn = (centerlinePoints[i] - prev).normalized;
                Vector3 dirOut = (next - centerlinePoints[i]).normalized;
                Vector3 forward = (dirIn + dirOut).normalized;
                if (forward.sqrMagnitude < 0.0001f) forward = dirOut;

                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

                vertices[i * 2] = curr - right * halfWidth;
                vertices[i * 2 + 1] = curr + right * halfWidth;

                uvs[i * 2] = new Vector2(0f, i);
                uvs[i * 2 + 1] = new Vector2(1f, i);
            }

            var triangles = new int[count * 6];
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                int bl = i * 2;
                int br = i * 2 + 1;
                int tl = next * 2;
                int tr = next * 2 + 1;

                int t = i * 6;
                triangles[t] = bl;
                triangles[t + 1] = tl;
                triangles[t + 2] = br;
                triangles[t + 3] = br;
                triangles[t + 4] = tl;
                triangles[t + 5] = tr;
            }

            var mesh = new Mesh { name = "RoadMesh" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var roadGO = new GameObject("Road");
            roadGO.transform.SetParent(parent, false);

            roadGO.AddComponent<MeshFilter>().sharedMesh = mesh;
            roadGO.AddComponent<MeshRenderer>().sharedMaterial = material;
            roadGO.AddComponent<MeshCollider>().sharedMesh = mesh;
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

        static void BuildCornerPost(Transform parent, Vector3 position, float height, float wallThickness, Material material, PhysicsMaterial physicMaterial, string name)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = name;
            post.transform.SetParent(parent, false);
            post.transform.position = position;

            // Deliberately generous diameter (wider than the wall) so it overlaps both
            // adjoining wall segments even when the bisector math above is only
            // approximate. Default cylinder mesh has radius 0.5, so scale.x/z = diameter.
            float diameter = wallThickness * 1.8f;
            post.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            post.GetComponent<Renderer>().sharedMaterial = material;

            // Cylinder primitives ship with a CapsuleCollider, which is exactly what we
            // want here (round profile bridges two straight walls at any angle) --
            // unlike the flattened oil slick hazard, this one is left as-is.
            var collider = post.GetComponent<CapsuleCollider>();
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
