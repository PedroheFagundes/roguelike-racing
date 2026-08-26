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
    /// Fully operable by mouse, keyboard, or gamepad: arrow keys/WASD or the left
    /// stick/d-pad move a highlighted selection, Enter/Space/the South face button
    /// confirms it, and clicking a button with the mouse still works too. This matters
    /// as much as in-race controls do — the original version was mouse-only, which broke
    /// keyboard/gamepad-only play the moment a lap or item box paused the game.
    ///
    /// This is the v1/single-player default documented in docs/DESIGN_DECISIONS.md:
    /// pausing everything is the simplest thing that works when there's only one human
    /// to wait on. Moving to multiplayer later means adding a per-decision timeout and
    /// no longer touching Time.timeScale here — it does not mean rewriting how choices
    /// are built or applied (see ChoicePrompt).
    /// </summary>
    public class PauseChoiceUI : MonoBehaviour
    {
        const float NavRepeatDelaySeconds = 0.2f;

        string _headerText = "Choose one";
        List<ChoicePrompt> _options;
        float _previousTimeScale = 1f;
        int _selectedIndex;
        float _navRepeatTimer;

        public bool IsOpen => _options != null;

        public void Open(string header, List<ChoicePrompt> options)
        {
            if (options == null || options.Count == 0) return;

            _headerText = header;
            _options = options;
            _selectedIndex = 0;
            _navRepeatTimer = 0f;
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        void Update()
        {
            if (_options == null) return;

            HandleNavigation();

            bool confirm = Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.JoystickButton0);

            if (confirm) Choose(_options[_selectedIndex]);
        }

        void HandleNavigation()
        {
            int step = 0;
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) step = 1;
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) step = -1;

            // Stick/d-pad axes have no *Down event, so drive them with a manual repeat
            // timer using unscaled time — Time.deltaTime is 0 while the game is paused.
            float stickY = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(stickY) > 0.5f)
            {
                _navRepeatTimer -= Time.unscaledDeltaTime;
                if (_navRepeatTimer <= 0f)
                {
                    step = stickY > 0f ? -1 : 1;
                    _navRepeatTimer = NavRepeatDelaySeconds;
                }
            }
            else
            {
                _navRepeatTimer = 0f;
            }

            if (step != 0)
            {
                _selectedIndex = (_selectedIndex + step + _options.Count) % _options.Count;
            }
        }

        void OnGUI()
        {
            if (_options == null) return;

            OnGuiScale.Begin();

            const float panelWidth = 460f;
            const float buttonHeight = 70f;
            const float buttonSpacing = 12f;
            const float footerHeight = 26f;
            float panelHeight = 90f + _options.Count * (buttonHeight + buttonSpacing) + footerHeight;

            var panelRect = new Rect(
                (OnGuiScale.Width - panelWidth) * 0.5f,
                (OnGuiScale.Height - panelHeight) * 0.5f,
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
            var selectedButtonStyle = new GUIStyle(buttonStyle) { fontStyle = FontStyle.Bold };

            for (int i = 0; i < _options.Count; i++)
            {
                ChoicePrompt option = _options[i];
                var buttonRect = new Rect(
                    panelRect.x + 15f,
                    panelRect.y + 60f + i * (buttonHeight + buttonSpacing),
                    panelWidth - 30f, buttonHeight);

                string marker = i == _selectedIndex ? ">> " : "   ";
                GUIStyle style = i == _selectedIndex ? selectedButtonStyle : buttonStyle;

                if (GUI.Button(buttonRect, $"{marker}{option.Title}\n{marker}{option.Description}", style))
                {
                    Choose(option);
                    break;
                }
            }

            var footerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            var footerRect = new Rect(panelRect.x + 10f, panelRect.yMax - footerHeight, panelRect.width - 20f, footerHeight);
            GUI.Label(footerRect, "Up/Down or stick to choose, Enter/Space/A to confirm, or click", footerStyle);
        }

        void Choose(ChoicePrompt option)
        {
            _options = null;
            Time.timeScale = _previousTimeScale;
            option.Apply?.Invoke();
        }
    }
}
