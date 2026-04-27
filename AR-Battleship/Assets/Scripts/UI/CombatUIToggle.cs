using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ARBattleship.Unity.UI
{
    public class CombatUIToggle : MonoBehaviour
    {
        private GameObject combatPanel;
        private Button toggleButton;
        private Text buttonLabel;
        private bool isVisible = false;

        void OnEnable()
        {
            GameManager.OnGameStarted += HidePanelAfterStart;
        }

        void OnDisable()
        {
            GameManager.OnGameStarted -= HidePanelAfterStart;
        }

        void Start()
        {
            CreateToggleButton();
        }

        void HidePanelAfterStart()
        {
            StartCoroutine(HidePanelNextFrame());
        }

        IEnumerator HidePanelNextFrame()
        {
            yield return null;
            FindPanel();
            SetPanelVisible(false);
        }

        void FindPanel()
        {
            if (combatPanel == null)
                combatPanel = GameObject.Find("CombatPanel");
        }

        void SetPanelVisible(bool visible)
        {
            isVisible = visible;
            if (combatPanel != null) combatPanel.SetActive(visible);
            if (buttonLabel != null)
                buttonLabel.text = visible ? "Hide 2D Board" : "Show 2D Board";
        }

        void CreateToggleButton()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            GameObject btnObj = new GameObject("ToggleCombatUI");
            btnObj.transform.SetParent(canvas.transform, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-10f, -10f);
            rt.sizeDelta = new Vector2(140f, 40f);

            btnObj.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            toggleButton = btnObj.AddComponent<Button>();
            toggleButton.onClick.AddListener(Toggle);

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRT = textObj.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            buttonLabel = textObj.AddComponent<Text>();
            buttonLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonLabel.fontSize = 14;
            buttonLabel.fontStyle = FontStyle.Bold;
            buttonLabel.alignment = TextAnchor.MiddleCenter;
            buttonLabel.color = Color.white;
            buttonLabel.text = "Show 2D Board";
        }

        void Toggle()
        {
            FindPanel();
            SetPanelVisible(!isVisible);
        }
    }
}