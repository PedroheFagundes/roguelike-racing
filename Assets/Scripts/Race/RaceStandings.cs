using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// Tracks every kart's LapTracker and ranks them by Progress on demand. Only the
    /// player's position is shown today (RaceHud), but every kart is registered since
    /// ranking needs everyone's progress to know where any one kart stands.
    /// </summary>
    public class RaceStandings : MonoBehaviour
    {
        readonly List<LapTracker> _trackers = new List<LapTracker>();

        public int TotalKarts => _trackers.Count;

        public void Register(LapTracker tracker)
        {
            _trackers.Add(tracker);
        }

        /// <summary>1-based rank (1 = leading). Ties keep registration order.</summary>
        public int GetPosition(LapTracker tracker)
        {
            int rank = 1;
            int progress = tracker.Progress;

            foreach (LapTracker other in _trackers)
            {
                if (other == tracker) continue;
                if (other.Progress > progress) rank++;
            }

            return rank;
        }
    }
}
