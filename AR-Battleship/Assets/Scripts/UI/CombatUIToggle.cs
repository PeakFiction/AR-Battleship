using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ARBattleship.Unity.UI
{
    /// <summary>
    /// Runtime-generated toggle button that shows or hides the scene's
    /// "CombatPanel" GameObject.  Designed for quick developer access rather
    /// than production UI.
    /// </summary>
    public class CombatUIToggle : MonoBehaviour
    {

        /// <summary>Cached reference to the CombatPanel, found by name at runtime.</summary>
        private GameObject combatPanel;

        /// <summary>The generated toggle button component.</summary>
        private Button toggleButton;

        /// <summary>The text label on the toggle button that shows current state.</summary>
        private Text buttonLabel;

        /// <summary>True when the combat panel is currently visible.</summary>
        private bool isVisible = false;

        /// <summary>Subscribes to the game-started event to hide the panel at battle start.</summary>
        void OnEnable()  => GameManager.OnGameStarted += HidePanelAfterStart;

        /// <summary>Unsubscribes to prevent ghost callbacks after scene unload.</summary>
        void OnDisable() => GameManager.OnGameStarted -= HidePanelAfterStart;

        /// <summary>Generates the toggle button into the parent Canvas.</summary>
        void Start() => CreateToggleButton();

        /// <summary>
        /// Defers the panel hide to the next frame via coroutine.  This ensures
        /// that any Start() initialisation in CombatUI has already run before
        /// we attempt to hide the panel.
        /// </summary>
        void HidePanelAfterStart() => StartCoroutine(HidePanelNextFrame());

        /// <summary>Yields one frame then hides the combat panel.</summary>
        IEnumerator HidePanelNextFrame()
        {
            yield return null; // Wait one frame for CombatUI.Start to complete
            FindPanel();
            SetPanelVisible(false);
        }

        /// <summary>
        /// Attempts to locate the CombatPanel in the scene by name.
        /// Called lazily so the panel only needs to exist by the time it is toggled.
        /// </summary>
        void FindPanel()
        {
            if (combatPanel == null)
                combatPanel = GameObject.Find("CombatPanel");
        }

        /// <summary>
        /// Sets the combat panel's active state and updates the button label.
        /// </summary>
        /// <param name="visible">True to show the panel; false to hide it.</param>
        void SetPanelVisible(bool visible)
        {
            isVisible = visible;

            if (combatPanel  != null) combatPanel.SetActive(visible);
            if (buttonLabel  != null) buttonLabel.text = visible ? "Hide 2D Board" : "Show 2D Board";
        }

        /// <summary>
        /// Programmatically builds a small toggle button in the top-right corner
        /// of the parent Canvas.  Uses legacy Text (no TMP) for simplicity.
        /// </summary>
        void CreateToggleButton()
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            // Create the button root in the Canvas.
            GameObject btnObj = new GameObject("ToggleCombatUI");
            btnObj.transform.SetParent(canvas.transform, false);

            // Anchor to the top-right corner.
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin       = new Vector2(1f, 1f);
            rt.anchorMax       = new Vector2(1f, 1f);
            rt.pivot           = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-10f, -10f);
            rt.sizeDelta       = new Vector2(140f, 40f);

            // Semi-transparent dark background.
            btnObj.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

            // Wire the toggle action.
            toggleButton = btnObj.AddComponent<Button>();
            toggleButton.onClick.AddListener(Toggle);

            // Label child.
            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRT = textObj.AddComponent<RectTransform>();
            textRT.anchorMin  = Vector2.zero;
            textRT.anchorMax  = Vector2.one;
            textRT.offsetMin  = Vector2.zero;
            textRT.offsetMax  = Vector2.zero;

            buttonLabel            = textObj.AddComponent<Text>();
            buttonLabel.font       = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonLabel.fontSize   = 14;
            buttonLabel.fontStyle  = FontStyle.Bold;
            buttonLabel.alignment  = TextAnchor.MiddleCenter;
            buttonLabel.color      = Color.white;
            buttonLabel.text       = "Show 2D Board"; // Initial state: panel is hidden
        }

        /// <summary>Finds the panel if needed and flips its visibility.</summary>
        void Toggle()
        {
            FindPanel();
            SetPanelVisible(!isVisible);
        }
    }
}
