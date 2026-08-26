using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// Minimal on-screen lap counter for the player, built with legacy OnGUI so there is
    /// visible feedback for the checkpoint/lap system without pulling in a Canvas/UI
    /// system this early — that belongs to the level-up/item UI work later.
    /// </summary>
    public class RaceHud : MonoBehaviour
    {
        public LapTracker target;

        GUIStyle _style;

        void OnGUI()
        {
            if (target == null) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label);
                _style.fontSize = 28;
                _style.normal.textColor = Color.white;
            }

            GUI.Label(new Rect(20, 20, 300, 50), $"Lap: {target.LapCount}", _style);
        }
    }
}
