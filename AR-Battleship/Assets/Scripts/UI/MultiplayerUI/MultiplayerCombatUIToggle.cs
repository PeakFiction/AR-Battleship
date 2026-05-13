using System.Collections;
using ARBattleship.Multiplayer.Battleship;
using UnityEngine;
using UnityEngine.UI;

namespace ARBattleship.Unity.UI
{
    public class MultiplayerCombatUIToggle : MonoBehaviour
    {
        [SerializeField] private MultiplayerCombatUI combatUI;

        private Button toggleButton;
        private Text buttonLabel;
        private bool isVisible = false;

        private void OnEnable()
        {
            NetworkBattleshipEvents.BattleStarted += HidePanelAfterStart;
        }

        private void OnDisable()
        {
            NetworkBattleshipEvents.BattleStarted -= HidePanelAfterStart;
        }

        private void Start()
        {
            CreateToggleButton();
        }

        private void HidePanelAfterStart(int startingPlayerNumber)
        {
            StartCoroutine(HidePanelNextFrame());
        }

        private IEnumerator HidePanelNextFrame()
        {
            yield return null;

            FindCombatUI();
            SetPanelVisible(false);
        }

        private void FindCombatUI()
        {
            if (combatUI == null)
            {
                combatUI = FindObjectOfType<MultiplayerCombatUI>();
            }
        }

        private void SetPanelVisible(bool visible)
        {
            isVisible = visible;

            if (combatUI != null)
            {
                combatUI.SetGridVisible(visible);
            }

            if (buttonLabel != null)
            {
                buttonLabel.text = visible ? "Hide 2D Board" : "Show 2D Board";
            }
        }

        private void CreateToggleButton()
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

            Image image = btnObj.AddComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

            toggleButton = btnObj.AddComponent<Button>();
            toggleButton.targetGraphic = image;
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
            buttonLabel.raycastTarget = false;
        }

        private void Toggle()
        {
            FindCombatUI();
            SetPanelVisible(!isVisible);
        }
    }
}