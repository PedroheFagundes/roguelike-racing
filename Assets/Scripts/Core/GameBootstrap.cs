using RoguelikeRacing.CameraRig;
using RoguelikeRacing.Kart;
using RoguelikeRacing.Track;
using UnityEngine;

namespace RoguelikeRacing.Core
{
    /// <summary>
    /// Spawns the whole step-1 prototype (track, kart, chase camera, light) purely from
    /// code via RuntimeInitializeOnLoadMethod. This means the .unity scene file itself can
    /// stay empty/default: nothing here depends on hand-wired scene content or prefabs,
    /// which keeps the scene file simple and safe to hand-author or regenerate.
    /// </summary>
    public static class GameBootstrap
    {
        const string RootName = "~Bootstrap";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            if (GameObject.Find(RootName) != null) return;

            var root = new GameObject(RootName);
            Object.DontDestroyOnLoad(root);

            BuildLighting(root.transform);

            TrackData track = TrackBuilder.BuildOvalTrack(root.transform);
            GameObject kart = KartFactory.SpawnKart(track.StartPosition, track.StartRotation, root.transform);

            BuildChaseCamera(root.transform, kart.transform, track.StartPosition);
        }

        static void BuildLighting(Transform root)
        {
            var sunGO = new GameObject("Sun");
            sunGO.transform.SetParent(root, false);
            sunGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.4f);
        }

        static void BuildChaseCamera(Transform root, Transform kartTransform, Vector3 startPosition)
        {
            var cameraGO = new GameObject("ChaseCamera");
            cameraGO.transform.SetParent(root, false);
            cameraGO.transform.position = startPosition + Vector3.up * 3f;

            var camera = cameraGO.AddComponent<Camera>();
            camera.nearClipPlane = 0.1f;
            cameraGO.AddComponent<AudioListener>();

            var chaseCamera = cameraGO.AddComponent<ChaseCamera>();
            chaseCamera.target = kartTransform;
        }
    }
}
