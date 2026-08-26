using System.Collections.Generic;
using RoguelikeRacing.CameraRig;
using RoguelikeRacing.Kart;
using RoguelikeRacing.Race;
using RoguelikeRacing.Track;
using UnityEngine;

namespace RoguelikeRacing.Core
{
    /// <summary>
    /// Shows the pre-race setup screen (track + character pick), then spawns the whole
    /// race (track, player kart, AI karts, chase camera, light) from code once the
    /// player confirms. This means the .unity scene file itself can stay empty/default:
    /// nothing here depends on hand-wired scene content or prefabs, which keeps the
    /// scene file simple and safe to hand-author or regenerate.
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

            var setupGO = new GameObject("RaceSetupUI");
            setupGO.transform.SetParent(root.transform, false);
            var setupUI = setupGO.AddComponent<RaceSetupUI>();
            setupUI.RaceConfirmed += (trackLayout, characterIndex) => BuildRace(root.transform, trackLayout, characterIndex);
        }

        static void BuildRace(Transform root, TrackLayout trackLayout, int playerCharacterIndex)
        {
            TrackData track = TrackBuilder.Build(trackLayout.BuildCenterline(), root);
            List<Checkpoint> checkpoints = CheckpointBuilder.BuildCheckpoints(track, root);
            PauseChoiceUI pauseChoiceUI = BuildPauseChoiceUI(root);
            RaceStandings standings = BuildRaceStandings(root);

            CharacterDefinition playerCharacter = CharacterCatalog.All[playerCharacterIndex];
            GameObject playerKart = KartFactory.SpawnKart(track.StartPosition, track.StartRotation, root, "PlayerKart", playerCharacter.BodyColor);
            playerCharacter.ApplyTo(playerKart.GetComponent<KartController>());
            playerKart.AddComponent<KartInventory>();
            playerKart.AddComponent<KartInput>();

            var wrongWayDetector = playerKart.AddComponent<WrongWayDetector>();
            wrongWayDetector.Initialize(track.CenterlinePoints);

            var playerLapTracker = playerKart.AddComponent<LapTracker>();
            playerLapTracker.Initialize(checkpoints.Count);
            playerLapTracker.LapCompleted += lap => Debug.Log($"Player completed lap {lap}");
            standings.Register(playerLapTracker);

            var levelUpController = playerKart.AddComponent<LevelUpController>();
            levelUpController.Initialize(pauseChoiceUI);

            ItemBoxBuilder.BuildItemBoxes(track, root, pauseChoiceUI, playerKart);

            // AI gets whichever characters the player didn't pick, so every race still
            // features all 3 archetypes regardless of what the player chose. This assumes
            // exactly 2 AI karts for exactly 3 characters (CharacterCatalog.All.Count - 1);
            // growing either roster independently would need revisiting this pairing.
            var aiCharacters = new List<CharacterDefinition>();
            for (int i = 0; i < CharacterCatalog.All.Count; i++)
            {
                if (i != playerCharacterIndex) aiCharacters.Add(CharacterCatalog.All[i]);
            }

            // Staggered grid behind the player, offset to either side so they don't
            // spawn stacked on top of each other (and each other's Rigidbody).
            SpawnAIKart(track, root, "AIKart_1", aiCharacters[0], checkpoints.Count, indexOffsetBehindStart: 3, lateralOffset: 3.5f, standings: standings);
            SpawnAIKart(track, root, "AIKart_2", aiCharacters[1], checkpoints.Count, indexOffsetBehindStart: 3, lateralOffset: -3.5f, standings: standings);

            BuildChaseCamera(root, playerKart.transform, track.StartPosition);
            BuildRaceHud(root, playerLapTracker, standings, wrongWayDetector, playerKart.GetComponent<KartInventory>());
        }

        static void SpawnAIKart(TrackData track, Transform parent, string name, CharacterDefinition character, int checkpointCount, int indexOffsetBehindStart, float lateralOffset, RaceStandings standings)
        {
            int count = track.CenterlinePoints.Count;
            int spawnIndex = ((-indexOffsetBehindStart % count) + count) % count;

            Vector3 point = track.CenterlinePoints[spawnIndex];
            Vector3 nextPoint = track.CenterlinePoints[(spawnIndex + 1) % count];
            Vector3 direction = (nextPoint - point).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            Vector3 position = point + rotation * Vector3.right * lateralOffset + Vector3.up * 0.4f;

            GameObject aiKart = KartFactory.SpawnKart(position, rotation, parent, name, character.BodyColor);

            var aiController = aiKart.GetComponent<KartController>();
            character.ApplyTo(aiController);
            aiKart.AddComponent<KartInventory>();

            var driver = aiKart.AddComponent<KartAIDriver>();
            driver.Initialize(track.CenterlinePoints, startWaypointIndex: (spawnIndex + 1) % count);

            var lapTracker = aiKart.AddComponent<LapTracker>();
            lapTracker.Initialize(checkpointCount);
            standings.Register(lapTracker);
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

        static void BuildRaceHud(Transform root, LapTracker playerLapTracker, RaceStandings standings, WrongWayDetector wrongWayDetector, KartInventory inventory)
        {
            var hudGO = new GameObject("RaceHud");
            hudGO.transform.SetParent(root, false);

            var hud = hudGO.AddComponent<RaceHud>();
            hud.target = playerLapTracker;
            hud.standings = standings;
            hud.wrongWayDetector = wrongWayDetector;
            hud.inventory = inventory;
        }

        static PauseChoiceUI BuildPauseChoiceUI(Transform root)
        {
            var go = new GameObject("PauseChoiceUI");
            go.transform.SetParent(root, false);
            return go.AddComponent<PauseChoiceUI>();
        }

        static RaceStandings BuildRaceStandings(Transform root)
        {
            var go = new GameObject("RaceStandings");
            go.transform.SetParent(root, false);
            return go.AddComponent<RaceStandings>();
        }
    }
}
