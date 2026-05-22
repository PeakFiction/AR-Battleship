using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARBattleship.Unity.UI
{
    /// <summary>
    /// Generates a fleet status panel that displays a coloured indicator dot
    /// and label for each ship on both sides.  Dims indicators when ships are sunk.
    /// </summary>
    public class ShipStatusUI : MonoBehaviour
    {

        /// <summary>Root panel generated at runtime; hidden until the game starts.</summary>
        private GameObject statusPanel;

        /// <summary>
        /// Maps each ship type string to the player's indicator dot Image.
        /// Dimmed to grey when the ship is sunk.
        /// </summary>
        private Dictionary<string, Image> playerShipIndicators = new Dictionary<string, Image>();

        /// <summary>
        /// Maps each ship type string to the enemy's indicator dot Image.
        /// Dimmed to grey when the ship is sunk.
        /// </summary>
        private Dictionary<string, Image> enemyShipIndicators = new Dictionary<string, Image>();

        /// <summary>
        /// Maps each ship type string to the enemy's label Text component.
        /// Updated to show "[SUNK]" when the ship goes down.
        /// </summary>
        private Dictionary<string, Text> enemyShipLabels = new Dictionary<string, Text>();

        /// <summary>
        /// Ordered list of ship type strings matching the domain model.
        /// Used to generate the indicator rows and look up the human-readable label.
        /// </summary>
        private readonly string[] shipTypes = new string[]
        {
            "Carrier",
            "Battleship",
            "Cruiser",
            "Submarine",
            "Destroyer"
        };

        /// <summary>Subscribes to game-start and ship-sunk events.</summary>
        private void OnEnable()
        {
            GameManager.OnGameStarted += HandleGameStarted;
            GameManager.OnShipSunk   += HandleShipSunk;
        }

        /// <summary>Unsubscribes to prevent ghost callbacks after scene unload.</summary>
        private void OnDisable()
        {
            GameManager.OnGameStarted -= HandleGameStarted;
            GameManager.OnShipSunk   -= HandleShipSunk;
        }

        /// <summary>Builds the status panel and hides it until gameplay begins.</summary>
        private void Start()
        {
            BuildStatusUI();
            statusPanel.SetActive(false);
        }

        /// <summary>Shows the status panel when the battle phase begins.</summary>
        private void HandleGameStarted() => statusPanel.SetActive(true);

        /// <summary>
        /// Dims the indicator dot (and updates the enemy label) for the sunk ship.
        /// </summary>
        /// <param name="playerIndex">0 = local player's ship sunk; 1 = enemy's ship sunk.</param>
        /// <param name="shipType">Type string of the sunk ship (e.g. "Carrier").</param>
        private void HandleShipSunk(int playerIndex, string shipType)
        {
            // Grey out the indicator for the relevant side.
            if (playerIndex == 0)
            {
                if (playerShipIndicators.ContainsKey(shipType))
                    playerShipIndicators[shipType].color = new Color(0.3f, 0.3f, 0.3f, 1f);
            }
            else
            {
                if (enemyShipIndicators.ContainsKey(shipType))
                    enemyShipIndicators[shipType].color = new Color(0.3f, 0.3f, 0.3f, 1f);

                // Reveal the ship type on the enemy label and mark it sunk.
                if (enemyShipLabels.ContainsKey(shipType))
                {
                    enemyShipLabels[shipType].text  = GetShipLabel(shipType) + " [SUNK]";
                    enemyShipLabels[shipType].color = new Color(0.5f, 0.5f, 0.5f, 1f);
                }
            }
        }

        /// <summary>
        /// Constructs the full status panel at runtime:
        ///   • "YOUR FLEET" section with green indicator dots.
        ///   • "ENEMY FLEET" section with red indicator dots and hidden labels.
        /// </summary>
        private void BuildStatusUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            // ── Panel root ────────────────────────────────────────────────────
            statusPanel = new GameObject("ShipStatusPanel");
            statusPanel.transform.SetParent(canvas.transform, false);

            RectTransform panelRT = statusPanel.AddComponent<RectTransform>();
            panelRT.anchorMin      = new Vector2(0f, 1f);
            panelRT.anchorMax      = new Vector2(0f, 1f);
            panelRT.pivot          = new Vector2(0f, 1f);
            panelRT.anchoredPosition = new Vector2(10f, -10f);
            panelRT.sizeDelta      = new Vector2(260f, 320f);
            statusPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            // ── "YOUR FLEET" header ───────────────────────────────────────────
            BuildLabel(statusPanel.transform, "YOUR FLEET", new Vector2(10f, -10f), new Vector2(240f, 25f), Color.white, 14, FontStyle.Bold);

            // ── Player indicator rows ─────────────────────────────────────────
            for (int i = 0; i < shipTypes.Length; i++)
            {
                string type   = shipTypes[i];
                float yOffset = -40f - i * 28f; // Stack rows downward

                // Green dot: alive = green, sunk = grey.
                GameObject dot = new GameObject("PlayerDot_" + type);
                dot.transform.SetParent(statusPanel.transform, false);
                RectTransform dotRT = dot.AddComponent<RectTransform>();
                dotRT.anchorMin      = new Vector2(0f, 1f);
                dotRT.anchorMax      = new Vector2(0f, 1f);
                dotRT.pivot          = new Vector2(0f, 1f);
                dotRT.anchoredPosition = new Vector2(10f, yOffset);
                dotRT.sizeDelta      = new Vector2(14f, 14f);
                Image dotImg = dot.AddComponent<Image>();
                dotImg.color         = new Color(0.2f, 0.8f, 0.2f, 1f); // Alive green
                playerShipIndicators[type] = dotImg;

                BuildLabel(statusPanel.transform, GetShipLabel(type), new Vector2(32f, yOffset + 2f), new Vector2(200f, 18f), Color.white, 12, FontStyle.Normal);
            }

            // ── "ENEMY FLEET" header ──────────────────────────────────────────
            float enemyHeaderY = -40f - shipTypes.Length * 28f - 20f;
            BuildLabel(statusPanel.transform, "ENEMY FLEET", new Vector2(10f, enemyHeaderY), new Vector2(240f, 25f), new Color(0.85f, 0.1f, 0.1f), 14, FontStyle.Bold);

            // ── Enemy indicator rows ──────────────────────────────────────────
            for (int i = 0; i < shipTypes.Length; i++)
            {
                string type   = shipTypes[i];
                float yOffset = enemyHeaderY - 28f - i * 28f;

                // Red dot: alive = red, sunk = grey.
                GameObject dot = new GameObject("EnemyDot_" + type);
                dot.transform.SetParent(statusPanel.transform, false);
                RectTransform dotRT = dot.AddComponent<RectTransform>();
                dotRT.anchorMin      = new Vector2(0f, 1f);
                dotRT.anchorMax      = new Vector2(0f, 1f);
                dotRT.pivot          = new Vector2(0f, 1f);
                dotRT.anchoredPosition = new Vector2(10f, yOffset);
                dotRT.sizeDelta      = new Vector2(14f, 14f);
                Image dotImg = dot.AddComponent<Image>();
                dotImg.color         = new Color(0.85f, 0.1f, 0.1f, 1f); // Alive red
                enemyShipIndicators[type] = dotImg;

                // Labels start as "???" to hide enemy ship types until sunk.
                Text label = BuildLabel(statusPanel.transform, "???", new Vector2(32f, yOffset + 2f), new Vector2(200f, 18f), new Color(0.6f, 0.6f, 0.6f), 12, FontStyle.Normal);
                enemyShipLabels[type] = label;
            }
        }

        /// <summary>
        /// Creates a legacy Text label as a child of <paramref name="parent"/>.
        /// </summary>
        private Text BuildLabel(Transform parent, string text, Vector2 pos, Vector2 size, Color color, int fontSize, FontStyle style)
        {
            GameObject obj = new GameObject("Label_" + text);
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin      = new Vector2(0f, 1f);
            rt.anchorMax      = new Vector2(0f, 1f);
            rt.pivot          = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta      = size;

            Text t = obj.AddComponent<Text>();
            t.text      = text;
            t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize  = fontSize;
            t.fontStyle = style;
            t.color     = color;
            t.alignment = TextAnchor.MiddleLeft;
            return t;
        }

        /// <summary>
        /// Returns a human-readable ship label with its length in parentheses.
        /// Used for the "YOUR FLEET" section and for revealing sunk enemy ships.
        /// </summary>
        /// <param name="shipType">Domain ship type string.</param>
        /// <returns>Display string, e.g. "Carrier (5)".</returns>
        private string GetShipLabel(string shipType) => shipType switch
        {
            "Carrier"    => "Carrier (5)",
            "Battleship" => "Battleship (4)",
            "Cruiser"    => "Cruiser (3)",
            "Submarine"  => "Submarine (3)",
            "Destroyer"  => "Destroyer (2)",
            _            => shipType // Fallback: return as-is for unknown types
        };
    }
}
