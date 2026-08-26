using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// Legacy OnGUI draws everything in fixed screen pixels, which looks tiny on
    /// anything bigger than a modest window — the menus (PauseChoiceUI, RaceSetupUI,
    /// RaceHud) were sized assuming a small reference viewport and never scaled up for
    /// bigger screens/monitors.
    ///
    /// Call Begin() first thing in OnGUI, then use Width/Height instead of
    /// Screen.width/Screen.height for every Rect. Everything drawn in that space gets
    /// scaled up uniformly (buttons AND text together, since this scales the whole GUI
    /// matrix) to fill the real screen, while never shrinking below the original design
    /// on smaller viewports (Factor is clamped to a minimum of 1).
    /// </summary>
    public static class OnGuiScale
    {
        const float ReferenceHeight = 600f;

        public static float Factor => Mathf.Max(1f, Screen.height / ReferenceHeight);
        public static float Width => Screen.width / Factor;
        public static float Height => Screen.height / Factor;

        public static void Begin()
        {
            GUIUtility.ScaleAroundPivot(new Vector2(Factor, Factor), Vector2.zero);
        }
    }
}
