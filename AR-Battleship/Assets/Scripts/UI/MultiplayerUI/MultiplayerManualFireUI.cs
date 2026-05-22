using ARBattleship.Core.Application.Enums;
using ARBattleship.Multiplayer.Battleship;
using UnityEngine;
using UnityEngine.UI;

namespace ARBattleship.Unity.UI
{
    /// <summary>
    /// Provides a fallback manual coordinate input UI for firing shots in multiplayer combat.
    /// </summary>
    public class MultiplayerManualFireUI : MonoBehaviour
    {
        [SerializeField] private MultiplayerBattleshipSession session;

        private InputField colInput;
        private InputField rowInput;
        private Text statusText;
        private GameObject panel;

        private int localPlayerNumber;
        private int pendingX = -1;
        private int pendingY = -1;
        private bool battleStarted = false;

        /// <summary>
        /// Builds the manual fire panel and hides it until the battle begins.
        /// </summary>
        private void Awake()
        {
            if (session == null)
                session = FindObjectOfType<MultiplayerBattleshipSession>();
            BuildUI();
        }

        /// <summary>
        /// Subscribes to multiplayer events needed by the manual fire form.
        /// </summary>
        private void OnEnable()
        {
            NetworkBattleshipEvents.LocalPlayerAssigned += OnLocalPlayerAssigned;
            NetworkBattleshipEvents.BattleStarted += ShowPanel;
            NetworkBattleshipEvents.ShotResolved += OnShotResolved;
            NetworkBattleshipEvents.ShotRejected += OnShotRejected;
            NetworkBattleshipEvents.GameOver += HidePanel;
        }

        /// <summary>
        /// Unsubscribes from multiplayer events when the form is disabled.
        /// </summary>
        private void OnDisable()
        {
            NetworkBattleshipEvents.LocalPlayerAssigned -= OnLocalPlayerAssigned;
            NetworkBattleshipEvents.BattleStarted -= ShowPanel;
            NetworkBattleshipEvents.ShotResolved -= OnShotResolved;
            NetworkBattleshipEvents.ShotRejected -= OnShotRejected;
            NetworkBattleshipEvents.GameOver -= HidePanel;
        }

        /// <summary>
        /// Stores the local player number used when sending fire requests.
        /// </summary>
        private void OnLocalPlayerAssigned(int playerNumber)
        {
            localPlayerNumber = playerNumber;
        }

        /// <summary>
        /// Displays the manual fire form when multiplayer combat starts.
        /// </summary>
        private void ShowPanel(int startingPlayerNumber)
        {
            battleStarted = true;

            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the manual fire form after the game ends.
        /// </summary>
        private void HidePanel(int winnerPlayerNumber)
        {
            battleStarted = false;

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        /// <summary>
        /// Validates the coordinate input and sends a shot request through the multiplayer session.
        /// </summary>
        private void OnFireClicked()
        {
            string colText = colInput.text.Trim().ToUpper();
            string rowText = rowInput.text.Trim();

            if (colText.Length != 1 || colText[0] < 'A' || colText[0] > 'J')
            {
                statusText.text = "Invalid column";
                return;
            }

            if (!int.TryParse(rowText, out int rowNum) || rowNum < 1 || rowNum > 10)
            {
                statusText.text = "Invalid row";
                return;
            }

            if (!battleStarted)
            {
                statusText.text = "Game not active";
                return;
            }

            if (session == null)
            {
                statusText.text = "Session missing";
                return;
            }

            int col = colText[0] - 'A';
            int row = rowNum - 1;
            string coord = $"{colText}{rowNum}";

            pendingX = col;
            pendingY = row;

            session.FireShot(col, row);

            statusText.text = $"{coord}: Shot requested...";
        }

        /// <summary>
        /// Shows feedback for a resolved manual shot and updates turn-dependent controls.
        /// </summary>
        private void OnShotResolved(
            int shooterPlayerNumber,
            int x,
            int y,
            ShotOutcome outcome,
            int? hitSegmentIndex,
            string shipOrientation,
            string shipType)
        {
            if (shooterPlayerNumber != localPlayerNumber)
            {
                return;
            }

            if (x != pendingX || y != pendingY)
            {
                return;
            }

            string coord = $"{(char)('A' + x)}{y + 1}";

            statusText.text = outcome switch
            {
                ShotOutcome.Miss => $"{coord}: Miss",
                ShotOutcome.Hit => $"{coord}: Hit!",
                ShotOutcome.Sunk => $"{coord}: Sunk!",
                _ => $"{coord}: {outcome}"
            };

            pendingX = -1;
            pendingY = -1;
        }

        /// <summary>
        /// Shows a readable error message when the server rejects a manual shot.
        /// </summary>
        private void OnShotRejected(
            int shooterPlayerNumber,
            int x,
            int y,
            GameErrorCode errorCode)
        {
            if (shooterPlayerNumber != localPlayerNumber)
            {
                return;
            }

            string coord = $"{(char)('A' + x)}{y + 1}";
            statusText.text = $"{coord}: {errorCode}";

            pendingX = -1;
            pendingY = -1;
        }

        /// <summary>
        /// Generates the manual fire panel, coordinate input fields, and fire button.
        /// </summary>
        private void BuildUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            panel = new GameObject("ManualFirePanel");
            panel.transform.SetParent(canvas.transform, false);

            RectTransform panelRT = panel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0f, 1f);
            panelRT.anchorMax = new Vector2(0f, 1f);
            panelRT.pivot = new Vector2(0f, 1f);
            panelRT.anchoredPosition = new Vector2(10f, -10f);
            panelRT.sizeDelta = new Vector2(180f, 175f);

            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            panelBg.raycastTarget = false;

            CreateLabel(
                panel.transform,
                "Manual Fire",
                new Vector2(0f, -5f),
                new Vector2(180f, 25f),
                16,
                FontStyle.Bold,
                TextAnchor.MiddleCenter
            );

            CreateLabel(
                panel.transform,
                "Column (A-J):",
                new Vector2(10f, -32f),
                new Vector2(160f, 20f),
                12,
                FontStyle.Normal,
                TextAnchor.MiddleLeft
            );

            colInput = CreateInputField(
                panel.transform,
                new Vector2(10f, -52f),
                new Vector2(160f, 28f),
                "A",
                1
            );

            CreateLabel(
                panel.transform,
                "Row (1-10):",
                new Vector2(10f, -85f),
                new Vector2(160f, 20f),
                12,
                FontStyle.Normal,
                TextAnchor.MiddleLeft
            );

            rowInput = CreateInputField(
                panel.transform,
                new Vector2(10f, -105f),
                new Vector2(160f, 28f),
                "1",
                2
            );

            GameObject fireBtnObj = new GameObject("FireButton");
            fireBtnObj.transform.SetParent(panel.transform, false);

            RectTransform fireRT = fireBtnObj.AddComponent<RectTransform>();
            fireRT.anchorMin = new Vector2(0f, 1f);
            fireRT.anchorMax = new Vector2(0f, 1f);
            fireRT.pivot = new Vector2(0f, 1f);
            fireRT.anchoredPosition = new Vector2(10f, -138f);
            fireRT.sizeDelta = new Vector2(100f, 30f);

            Image fireImage = fireBtnObj.AddComponent<Image>();
            fireImage.color = new Color(0.8f, 0.15f, 0.15f, 1f);

            Button fireButton = fireBtnObj.AddComponent<Button>();
            fireButton.targetGraphic = fireImage;
            fireButton.onClick.AddListener(OnFireClicked);

            GameObject fireLabelObj = new GameObject("Label");
            fireLabelObj.transform.SetParent(fireBtnObj.transform, false);

            RectTransform flRT = fireLabelObj.AddComponent<RectTransform>();
            flRT.anchorMin = Vector2.zero;
            flRT.anchorMax = Vector2.one;
            flRT.offsetMin = Vector2.zero;
            flRT.offsetMax = Vector2.zero;

            Text fireLabel = fireLabelObj.AddComponent<Text>();
            fireLabel.text = "FIRE";
            fireLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            fireLabel.fontSize = 16;
            fireLabel.fontStyle = FontStyle.Bold;
            fireLabel.alignment = TextAnchor.MiddleCenter;
            fireLabel.color = Color.white;
            fireLabel.raycastTarget = false;

            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(panel.transform, false);

            RectTransform statusRT = statusObj.AddComponent<RectTransform>();
            statusRT.anchorMin = new Vector2(0f, 1f);
            statusRT.anchorMax = new Vector2(1f, 1f);
            statusRT.pivot = new Vector2(0.5f, 1f);
            statusRT.anchoredPosition = new Vector2(0f, -138f);
            statusRT.sizeDelta = new Vector2(-120f, 30f);

            statusText = statusObj.AddComponent<Text>();
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 12;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.color = Color.yellow;
            statusText.text = "";
            statusText.raycastTarget = false;

            panel.SetActive(false);
        }

        /// <summary>
        /// Creates a generated Unity UI text label with consistent alignment and font settings.
        /// </summary>
        private void CreateLabel(
            Transform parent,
            string text,
            Vector2 pos,
            Vector2 size,
            int fontSize,
            FontStyle style,
            TextAnchor anchor)
        {
            GameObject obj = new GameObject(text);
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Text t = obj.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = Color.white;
            t.alignment = anchor;
            t.raycastTarget = false;
        }

        /// <summary>
        /// Creates a generated coordinate input field with placeholder text.
        /// </summary>
        private InputField CreateInputField(
            Transform parent,
            Vector2 pos,
            Vector2 size,
            string defaultText,
            int charLimit)
        {
            GameObject obj = new GameObject("InputField");
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            obj.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);

            RectTransform textRT = textObj.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(8f, 2f);
            textRT.offsetMax = new Vector2(-8f, -2f);

            Text inputText = textObj.AddComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inputText.fontSize = 16;
            inputText.color = Color.white;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.supportRichText = false;

            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(obj.transform, false);

            RectTransform phRT = placeholderObj.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(8f, 2f);
            phRT.offsetMax = new Vector2(-8f, -2f);

            Text placeholder = placeholderObj.AddComponent<Text>();
            placeholder.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholder.fontSize = 16;
            placeholder.fontStyle = FontStyle.Italic;
            placeholder.color = new Color(1f, 1f, 1f, 0.3f);
            placeholder.alignment = TextAnchor.MiddleLeft;
            placeholder.text = defaultText;
            placeholder.raycastTarget = false;

            InputField input = obj.AddComponent<InputField>();
            input.textComponent = inputText;
            input.placeholder = placeholder;
            input.characterLimit = charLimit;
            input.text = defaultText;

            return input;
        }
    }
}
