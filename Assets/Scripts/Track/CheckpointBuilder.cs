using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeRacing.Track
{
    /// <summary>
    /// Places sequential trigger "gates" across the road at evenly spaced points along
    /// the track's centerline (the same points TrackBuilder used to draw the road, so
    /// gates are guaranteed to line up with it). Gate 0 is the start/finish line.
    ///
    /// Visually built as an arch (two side pillars + a top beam, open in the middle) so
    /// it reads as "drive through this" rather than a solid wall blocking the road — the
    /// earlier flat-slab version looked exactly like an obstacle. The actual trigger
    /// volume LapTracker detects is a separate invisible box spanning the full opening,
    /// so lap detection doesn't care which part of the gate the kart drives through.
    /// </summary>
    public static class CheckpointBuilder
    {
        public static List<Checkpoint> BuildCheckpoints(TrackData track, Transform parent, int checkpointCount = 8, float gateHeight = 2.5f)
        {
            var checkpoints = new List<Checkpoint>(checkpointCount);
            var root = new GameObject("Checkpoints").transform;
            root.SetParent(parent, false);

            int pointCount = track.CenterlinePoints.Count;
            int step = Mathf.Max(1, pointCount / checkpointCount);

            for (int i = 0; i < checkpointCount; i++)
            {
                int pointIndex = (i * step) % pointCount;
                Vector3 point = track.CenterlinePoints[pointIndex];
                Vector3 nextPoint = track.CenterlinePoints[(pointIndex + 1) % pointCount];
                Vector3 direction = (nextPoint - point).normalized;
                Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

                bool isFinishLine = i == 0;
                Checkpoint checkpoint = BuildGate(root, point, rotation, track.RoadWidth, gateHeight, i, isFinishLine);
                checkpoints.Add(checkpoint);
            }

            return checkpoints;
        }

        static Checkpoint BuildGate(Transform parent, Vector3 position, Quaternion rotation, float roadWidth, float gateHeight, int index, bool isFinishLine)
        {
            var gateGO = new GameObject(isFinishLine ? "Checkpoint_Finish" : $"Checkpoint_{index}");
            gateGO.transform.SetParent(parent, false);
            gateGO.transform.SetPositionAndRotation(position + Vector3.up * (gateHeight * 0.5f), rotation);

            // Invisible trigger volume spanning the whole opening -- this, not the arch
            // visual below, is what LapTracker actually detects.
            var boxCollider = gateGO.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(roadWidth, gateHeight, 0.6f);
            boxCollider.isTrigger = true;

            var checkpoint = gateGO.AddComponent<Checkpoint>();
            checkpoint.Index = index;
            checkpoint.IsFinishLine = isFinishLine;

            Material material = CreateGateMaterial(isFinishLine);
            BuildArch(gateGO.transform, roadWidth, gateHeight, material);

            return checkpoint;
        }

        static void BuildArch(Transform parent, float roadWidth, float gateHeight, Material material)
        {
            const float pillarThickness = 0.5f;
            const float beamThickness = 0.4f;
            float halfWidth = roadWidth * 0.5f;

            BuildArchPiece(parent, new Vector3(halfWidth - pillarThickness * 0.5f, 0f, 0f),
                new Vector3(pillarThickness, gateHeight, pillarThickness), material, "Pillar_Right");
            BuildArchPiece(parent, new Vector3(-(halfWidth - pillarThickness * 0.5f), 0f, 0f),
                new Vector3(pillarThickness, gateHeight, pillarThickness), material, "Pillar_Left");
            BuildArchPiece(parent, new Vector3(0f, gateHeight * 0.5f - beamThickness * 0.5f, 0f),
                new Vector3(roadWidth, beamThickness, beamThickness), material, "Beam");
        }

        static void BuildArchPiece(Transform parent, Vector3 localPosition, Vector3 scale, Material material, string name)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = scale;
            Object.Destroy(piece.GetComponent<Collider>());
            piece.GetComponent<Renderer>().sharedMaterial = material;
        }

        static Material CreateGateMaterial(bool isFinishLine)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = isFinishLine ? new Color(0.95f, 0.85f, 0.1f) : new Color(0.2f, 0.8f, 0.9f);
            return material;
        }
    }
}
