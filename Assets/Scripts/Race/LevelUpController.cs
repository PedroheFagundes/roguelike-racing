using System.Collections.Generic;
using RoguelikeRacing.Kart;
using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// On every lap the player completes, pauses and offers a choice of permanent kart
    /// upgrades via PauseChoiceUI. AI karts do not go through this controller at all —
    /// they get a random upgrade auto-applied instead (see GameBootstrap), since there
    /// is no one to show a choice UI to and letting the player out-scale the AI every
    /// lap would make the "roguelike" layer pointless to test.
    /// </summary>
    [RequireComponent(typeof(KartController))]
    [RequireComponent(typeof(LapTracker))]
    public class LevelUpController : MonoBehaviour
    {
        public int optionsPerLevelUp = 3;

        KartController _controller;
        LapTracker _lapTracker;
        PauseChoiceUI _pauseChoiceUI;

        public void Initialize(PauseChoiceUI pauseChoiceUI)
        {
            _pauseChoiceUI = pauseChoiceUI;
        }

        void Awake()
        {
            _controller = GetComponent<KartController>();
            _lapTracker = GetComponent<LapTracker>();
        }

        void OnEnable()
        {
            _lapTracker.LapCompleted += OnLapCompleted;
        }

        void OnDisable()
        {
            _lapTracker.LapCompleted -= OnLapCompleted;
        }

        void OnLapCompleted(int lapCount)
        {
            if (_pauseChoiceUI == null) return;

            List<KartUpgrade> choices = PickRandomUpgrades(optionsPerLevelUp);

            var prompts = new List<ChoicePrompt>(choices.Count);
            foreach (KartUpgrade upgrade in choices)
            {
                prompts.Add(new ChoicePrompt(upgrade.Name, upgrade.Description, () => upgrade.Apply(_controller)));
            }

            _pauseChoiceUI.Open($"Level up! (volta {lapCount}) - escolha um upgrade", prompts);
        }

        static List<KartUpgrade> PickRandomUpgrades(int count)
        {
            var pool = new List<KartUpgrade>(KartUpgradeCatalog.All);
            var picked = new List<KartUpgrade>();

            count = Mathf.Min(count, pool.Count);
            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, pool.Count);
                picked.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return picked;
        }
    }
}
