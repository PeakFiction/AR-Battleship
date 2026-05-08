using UnityEngine;
using UnityEngine.UI;
using ARBattleship.Core.Application.Enums;
using ARBattleship.Unity;
using ARBattleship.Multiplayer.Battleship;

public class ManualFireUI : MonoBehaviour
{
    private InputField colInput;
    private InputField rowInput;
    private Text statusText;
    private GameObject panel;

    void Start()
    {
        BuildUI();
    }

    void OnEnable()
    {
        GameManager.OnGameStarted += ShowPanel;
        GameManager.OnGameOver += HidePanel;
        GameManager.OnPlayerShotFired += OnShotResult;
    }

    void OnDisable()
    {
        GameManager.OnGameStarted -= ShowPanel;
        GameManager.OnGameOver -= HidePanel;
        GameManager.OnPlayerShotFired -= OnShotResult;
    }

    void ShowPanel()
    {
        if (panel != null) panel.SetActive(true);
    }

    void HidePanel(int winnerIndex)
    {
        if (panel != null) panel.SetActive(false);
    }

    void OnShotResult(int x, int y, ShotOutcome outcome)
    {
        if (statusText == null) return;
        string colLetter = ((char)('A' + x)).ToString();
        string rowNum = (y + 1).ToString();
        string coord = $"{colLetter}{rowNum}";

        if (outcome == ShotOutcome.Miss)
            statusText.text = $"{coord}: Miss";
        else if (outcome == ShotOutcome.Hit)
            statusText.text = $"{coord}: Hit!";
        else if (outcome == ShotOutcome.Sunk)
            statusText.text = $"{coord}: Sunk!";
    }

    void BuildUI()
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

        CreateLabel(panel.transform, "Manual Fire", new Vector2(0f, -5f), new Vector2(180f, 25f), 16, FontStyle.Bold, TextAnchor.MiddleCenter);

        CreateLabel(panel.transform, "Column (A-J):", new Vector2(10f, -32f), new Vector2(160f, 20f), 12, FontStyle.Normal, TextAnchor.MiddleLeft);
        colInput = CreateInputField(panel.transform, new Vector2(10f, -52f), new Vector2(160f, 28f), "A", 1);

        CreateLabel(panel.transform, "Row (1-10):", new Vector2(10f, -85f), new Vector2(160f, 20f), 12, FontStyle.Normal, TextAnchor.MiddleLeft);
        rowInput = CreateInputField(panel.transform, new Vector2(10f, -105f), new Vector2(160f, 28f), "1", 2);

        GameObject fireBtnObj = new GameObject("FireButton");
        fireBtnObj.transform.SetParent(panel.transform, false);
        RectTransform fireRT = fireBtnObj.AddComponent<RectTransform>();
        fireRT.anchorMin = new Vector2(0f, 1f);
        fireRT.anchorMax = new Vector2(0f, 1f);
        fireRT.pivot = new Vector2(0f, 1f);
        fireRT.anchoredPosition = new Vector2(10f, -138f);
        fireRT.sizeDelta = new Vector2(100f, 30f);
        fireBtnObj.AddComponent<Image>().color = new Color(0.8f, 0.15f, 0.15f, 1f);
        Button fireButton = fireBtnObj.AddComponent<Button>();
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

    void OnFireClicked()
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

        int col = colText[0] - 'A';
        int row = rowNum - 1;

        var networkController = FindObjectOfType<NetworkBattleshipGameController>();
        if (networkController != null)
        {
            networkController.RequestFireShot(col, row);
            statusText.text = "Firing...";
            return;
        }

        if (GameManager.Instance.CurrentPlayerTurn != 0)
        {
            statusText.text = "Not your turn";
            return;
        }

        ShotOutcome result = GameManager.Instance.FireShot(col, row);

        if (result == ShotOutcome.None)
            statusText.text = "Firing...";
        else
        {
            string coord = $"{colText}{rowNum}";
            if (result == ShotOutcome.Miss) statusText.text = $"{coord}: Miss";
            else if (result == ShotOutcome.Hit) statusText.text = $"{coord}: Hit!";
            else if (result == ShotOutcome.Sunk) statusText.text = $"{coord}: Sunk!";
        }
    }

    void CreateLabel(Transform parent, string text, Vector2 pos, Vector2 size, int fontSize, FontStyle style, TextAnchor anchor)
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

    InputField CreateInputField(Transform parent, Vector2 pos, Vector2 size, string defaultText, int charLimit)
    {
        GameObject obj = new GameObject("InputField");
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image bg = obj.AddComponent<Image>();
        bg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

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