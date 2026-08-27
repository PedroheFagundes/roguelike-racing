using System.Collections.Generic;
using RoguelikeRacing.Track;
using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// Places rotating item box triggers at a handful of points along the track's
    /// centerline (the same points TrackBuilder used to draw the road, so boxes stay in
    /// sync with it automatically), alternating left/right of the racing line and offset
    /// from the checkpoint gates so picking one up is a small, optional detour.
    /// </summary>
    public static class ItemBoxBuilder
    {
        public static List<ItemBox> BuildItemBoxes(
            TrackData track, Transform parent, PauseChoiceUI pauseChoiceUI, GameObject playerKart,
            int boxCount = 5, float? lateralOffset = null)
        {
            // Scale with the actual road width by default instead of a fixed constant,
            // so boxes stay a sensible detour off the racing line regardless of track.
            float offset = lateralOffset ?? track.RoadWidth * 0.3f;

            var boxes = new List<ItemBox>(boxCount);
            var root = new GameObject("ItemBoxes").transform;
            root.SetParent(parent, false);

            int pointCount = track.CenterlinePoints.Count;
            int step = Mathf.Max(1, pointCount / boxCount);

            for (int i = 0; i < boxCount; i++)
            {
                int pointIndex = (i * step + step / 2) % pointCount;
                Vector3 point = track.CenterlinePoints[pointIndex];
                Vector3 nextPoint = track.CenterlinePoints[(pointIndex + 1) % pointCount];
                Vector3 direction = (nextPoint - point).normalized;
                Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

                float side = (i % 2 == 0) ? offset : -offset;
                Vector3 position = point + rotation * Vector3.right * side + Vector3.up * 0.6f;

                boxes.Add(BuildBox(root, position, pauseChoiceUI, playerKart));
            }

            return boxes;
        }

        static ItemBox BuildBox(Transform parent, Vector3 position, PauseChoiceUI pauseChoiceUI, GameObject playerKart)
        {
            var boxGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxGO.name = "ItemBox";
            boxGO.transform.SetParent(parent, false);
            boxGO.transform.position = position;
            boxGO.transform.localScale = Vector3.one * 1.2f;

            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.95f, 0.75f, 0.05f);
            boxGO.GetComponent<Renderer>().sharedMaterial = material;

            var collider = boxGO.GetComponent<BoxCollider>();
            collider.isTrigger = true;

            var box = boxGO.AddComponent<ItemBox>();
            box.Initialize(pauseChoiceUI, playerKart);
            return box;
        }
    }
}
