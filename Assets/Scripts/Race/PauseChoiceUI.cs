using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// Pauses the whole game (Time.timeScale = 0) and shows a simple OnGUI panel with
    /// the given choices; picking one applies it and resumes. Shared by level-up
    /// (step 4) and item boxes (step 5) so both go through the same pause/apply path
    /// instead of each rolling their own.
    ///
    /// This is the v1/single-player default documented in docs/DESIGN_DECISIONS.md:
    /// pausing everything is the simplest thing that works when there's only one human
    /// to wait on. Moving to multiplayer later means adding a per-decision timeout and
    /// no longer touching Time.timeScale here — it does not mean rewriting how choices
    /// are built or applied (see ChoicePrompt).
    /// </summary>
    public class PauseChoiceUI : MonoBehaviour
    {
        string _headerText = "Choose one";
        List<ChoicePrompt> _options;
        float _previousTimeScale = 1f;

        public bool IsOpen => _options != null;

        public void Open(string header, List<ChoicePrompt> options)
        {
            if (options == null || options.Count == 0) return;

            _headerText = header;
            _options = options;
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        void OnGUI()
        {
            if (_options == null) return;

            const float panelWidth = 460f;
            const float buttonHeight = 70f;
            const float buttonSpacing = 12f;
            float panelHeight = 90f + _options.Count * (buttonHeight + buttonSpacing);

            var panelRect = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth, panelHeight);

            GUI.Box(panelRect, GUIContent.none);

            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + 10f, panelRect.width - 20f, 40f), _headerText, headerStyle);

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };

            for (int i = 0; i < _options.Count; i++)
            {
                ChoicePrompt option = _options[i];
                var buttonRect = new Rect(
                    panelRect.x + 15f,
                    panelRect.y + 60f + i * (buttonHeight + buttonSpacing),
                    panelWidth - 30f, buttonHeight);

                if (GUI.Button(buttonRect, $"  {option.Title}\n  {option.Description}", buttonStyle))
                {
                    Choose(option);
                    break;
                }
            }
        }

        void Choose(ChoicePrompt option)
        {
            _options = null;
            Time.timeScale = _previousTimeScale;
            option.Apply?.Invoke();
        }
    }
}
