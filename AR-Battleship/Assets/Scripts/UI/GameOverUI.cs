using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ARBattleship.Unity.UI
{
    /// <summary>
    /// Builds and animates a full-screen game-over overlay at runtime.
    /// Responds to <see cref="GameManager.OnGameOver"/> and displays
    /// win/loss messaging with Rematch and Main Menu navigation.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {

        /// <summary>Root panel generated at runtime; hidden until the game ends.</summary>
        private GameObject gameOverPanel;

        /// <summary>Full-screen black overlay that fades in first.</summary>
        private Image blackOverlay;

        /// <summary>Large heading text: "MISSION COMPLETE" or "GAME OVER".</summary>
        private Text gameOverText;

        /// <summary>Smaller descriptive sub-text below the heading.</summary>
        private Text subText;

        /// <summary>Rematch button – starts a new game in the gameplay scene.</summary>
        private Button rematchButton;

        /// <summary>Main Menu button – returns to the title screen.</summary>
        private Button menuButton;

        /// <summary>AudioSource added at runtime for the game-over jingle.</summary>
        private AudioSource audioSource;

        /// <summary>
        /// Optional game-over jingle AudioClip.  If null, no music plays.
        /// Falls back to a Resources.Load lookup for "gameoverjingle" at runtime
        /// if this field is not assigned.
        /// </summary>
        [SerializeField] private AudioClip gameOverJingle;

        /// <summary>Subscribes to the game-over event.</summary>
        private void OnEnable()  => GameManager.OnGameOver += HandleGameOver;

        /// <summary>Unsubscribes to prevent ghost callbacks after scene unload.</summary>
        private void OnDisable() => GameManager.OnGameOver -= HandleGameOver;

        /// <summary>Builds the game-over panel and hides it until the game ends.</summary>
        private void Start()
        {
            BuildGameOverUI();
            gameOverPanel.SetActive(false);
        }

        /// <summary>
        /// Called by GameManager.OnGameOver with the winner index.
        /// Plays the jingle, sets the win/loss text, and starts the fade-in animation.
        /// </summary>
        /// <param name="winnerIndex">0 = local player won; 1 = enemy / AI won.</param>
        private void HandleGameOver(int winnerIndex)
        {
            // Attempt to load the jingle from Resources if not already assigned.
            AudioClip jingle = gameOverJingle ?? Resources.Load<AudioClip>("gameoverjingle");
            if (jingle != null)
            {
                audioSource.clip = jingle;
                audioSource.Play();
            }

            gameOverPanel.SetActive(true);
            bool playerWon = winnerIndex == 0;

            // Set heading text and colour based on outcome.
            gameOverText.text  = playerWon ? "MISSION\nCOMPLETE" : "GAME OVER";
            gameOverText.color = playerWon ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.85f, 0.1f, 0.1f);

            // Set sub-text description.
            subText.text = playerWon ? "Enemy fleet destroyed." : "Your fleet has been sunk.";

            StartCoroutine(FadeIn());
        }

        /// <summary>
        /// Two-stage fade-in coroutine:
        ///   1. Black overlay fades from 0 → 1 alpha (first 40% of duration).
        ///   2. Short pause (0.4 s).
        ///   3. Heading and sub-text fade from 0 → 1 (remaining 60%).
        ///   4. Brief hold (0.8 s) then the navigation buttons appear.
        /// </summary>
        private IEnumerator FadeIn()
        {
            const float duration = 2.5f;
            float elapsed = 0f;

            // Start all elements fully transparent.
            Color overlayColor = blackOverlay.color;
            overlayColor.a = 0f;
            blackOverlay.color = overlayColor;

            Color textColor = gameOverText.color;
            textColor.a = 0f;
            gameOverText.color = textColor;

            Color subColor = subText.color;
            subColor.a = 0f;
            subText.color = subColor;

            // Stage 1: Fade in the black overlay.
            while (elapsed < duration * 0.4f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.4f);
                overlayColor.a = Mathf.Lerp(0f, 1f, t);
                blackOverlay.color = overlayColor;
                yield return null;
            }

            overlayColor.a = 1f;
            blackOverlay.color = overlayColor;
            elapsed = 0f;

            // Pause between the overlay and the text appearing.
            yield return new WaitForSeconds(0.4f);

            // Stage 2: Fade in heading and sub-text simultaneously.
            while (elapsed < duration * 0.6f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.6f);
                textColor.a = Mathf.Lerp(0f, 1f, t);
                gameOverText.color = textColor;
                subColor.a = Mathf.Lerp(0f, 0.8f, t);
                subText.color = subColor;
                yield return null;
            }

            textColor.a = 1f;
            gameOverText.color = textColor;

            // Final hold before showing the navigation buttons.
            yield return new WaitForSeconds(0.8f);

            rematchButton.gameObject.SetActive(true);
            menuButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// Constructs all UI elements for the game-over screen at runtime:
        /// black overlay, heading text, sub-text, Rematch button, Main Menu button,
        /// and the AudioSource component.
        /// </summary>
        private void BuildGameOverUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            // ── Root panel (full screen) ──────────────────────────────────────
            gameOverPanel = new GameObject("GameOverPanel");
            gameOverPanel.transform.SetParent(canvas.transform, false);
            RectTransform panelRT = gameOverPanel.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            // ── Black overlay Image ───────────────────────────────────────────
            GameObject overlay = new GameObject("BlackOverlay");
            overlay.transform.SetParent(gameOverPanel.transform, false);
            RectTransform overlayRT = overlay.AddComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.offsetMin = Vector2.zero;
            overlayRT.offsetMax = Vector2.zero;
            blackOverlay = overlay.AddComponent<Image>();
            blackOverlay.color = new Color(0f, 0f, 0f, 0f); // Start fully transparent

            // ── Heading text ──────────────────────────────────────────────────
            GameObject textObj = new GameObject("GameOverText");
            textObj.transform.SetParent(gameOverPanel.transform, false);
            RectTransform textRT = textObj.AddComponent<RectTransform>();
            textRT.anchorMin       = new Vector2(0.5f, 0.5f);
            textRT.anchorMax       = new Vector2(0.5f, 0.5f);
            textRT.pivot           = new Vector2(0.5f, 0.5f);
            textRT.anchoredPosition = new Vector2(0f, 60f);
            textRT.sizeDelta       = new Vector2(700f, 200f);
            gameOverText = textObj.AddComponent<Text>();
            gameOverText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            gameOverText.fontSize  = 72;
            gameOverText.fontStyle = FontStyle.Bold;
            gameOverText.alignment = TextAnchor.MiddleCenter;
            gameOverText.color     = new Color(0.85f, 0.1f, 0.1f, 0f); // Starts transparent
            gameOverText.text      = "GAME OVER";

            // ── Sub-text ──────────────────────────────────────────────────────
            GameObject subObj = new GameObject("SubText");
            subObj.transform.SetParent(gameOverPanel.transform, false);
            RectTransform subRT = subObj.AddComponent<RectTransform>();
            subRT.anchorMin       = new Vector2(0.5f, 0.5f);
            subRT.anchorMax       = new Vector2(0.5f, 0.5f);
            subRT.pivot           = new Vector2(0.5f, 0.5f);
            subRT.anchoredPosition = new Vector2(0f, -20f);
            subRT.sizeDelta       = new Vector2(600f, 60f);
            subText = subObj.AddComponent<Text>();
            subText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subText.fontSize  = 24;
            subText.fontStyle = FontStyle.Italic;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color     = new Color(1f, 1f, 1f, 0f); // Starts transparent
            subText.text      = "";

            // ── Rematch button (loads the gameplay scene again) ───────────────
            GameObject rematchObj = CreateButton(gameOverPanel.transform, "REMATCH", new Vector2(-120f, -120f), new Vector2(200f, 55f));
            rematchButton = rematchObj.GetComponent<Button>();
            rematchButton.gameObject.SetActive(false); // Hidden until fade completes
            rematchButton.onClick.AddListener(() => SceneManager.LoadScene("5Gameplay"));

            // ── Main Menu button (returns to title screen) ────────────────────
            GameObject menuObj = CreateButton(gameOverPanel.transform, "MAIN MENU", new Vector2(120f, -120f), new Vector2(200f, 55f));
            menuButton = menuObj.GetComponent<Button>();
            menuButton.gameObject.SetActive(false); // Hidden until fade completes
            menuButton.onClick.AddListener(() => SceneManager.LoadScene("1TitleScreen"));

            // ── AudioSource for the game-over jingle ──────────────────────────
            audioSource            = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop       = false;
        }

        /// <summary>
        /// Creates a simple dark rectangular button with a centred bold label.
        /// </summary>
        /// <param name="parent">Transform to parent the button to.</param>
        /// <param name="label">Text displayed on the button.</param>
        /// <param name="anchoredPos">Position relative to a centre-anchored pivot.</param>
        /// <param name="size">Width and height of the button.</param>
        /// <returns>The root GameObject of the generated button.</returns>
        private GameObject CreateButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size)
        {
            GameObject obj = new GameObject(label);
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin       = new Vector2(0.5f, 0.5f);
            rt.anchorMax       = new Vector2(0.5f, 0.5f);
            rt.pivot           = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta       = size;

            obj.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);
            obj.AddComponent<Button>();

            // ── Label ──
            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform, false);
            RectTransform textRT = textObj.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            Text text = textObj.AddComponent<Text>();
            text.text      = label;
            text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize  = 18;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color     = Color.white;

            return obj;
        }
    }
}
