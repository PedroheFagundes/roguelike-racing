using System;
using RoguelikeRacing.Race;
using RoguelikeRacing.Track;
using UnityEngine;

namespace RoguelikeRacing.Core
{
    /// <summary>
    /// Pre-race setup screen: pick a track and a character, then start. Shown before
    /// GameBootstrap builds the race itself, using the same plain OnGUI approach as the
    /// rest of the prototype's UI (see PauseChoiceUI) -- no Canvas needed.
    ///
    /// Fully operable by mouse, keyboard, or gamepad, matching PauseChoiceUI: arrow
    /// keys/WASD or the left stick move focus between the track row, the character row,
    /// and the start button; moving focus within a row also changes that row's
    /// selection (same as clicking does), so there's no separate "confirm" step except
    /// to actually start the race.
    /// </summary>
    public class RaceSetupUI : MonoBehaviour
    {
        const float NavRepeatDelaySeconds = 0.18f;
        const int TrackRow = 0;
        const int CharacterRow = 1;
        const int StartRow = 2;

        public event Action<TrackLayout, int> RaceConfirmed;

        int _selectedTrackIndex;
        int _selectedCharacterIndex;
        int _focusRow;
        float _navRepeatTimer;

        GUIStyle _titleStyle;
        GUIStyle _sectionLabelStyle;
        GUIStyle _descriptionStyle;
        GUIStyle _footerStyle;
        GUIStyle _optionButtonStyle;
        GUIStyle _selectedOptionButtonStyle;
        GUIStyle _startButtonStyle;
        GUIStyle _startButtonFocusedStyle;

        void Update()
        {
            HandleNavigation();
        }

        void HandleNavigation()
        {
            int rowStep = 0;
            if (Input.GetKeyDown(KeyCode.DownArrow)) rowStep = 1;
            else if (Input.GetKeyDown(KeyCode.UpArrow)) rowStep = -1;

            int colStep = 0;
            if (Input.GetKeyDown(KeyCode.RightArrow)) colStep = 1;
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) colStep = -1;

            // Stick/d-pad axes have no *Down event, so drive them with a manual repeat
            // timer using unscaled time (consistent with PauseChoiceUI).
            float stickX = Input.GetAxisRaw("Horizontal");
            float stickY = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(stickX) > 0.5f || Mathf.Abs(stickY) > 0.5f)
            {
                _navRepeatTimer -= Time.unscaledDeltaTime;
                if (_navRepeatTimer <= 0f)
                {
                    if (Mathf.Abs(stickX) > Mathf.Abs(stickY)) colStep = stickX > 0f ? 1 : -1;
                    else rowStep = stickY > 0f ? -1 : 1;
                    _navRepeatTimer = NavRepeatDelaySeconds;
                }
            }
            else
            {
                _navRepeatTimer = 0f;
            }

            if (rowStep != 0)
            {
                _focusRow = Mathf.Clamp(_focusRow + rowStep, TrackRow, StartRow);
            }

            if (colStep != 0)
            {
                if (_focusRow == TrackRow)
                {
                    _selectedTrackIndex = Wrap(_selectedTrackIndex + colStep, TrackCatalog.All.Count);
                }
                else if (_focusRow == CharacterRow)
                {
                    _selectedCharacterIndex = Wrap(_selectedCharacterIndex + colStep, CharacterCatalog.All.Count);
                }
            }

            bool confirm = Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.JoystickButton0);

            if (confirm && _focusRow == StartRow) ConfirmAndStart();
        }

        static int Wrap(int value, int count) => ((value % count) + count) % count;

        void ConfirmAndStart()
        {
            RaceConfirmed?.Invoke(TrackCatalog.All[_selectedTrackIndex], _selectedCharacterIndex);
            Destroy(gameObject);
        }

        void EnsureStyles()
        {
            if (_titleStyle != null) return;

            _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
            _sectionLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            _descriptionStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Italic };
            _footerStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };

            _optionButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14 };
            _selectedOptionButtonStyle = new GUIStyle(_optionButtonStyle) { fontStyle = FontStyle.Bold };
            _selectedOptionButtonStyle.normal.textColor = Color.yellow;

            _startButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            _startButtonFocusedStyle = new GUIStyle(_startButtonStyle) { fontStyle = FontStyle.Bold };
            _startButtonFocusedStyle.normal.textColor = Color.yellow;
        }

        void OnGUI()
        {
            OnGuiScale.Begin();
            EnsureStyles();

            const float panelWidth = 540f;
            const float panelHeight = 400f;
            var panelRect = new Rect((OnGuiScale.Width - panelWidth) * 0.5f, (OnGuiScale.Height - panelHeight) * 0.5f, panelWidth, panelHeight);

            GUI.Box(panelRect, GUIContent.none);
            GUI.Label(new Rect(panelRect.x, panelRect.y + 10f, panelRect.width, 40f), "Roguelike Racing", _titleStyle);

            float y = panelRect.y + 60f;
            y = DrawTrackSection(panelRect, y);
            y += 10f;
            y = DrawCharacterSection(panelRect, y);
            y += 10f;

            var startButtonRect = new Rect(panelRect.x + 20f, y, panelRect.width - 40f, 40f);
            GUIStyle startStyle = _focusRow == StartRow ? _startButtonFocusedStyle : _startButtonStyle;
            if (GUI.Button(startButtonRect, "Iniciar corrida", startStyle))
            {
                ConfirmAndStart();
            }

            var footerRect = new Rect(panelRect.x + 10f, panelRect.yMax - 22f, panelRect.width - 20f, 20f);
            GUI.Label(footerRect, "Setas/analogico para navegar, Enter/Espaco/botao sul no Iniciar para confirmar, ou clique", _footerStyle);
        }

        float DrawTrackSection(Rect panelRect, float y)
        {
            GUI.Label(new Rect(panelRect.x + 20f, y, panelRect.width - 40f, 22f), "Pista", _sectionLabelStyle);
            y += 24f;

            for (int i = 0; i < TrackCatalog.All.Count; i++)
            {
                var rect = new Rect(panelRect.x + 20f + i * 170f, y, 160f, 40f);
                bool selected = i == _selectedTrackIndex;
                if (GUI.Button(rect, TrackCatalog.All[i].Name, selected ? _selectedOptionButtonStyle : _optionButtonStyle))
                {
                    _selectedTrackIndex = i;
                    _focusRow = TrackRow;
                }
            }
            y += 44f;

            GUI.Label(new Rect(panelRect.x + 20f, y, panelRect.width - 40f, 20f), TrackCatalog.All[_selectedTrackIndex].Description, _descriptionStyle);
            return y + 24f;
        }

        float DrawCharacterSection(Rect panelRect, float y)
        {
            GUI.Label(new Rect(panelRect.x + 20f, y, panelRect.width - 40f, 22f), "Personagem", _sectionLabelStyle);
            y += 24f;

            for (int i = 0; i < CharacterCatalog.All.Count; i++)
            {
                var rect = new Rect(panelRect.x + 20f + i * 170f, y, 160f, 40f);
                bool selected = i == _selectedCharacterIndex;
                if (GUI.Button(rect, CharacterCatalog.All[i].Name, selected ? _selectedOptionButtonStyle : _optionButtonStyle))
                {
                    _selectedCharacterIndex = i;
                    _focusRow = CharacterRow;
                }
            }
            y += 44f;

            GUI.Label(new Rect(panelRect.x + 20f, y, panelRect.width - 40f, 20f), CharacterCatalog.All[_selectedCharacterIndex].Description, _descriptionStyle);
            return y + 24f;
        }
    }
}
