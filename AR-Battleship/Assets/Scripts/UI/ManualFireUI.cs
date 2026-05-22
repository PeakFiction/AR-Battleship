using UnityEngine;
using UnityEngine.UI;
using ARBattleship.Core.Application.Enums;
using ARBattleship.Unity;
using ARBattleship.Multiplayer.Battleship;
using System;

/// <summary>
/// Developer debug panel that allows manual shot entry via text fields.
/// Supports both singleplayer (GameManager.FireShot) and multiplayer
/// (NetworkBattleshipGameController.RequestFireShot) code paths.
/// </summary>
public class ManualFireUI : MonoBehaviour
{

    /// <summary>Input field for the column letter (A–J).</summary>
    private InputField colInput;

    /// <summary>Input field for the row number (1–10).</summary>
    private InputField rowInput;

    /// <summary>Status label that displays the last shot outcome.</summary>
    private Text statusText;

    /// <summary>Root panel; shown when the game starts, hidden when it ends.</summary>
    private GameObject panel;

    /// <summary>Builds the UI panel at scene start.</summary>
    void Start() => BuildUI();

    /// <summary>Subscribes to game lifecycle events to show / hide the panel.</summary>
    void OnEnable()
    {
        GameManager.OnGameStarted     += ShowPanel;
        GameManager.OnGameOver        += HidePanel;
        GameManager.OnPlayerShotFired += OnShotResult;
    }

    /// <summary>Unsubscribes to prevent ghost callbacks after scene unload.</summary>
    void OnDisable()
    {
        GameManager.OnGameStarted     -= ShowPanel;
        GameManager.OnGameOver        -= HidePanel;
        GameManager.OnPlayerShotFired -= OnShotResult;
    }

    /// <summary>Makes the panel visible when the battle phase begins.</summary>
    void ShowPanel()
    {
        if (panel != null) panel.SetActive(true);
    }

    /// <summary>Hides the panel when the game ends.</summary>
    /// <param name="winnerIndex">Winner index (unused; panel is always hidden on game over).</param>
    void HidePanel(int winnerIndex)
    {
        if (panel != null) panel.SetActive(false);
    }

    /// <summary>
    /// Updates the status label with the outcome of the last player shot.
    /// Converts the zero-based x/y coordinates back to the standard A1–J10 notation.
    /// </summary>
    /// <param name="x">Column index (0-based, A = 0).</param>
    /// <param name="y">Row index (0-based, row 1 = 0).</param>
    /// <param name="outcome">Miss, Hit, or Sunk.</param>
    /// <param name="hitSegmentIndex">Unused by this panel.</param>
    /// <param name="shipOrientation">Unused by this panel.</param>
    /// <param name="shipType">Unused by this panel.</param>
    void OnShotResult(int x, int y, ShotOutcome outcome, int? hitSegmentIndex, string? shipOrientation, string? shipType)
    {
        if (statusText == null) return;

        string colLetter = ((char)('A' + x)).ToString();
        string rowNum    = (y + 1).ToString();
        string coord     = $"{colLetter}{rowNum}";

        statusText.text = outcome switch
        {
            ShotOutcome.Miss => $"{coord}: Miss",
            ShotOutcome.Hit  => $"{coord}: Hit!",
            ShotOutcome.Sunk => $"{coord}: Sunk!",
            _                => ""
        };
    }

    /// <summary>
    /// Constructs the entire debug panel at runtime:
    /// title label, column and row input fields, FIRE button, and status label.
    /// Anchored to the top-left corner of the parent Canvas.
    /// </summary>
    void BuildUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();

        // ── Panel background ──────────────────────────────────────────────────
        panel = new GameObject("ManualFirePanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin      = new Vector2(0f, 1f);
        panelRT.anchorMax      = new Vector2(0f, 1f);
        panelRT.pivot          = new Vector2(0f, 1f);
        panelRT.anchoredPosition = new Vector2(10f, -10f);
        panelRT.sizeDelta      = new Vector2(180f, 175f);
        Image panelBg          = panel.AddComponent<Image>();
        panelBg.color          = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        panelBg.raycastTarget  = false;

        // ── Title ─────────────────────────────────────────────────────────────
        CreateLabel(panel.transform, "Manual Fire", new Vector2(0f, -5f), new Vector2(180f, 25f), 16, FontStyle.Bold, TextAnchor.MiddleCenter);

        // ── Column input ──────────────────────────────────────────────────────
        CreateLabel(panel.transform, "Column (A-J):", new Vector2(10f, -32f), new Vector2(160f, 20f), 12, FontStyle.Normal, TextAnchor.MiddleLeft);
        colInput = CreateInputField(panel.transform, new Vector2(10f, -52f), new Vector2(160f, 28f), "A", 1);

        // ── Row input ─────────────────────────────────────────────────────────
        CreateLabel(panel.transform, "Row (1-10):", new Vector2(10f, -85f), new Vector2(160f, 20f), 12, FontStyle.Normal, TextAnchor.MiddleLeft);
        rowInput = CreateInputField(panel.transform, new Vector2(10f, -105f), new Vector2(160f, 28f), "1", 2);

        // ── FIRE button ───────────────────────────────────────────────────────
        GameObject fireBtnObj = new GameObject("FireButton");
        fireBtnObj.transform.SetParent(panel.transform, false);
        RectTransform fireRT = fireBtnObj.AddComponent<RectTransform>();
        fireRT.anchorMin      = new Vector2(0f, 1f);
        fireRT.anchorMax      = new Vector2(0f, 1f);
        fireRT.pivot          = new Vector2(0f, 1f);
        fireRT.anchoredPosition = new Vector2(10f, -138f);
        fireRT.sizeDelta      = new Vector2(100f, 30f);
        fireBtnObj.AddComponent<Image>().color = new Color(0.8f, 0.15f, 0.15f, 1f);
        Button fireButton = fireBtnObj.AddComponent<Button>();
        fireButton.onClick.AddListener(OnFireClicked);

        // Button label child.
        GameObject fireLabelObj = new GameObject("Label");
        fireLabelObj.transform.SetParent(fireBtnObj.transform, false);
        RectTransform flRT = fireLabelObj.AddComponent<RectTransform>();
        flRT.anchorMin  = Vector2.zero;
        flRT.anchorMax  = Vector2.one;
        flRT.offsetMin  = Vector2.zero;
        flRT.offsetMax  = Vector2.zero;
        Text fireLabel  = fireLabelObj.AddComponent<Text>();
        fireLabel.text      = "FIRE";
        fireLabel.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        fireLabel.fontSize  = 16;
        fireLabel.fontStyle = FontStyle.Bold;
        fireLabel.alignment = TextAnchor.MiddleCenter;
        fireLabel.color     = Color.white;
        fireLabel.raycastTarget = false;

        // ── Status label (result of last shot) ────────────────────────────────
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(panel.transform, false);
        RectTransform statusRT = statusObj.AddComponent<RectTransform>();
        statusRT.anchorMin      = new Vector2(0f, 1f);
        statusRT.anchorMax      = new Vector2(1f, 1f);
        statusRT.pivot          = new Vector2(0.5f, 1f);
        statusRT.anchoredPosition = new Vector2(0f, -138f);
        statusRT.sizeDelta      = new Vector2(-120f, 30f);
        statusText              = statusObj.AddComponent<Text>();
        statusText.font         = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize     = 12;
        statusText.alignment    = TextAnchor.MiddleCenter;
        statusText.color        = Color.yellow;
        statusText.text         = "";
        statusText.raycastTarget = false;

        // Panel starts hidden; shown when the game starts.
        panel.SetActive(false);
    }

    /// <summary>
    /// Validates the column and row inputs and fires the shot through either
    /// the multiplayer network controller or GameManager, depending on which
    /// is present in the scene.
    /// </summary>
    void OnFireClicked()
    {
        // ── Input validation ──────────────────────────────────────────────────
        string colText = colInput.text.Trim().ToUpper();
        string rowText = rowInput.text.Trim();

        // Column must be a single letter between A and J (inclusive).
        if (colText.Length != 1 || colText[0] < 'A' || colText[0] > 'J')
        {
            statusText.text = "Invalid column";
            return;
        }

        // Row must be an integer between 1 and 10 (inclusive).
        if (!int.TryParse(rowText, out int rowNum) || rowNum < 1 || rowNum > 10)
        {
            statusText.text = "Invalid row";
            return;
        }

        // Convert to zero-based grid indices.
        int col = colText[0] - 'A'; // 'A' = 0, 'J' = 9
        int row = rowNum - 1;       // Row 1 = index 0, Row 10 = index 9

        // ── Multiplayer path ──────────────────────────────────────────────────
        var networkController = FindObjectOfType<NetworkBattleshipGameController>();
        if (networkController != null)
        {
            // In multiplayer, submit the shot via the network controller.
            // The result will arrive asynchronously through OnShotResult.
            networkController.RequestFireShot(col, row);
            statusText.text = "Firing...";
            return;
        }

        // ── Singleplayer path ─────────────────────────────────────────────────
        if (GameManager.Instance.CurrentPlayerTurn != 0)
        {
            statusText.text = "Not your turn";
            return;
        }

        ShotOutcome result = GameManager.Instance.FireShot(col, row);

        if (result == ShotOutcome.None)
        {
            // None means the shot was fired but the result arrives asynchronously
            // (or was rejected).  The status label will update via OnShotResult.
            statusText.text = "Firing...";
        }
        else
        {
            // Immediate singleplayer result.
            string coord = $"{colText}{rowNum}";
            statusText.text = result switch
            {
                ShotOutcome.Miss => $"{coord}: Miss",
                ShotOutcome.Hit  => $"{coord}: Hit!",
                ShotOutcome.Sunk => $"{coord}: Sunk!",
                _                => ""
            };
        }
    }

    /// <summary>Creates a simple legacy Text label anchored to the top-left of the parent.</summary>
    void CreateLabel(Transform parent, string text, Vector2 pos, Vector2 size, int fontSize, FontStyle style, TextAnchor anchor)
    {
        GameObject obj = new GameObject(text);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin      = new Vector2(0f, 1f);
        rt.anchorMax      = new Vector2(0f, 1f);
        rt.pivot          = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta      = size;
        Text t      = obj.AddComponent<Text>();
        t.text      = text;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize  = fontSize;
        t.fontStyle = style;
        t.color     = Color.white;
        t.alignment = anchor;
        t.raycastTarget = false;
    }

    /// <summary>
    /// Creates a legacy InputField with a visible text field and placeholder text.
    /// </summary>
    /// <param name="parent">Parent transform.</param>
    /// <param name="pos">Anchored position relative to the top-left of the parent.</param>
    /// <param name="size">Width and height of the input field.</param>
    /// <param name="defaultText">Placeholder and initial text value.</param>
    /// <param name="charLimit">Maximum number of characters allowed.</param>
    /// <returns>The configured InputField component.</returns>
    InputField CreateInputField(Transform parent, Vector2 pos, Vector2 size, string defaultText, int charLimit)
    {
        GameObject obj = new GameObject("InputField");
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin      = new Vector2(0f, 1f);
        rt.anchorMax      = new Vector2(0f, 1f);
        rt.pivot          = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta      = size;

        Image bg = obj.AddComponent<Image>();
        bg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        // ── Text child ────────────────────────────────────────────────────────
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(8f, 2f);
        textRT.offsetMax = new Vector2(-8f, -2f);
        Text inputText           = textObj.AddComponent<Text>();
        inputText.font           = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        inputText.fontSize       = 16;
        inputText.color          = Color.white;
        inputText.alignment      = TextAnchor.MiddleLeft;
        inputText.supportRichText = false;

        // ── Placeholder child ─────────────────────────────────────────────────
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(obj.transform, false);
        RectTransform phRT = placeholderObj.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = new Vector2(8f, 2f);
        phRT.offsetMax = new Vector2(-8f, -2f);
        Text placeholder  = placeholderObj.AddComponent<Text>();
        placeholder.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        placeholder.fontSize  = 16;
        placeholder.fontStyle = FontStyle.Italic;
        placeholder.color     = new Color(1f, 1f, 1f, 0.3f);
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.text      = defaultText;
        placeholder.raycastTarget = false;

        // ── InputField ────────────────────────────────────────────────────────
        InputField input     = obj.AddComponent<InputField>();
        input.textComponent  = inputText;
        input.placeholder    = placeholder;
        input.characterLimit = charLimit;
        input.text           = defaultText;

        return input;
    }
}
