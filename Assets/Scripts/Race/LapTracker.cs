using System;
using RoguelikeRacing.Track;
using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// Requires a kart to cross checkpoint gates in order (0, 1, 2, ..., N-1, back to 0)
    /// before a finish-line (index 0) crossing counts as a completed lap. This is what
    /// stops corner-cutting or driving backwards through the finish line from scoring.
    ///
    /// Attached to every kart, player and AI alike — lap tracking isn't player-specific,
    /// and race position later will need every kart's progress anyway.
    /// </summary>
    public class LapTracker : MonoBehaviour
    {
        public int LapCount { get; private set; }
        public int TotalCheckpointCount { get; private set; }

        /// <summary>
        /// Monotonically increasing "how far around the track, in total" value used by
        /// RaceStandings to rank karts: laps completed count far more than a single
        /// checkpoint, and within the same lap a higher checkpoint index means further
        /// along. Approximate (doesn't account for distance within a checkpoint gap),
        /// which is good enough for a 3-kart prototype leaderboard.
        /// </summary>
        public int Progress => LapCount * Mathf.Max(1, TotalCheckpointCount) + _nextExpectedCheckpointIndex;

        /// <summary>Raised with the new lap count each time a lap completes.</summary>
        public event Action<int> LapCompleted;

        int _nextExpectedCheckpointIndex = 1;

        /// <summary>Must be called once after spawning, before any checkpoint can be counted.</summary>
        public void Initialize(int totalCheckpointCount)
        {
            TotalCheckpointCount = totalCheckpointCount;
            _nextExpectedCheckpointIndex = totalCheckpointCount > 1 ? 1 : 0;
        }

        void OnTriggerEnter(Collider other)
        {
            if (TotalCheckpointCount <= 0) return;

            var checkpoint = other.GetComponent<Checkpoint>();
            if (checkpoint == null) return;
            if (checkpoint.Index != _nextExpectedCheckpointIndex) return;

            if (checkpoint.IsFinishLine)
            {
                LapCount++;
                LapCompleted?.Invoke(LapCount);
            }

            _nextExpectedCheckpointIndex = (_nextExpectedCheckpointIndex + 1) % TotalCheckpointCount;
        }
    }
}
