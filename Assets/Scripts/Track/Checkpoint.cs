using UnityEngine;

namespace RoguelikeRacing.Track
{
    /// <summary>
    /// Marker on a checkpoint gate's trigger collider. Index defines crossing order
    /// (0 = start/finish line). LapTracker uses this to require karts hit gates in order
    /// (0, 1, 2, ..., N-1, back to 0) before a finish-line crossing counts as a lap.
    /// </summary>
    public class Checkpoint : MonoBehaviour
    {
        public int Index;
        public bool IsFinishLine;
    }
}
