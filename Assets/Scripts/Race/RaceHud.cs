using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// On-screen HUD for the player, built with legacy OnGUI (no Canvas needed): lap
    /// count, race position, held item + how to use it, and a big wrong-way warning.
    /// All fields are optional (null-checked) so this still degrades gracefully to just
    /// the lap counter if something isn't wired up.
    /// </summary>
    public class RaceHud : MonoBehaviour
    {
        public LapTracker target;
        public RaceStandings standings;
        public WrongWayDetector wrongWayDetector;
        public KartInventory inventory;

        GUIStyle _infoStyle;
        GUIStyle _itemStyle;
        GUIStyle _wrongWayStyle;

        void EnsureStyles()
        {
            if (_infoStyle != null) return;

            _infoStyle = new GUIStyle(GUI.skin.label) { fontSize = 26 };
            _infoStyle.normal.textColor = Color.white;

            _itemStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            _itemStyle.normal.textColor = Color.white;

            _wrongWayStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _wrongWayStyle.normal.textColor = Color.red;
        }

        void OnGUI()
        {
            if (target == null) return;

            OnGuiScale.Begin();
            EnsureStyles();

            string positionText = standings != null ? $"   Posicao: {standings.GetPosition(target)}/{standings.TotalKarts}" : "";
            GUI.Label(new Rect(20, 20, 420, 40), $"Volta: {target.LapCount}{positionText}", _infoStyle);

            string itemName = inventory != null && inventory.HeldItem.HasValue ? inventory.HeldItem.Value.Name : "-";
            string itemLine = $"Item: {itemName}   [E / Ctrl / botao X] usar item   [Shift / Space / A ou RB] drift";
            GUI.Label(new Rect(20, 58, 700, 26), itemLine, _itemStyle);

            if (wrongWayDetector != null && wrongWayDetector.IsWrongWay)
            {
                var rect = new Rect(OnGuiScale.Width * 0.5f - 220f, 90f, 440f, 60f);
                GUI.Label(rect, "CONTRAMAO!", _wrongWayStyle);
            }
        }
    }
}
