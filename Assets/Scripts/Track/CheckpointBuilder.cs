using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeRacing.Track
{
    /// <summary>
    /// Places sequential trigger "gates" across the road at evenly spaced points along
    /// the track's centerline (the same points TrackBuilder used to draw the road, so
    /// gates are guaranteed to line up with it). Gate 0 is the start/finish line.
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
            var gateGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gateGO.name = isFinishLine ? "Checkpoint_Finish" : $"Checkpoint_{index}";
            gateGO.transform.SetParent(parent, false);
            gateGO.transform.SetPositionAndRotation(position + Vector3.up * (gateHeight * 0.5f), rotation);
            gateGO.transform.localScale = new Vector3(roadWidth, gateHeight, 0.2f);

            var collider = gateGO.GetComponent<BoxCollider>();
            collider.isTrigger = true;

            var material = new Material(Shader.Find("Standard"));
            material.color = isFinishLine ? new Color(0.95f, 0.85f, 0.1f) : new Color(0.2f, 0.8f, 0.9f);
            gateGO.GetComponent<Renderer>().sharedMaterial = material;

            var checkpoint = gateGO.AddComponent<Checkpoint>();
            checkpoint.Index = index;
            checkpoint.IsFinishLine = isFinishLine;
            return checkpoint;
        }
    }
}
