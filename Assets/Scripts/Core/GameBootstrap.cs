using System.Collections.Generic;
using RoguelikeRacing.CameraRig;
using RoguelikeRacing.Kart;
using RoguelikeRacing.Race;
using RoguelikeRacing.Track;
using UnityEngine;

namespace RoguelikeRacing.Core
{
    /// <summary>
    /// Spawns the whole prototype (track, player kart, AI karts, chase camera, light)
    /// purely from code via RuntimeInitializeOnLoadMethod. This means the .unity scene
    /// file itself can stay empty/default: nothing here depends on hand-wired scene
    /// content or prefabs, which keeps the scene file simple and safe to hand-author or
    /// regenerate.
    /// </summary>
    public static class GameBootstrap
    {
        const string RootName = "~Bootstrap";

        static readonly Color PlayerColor = new Color(0.15f, 0.55f, 0.95f);
        static readonly Color AiColor1 = new Color(0.9f, 0.25f, 0.15f);
        static readonly Color AiColor2 = new Color(0.95f, 0.8f, 0.1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            if (GameObject.Find(RootName) != null) return;

            var root = new GameObject(RootName);
            Object.DontDestroyOnLoad(root);

            BuildLighting(root.transform);

            TrackData track = TrackBuilder.BuildOvalTrack(root.transform);
            List<Checkpoint> checkpoints = CheckpointBuilder.BuildCheckpoints(track, root.transform);
            PauseChoiceUI pauseChoiceUI = BuildPauseChoiceUI(root.transform);

            GameObject playerKart = KartFactory.SpawnKart(track.StartPosition, track.StartRotation, root.transform, "PlayerKart", PlayerColor);
            playerKart.AddComponent<KartInput>();

            var playerLapTracker = playerKart.AddComponent<LapTracker>();
            playerLapTracker.Initialize(checkpoints.Count);
            playerLapTracker.LapCompleted += lap => Debug.Log($"Player completed lap {lap}");

            var levelUpController = playerKart.AddComponent<LevelUpController>();
            levelUpController.Initialize(pauseChoiceUI);

            ItemBoxBuilder.BuildItemBoxes(track, root.transform, pauseChoiceUI, playerKart);

            // Staggered grid behind the player, offset to either side so they don't
            // spawn stacked on top of each other (and each other's Rigidbody).
            SpawnAIKart(track, root.transform, "AIKart_1", AiColor1, checkpoints.Count, indexOffsetBehindStart: 3, lateralOffset: 2.5f);
            SpawnAIKart(track, root.transform, "AIKart_2", AiColor2, checkpoints.Count, indexOffsetBehindStart: 3, lateralOffset: -2.5f);

            BuildChaseCamera(root.transform, playerKart.transform, track.StartPosition);
            BuildRaceHud(root.transform, playerLapTracker);
        }

        static void SpawnAIKart(TrackData track, Transform parent, string name, Color color, int checkpointCount, int indexOffsetBehindStart, float lateralOffset)
        {
            int count = track.CenterlinePoints.Count;
            int spawnIndex = ((-indexOffsetBehindStart % count) + count) % count;

            Vector3 point = track.CenterlinePoints[spawnIndex];
            Vector3 nextPoint = track.CenterlinePoints[(spawnIndex + 1) % count];
            Vector3 direction = (nextPoint - point).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            Vector3 position = point + rotation * Vector3.right * lateralOffset + Vector3.up * 0.4f;

            GameObject aiKart = KartFactory.SpawnKart(position, rotation, parent, name, color);

            var driver = aiKart.AddComponent<KartAIDriver>();
            driver.Initialize(track.CenterlinePoints, startWaypointIndex: (spawnIndex + 1) % count);

            var aiController = aiKart.GetComponent<KartController>();
            var lapTracker = aiKart.AddComponent<LapTracker>();
            lapTracker.Initialize(checkpointCount);
            lapTracker.LapCompleted += lap =>
            {
                // AI has no choice UI to show, so it just auto-applies a random upgrade
                // from the same pool the player picks from — otherwise the player would
                // out-scale the AI every lap and the roguelike layer wouldn't be testable.
                KartUpgrade upgrade = KartUpgradeCatalog.All[Random.Range(0, KartUpgradeCatalog.All.Count)];
                upgrade.Apply(aiController);
                Debug.Log($"{name} completed lap {lap}, auto-upgraded: {upgrade.Name}");
            };
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

        static void BuildRaceHud(Transform root, LapTracker playerLapTracker)
        {
            var hudGO = new GameObject("RaceHud");
            hudGO.transform.SetParent(root, false);

            var hud = hudGO.AddComponent<RaceHud>();
            hud.target = playerLapTracker;
        }

        static PauseChoiceUI BuildPauseChoiceUI(Transform root)
        {
            var go = new GameObject("PauseChoiceUI");
            go.transform.SetParent(root, false);
            return go.AddComponent<PauseChoiceUI>();
        }
    }
}
