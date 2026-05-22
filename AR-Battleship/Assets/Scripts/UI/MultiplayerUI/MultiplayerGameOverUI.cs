using System.Collections;
using ARBattleship.Multiplayer.Battleship;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ARBattleship.Unity.UI
{
    /// <summary>
    /// Displays the multiplayer victory or defeat overlay and provides navigation back to lobby or menu scenes.
    /// </summary>
    public class MultiplayerGameOverUI : MonoBehaviour
    {
        [SerializeField] private string rematchSceneName = "2LobbyScreen";
        [SerializeField] private string menuSceneName = "1TitleScreen";
        [SerializeField] private AudioClip gameOverJingle;

        private GameObject gameOverPanel;
        private Image blackOverlay;
        private Text gameOverText;
        private Text subText;
        private Button rematchButton;
        private Button menuButton;
        private AudioSource audioSource;

        private int localPlayerNumber;

        /// <summary>
        /// Ensures the overlay starts hidden before multiplayer game-over events occur.
        /// </summary>
        private void Awake()
        {
            BuildGameOverUI();
            gameOverPanel.SetActive(false);
        }

        /// <summary>
        /// Subscribes to player-assignment and game-over events.
        /// </summary>
        private void OnEnable()
        {
            NetworkBattleshipEvents.LocalPlayerAssigned += OnLocalPlayerAssigned;
            NetworkBattleshipEvents.GameOver += HandleGameOver;
        }

        /// <summary>
        /// Unsubscribes from multiplayer events to avoid duplicate overlays.
        /// </summary>
        private void OnDisable()
        {
            NetworkBattleshipEvents.LocalPlayerAssigned -= OnLocalPlayerAssigned;
            NetworkBattleshipEvents.GameOver -= HandleGameOver;
        }

        /// <summary>
        /// Stores the local player number used to decide victory or defeat text.
        /// </summary>
        private void OnLocalPlayerAssigned(int playerNumber)
        {
            localPlayerNumber = playerNumber;
        }

        /// <summary>
        /// Displays the result overlay and chooses text based on whether the local player won.
        /// </summary>
        private void HandleGameOver(int winnerPlayerNumber)
        {
            AudioClip jingle = gameOverJingle;

            if (jingle == null)
            {
                jingle = Resources.Load<AudioClip>("gameoverjingle");
            }

            if (jingle != null)
            {
                audioSource.clip = jingle;
                audioSource.Play();
            }

            gameOverPanel.SetActive(true);

            bool playerWon = winnerPlayerNumber == localPlayerNumber;

            gameOverText.text = playerWon ? "MISSION\nCOMPLETE" : "GAME OVER";

            gameOverText.color = playerWon
                ? new Color(0.2f, 0.8f, 0.2f)
                : new Color(0.85f, 0.1f, 0.1f);

            subText.text = playerWon
                ? "Enemy fleet destroyed."
                : "Your fleet has been sunk.";

            StartCoroutine(FadeIn());
        }

        /// <summary>
        /// Fades and scales the overlay in for a smoother game-over presentation.
        /// </summary>
        private IEnumerator FadeIn()
        {
            float duration = 2.5f;
            float elapsed = 0f;

            Color overlayColor = blackOverlay.color;
            overlayColor.a = 0f;
            blackOverlay.color = overlayColor;

            Color textColor = gameOverText.color;
            textColor.a = 0f;
            gameOverText.color = textColor;

            Color subColor = subText.color;
            subColor.a = 0f;
            subText.color = subColor;

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

            yield return new WaitForSeconds(0.4f);

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

            yield return new WaitForSeconds(0.8f);

            rematchButton.gameObject.SetActive(true);
            menuButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// Generates the game-over overlay, result label, and navigation buttons.
        /// </summary>
        private void BuildGameOverUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            gameOverPanel = new GameObject("GameOverPanel");
            gameOverPanel.transform.SetParent(canvas.transform, false);

            RectTransform panelRT = gameOverPanel.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            GameObject overlay = new GameObject("BlackOverlay");
            overlay.transform.SetParent(gameOverPanel.transform, false);

            RectTransform overlayRT = overlay.AddComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.offsetMin = Vector2.zero;
            overlayRT.offsetMax = Vector2.zero;

            blackOverlay = overlay.AddComponent<Image>();
            blackOverlay.color = new Color(0f, 0f, 0f, 0f);

            GameObject textObj = new GameObject("GameOverText");
            textObj.transform.SetParent(gameOverPanel.transform, false);

            RectTransform textRT = textObj.AddComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0.5f, 0.5f);
            textRT.anchorMax = new Vector2(0.5f, 0.5f);
            textRT.pivot = new Vector2(0.5f, 0.5f);
            textRT.anchoredPosition = new Vector2(0f, 60f);
            textRT.sizeDelta = new Vector2(700f, 200f);

            gameOverText = textObj.AddComponent<Text>();
            gameOverText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            gameOverText.fontSize = 72;
            gameOverText.fontStyle = FontStyle.Bold;
            gameOverText.alignment = TextAnchor.MiddleCenter;
            gameOverText.color = new Color(0.85f, 0.1f, 0.1f, 0f);
            gameOverText.text = "GAME OVER";

            GameObject subObj = new GameObject("SubText");
            subObj.transform.SetParent(gameOverPanel.transform, false);

            RectTransform subRT = subObj.AddComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0.5f, 0.5f);
            subRT.anchorMax = new Vector2(0.5f, 0.5f);
            subRT.pivot = new Vector2(0.5f, 0.5f);
            subRT.anchoredPosition = new Vector2(0f, -20f);
            subRT.sizeDelta = new Vector2(600f, 60f);

            subText = subObj.AddComponent<Text>();
            subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subText.fontSize = 24;
            subText.fontStyle = FontStyle.Italic;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(1f, 1f, 1f, 0f);
            subText.text = "";

            GameObject rematchObj = CreateButton(
                gameOverPanel.transform,
                "REMATCH",
                new Vector2(-120f, -120f),
                new Vector2(200f, 55f)
            );

            rematchButton = rematchObj.GetComponent<Button>();
            rematchButton.gameObject.SetActive(false);
            rematchButton.onClick.AddListener(ReturnToLobby);

            GameObject menuObj = CreateButton(
                gameOverPanel.transform,
                "MAIN MENU",
                new Vector2(120f, -120f),
                new Vector2(200f, 55f)
            );

            menuButton = menuObj.GetComponent<Button>();
            menuButton.gameObject.SetActive(false);
            menuButton.onClick.AddListener(ReturnToMenu);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        /// <summary>
        /// Stops the network session and loads the multiplayer lobby screen.
        /// </summary>
        private void ReturnToLobby()
        {
            ShutdownNetworkIfNeeded();
            SceneManager.LoadScene(rematchSceneName);
        }

        /// <summary>
        /// Stops the network session and loads the title/menu screen.
        /// </summary>
        private void ReturnToMenu()
        {
            ShutdownNetworkIfNeeded();
            SceneManager.LoadScene(menuSceneName);
        }

        /// <summary>
        /// Safely shuts down Netcode before leaving the multiplayer match.
        /// </summary>
        private void ShutdownNetworkIfNeeded()
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        /// <summary>
        /// Creates a generated UI button with consistent placement and styling.
        /// </summary>
        private GameObject CreateButton(
            Transform parent,
            string label,
            Vector2 anchoredPos,
            Vector2 size)
        {
            GameObject obj = new GameObject(label);
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image image = obj.AddComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            Button button = obj.AddComponent<Button>();
            button.targetGraphic = image;

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform, false);

            RectTransform textRT = textObj.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;

            return obj;
        }
    }
}
