using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SplashScreenController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "0MainMenu";

    [Header("Tap To Continue")]
    [SerializeField] private TMP_Text tapToContinueText;
    [SerializeField] private CanvasGroup tapToContinueCanvasGroup;
    [SerializeField] private float tapToContinueFadeSeconds = 0.45f;
    [SerializeField] private float afterTapFadeDelaySeconds = 0.08f;

    [Header("Screen Fade")]
    [SerializeField] private bool fadeInOnStart = true;
    [SerializeField] private float initialFadeInSeconds = 0.65f;
    [SerializeField] private float fadeOutToMainMenuSeconds = 1.15f;
    [SerializeField] private float blackHoldBeforeLoadSeconds = 0.15f;
    [SerializeField] private float fadeInMainMenuSeconds = 0.85f;
    [SerializeField] private int framesToWaitBeforeMainMenuFadeIn = 3;

    [Header("Input")]
    [SerializeField] private bool allowKeyboardOrMouseInput = true;
    [SerializeField] private bool allowTouchInput = true;

    private Canvas fadeCanvas;
    private CanvasGroup fadeCanvasGroup;
    private Image fadeImage;
    private bool isTransitioning;
    private bool isQuitting;

    private void Awake()
    {
        CreateFadeOverlay();

        if (fadeInOnStart)
            SetFadeAlpha(1f);
        else
            SetFadeAlpha(0f);
    }

    private IEnumerator Start()
    {
        if (fadeInOnStart)
            yield return FadeScreen(1f, 0f, initialFadeInSeconds);
    }

    private void Update()
    {
        if (isTransitioning)
            return;

        bool pressed = false;

        if (allowKeyboardOrMouseInput)
            pressed = Input.GetMouseButtonDown(0) || Input.anyKeyDown;

        if (!pressed && allowTouchInput)
            pressed = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;

        if (pressed)
            ContinueToMainMenu();
    }

    public void ContinueToMainMenu()
    {
        if (isTransitioning)
            return;

        StartCoroutine(ContinueToMainMenuRoutine());
    }

    private IEnumerator ContinueToMainMenuRoutine()
    {
        isTransitioning = true;

        // Keep this controller and its generated fade canvas alive through the scene load.
        DontDestroyOnLoad(gameObject);

        CreateFadeOverlay();
        BringFadeCanvasToFront();

        yield return FadeTapToContinueOut();

        if (afterTapFadeDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(afterTapFadeDelaySeconds);

        // Fade the splash scene to black.
        yield return FadeScreen(0f, 1f, fadeOutToMainMenuSeconds);

        if (blackHoldBeforeLoadSeconds > 0f)
            yield return new WaitForSecondsRealtime(blackHoldBeforeLoadSeconds);

        // Keep the overlay fully black while loading the next scene.
        SetFadeAlpha(1f);
        BringFadeCanvasToFront();

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(mainMenuSceneName);
        if (loadOperation == null)
        {
            Debug.LogError($"SplashScreenController could not load scene: {mainMenuSceneName}");
            yield return FadeScreen(1f, 0f, fadeInMainMenuSeconds);
            isTransitioning = false;
            yield break;
        }

        while (!loadOperation.isDone)
            yield return null;

        // The new scene is loaded now. Keep black for a few frames so the first visible
        // frame is the fade-in instead of an instant flash of the menu.
        SetFadeAlpha(1f);
        BringFadeCanvasToFront();

        int waitFrames = Mathf.Max(1, framesToWaitBeforeMainMenuFadeIn);
        for (int i = 0; i < waitFrames; i++)
            yield return null;

        // Fade from black into the main menu.
        yield return FadeScreen(1f, 0f, fadeInMainMenuSeconds);

        CleanupFadeOverlay();

        // This object only exists for the splash transition. Destroy it after the
        // main menu has faded in so it does not clutter the next scene.
        Destroy(gameObject);
    }

    private IEnumerator FadeTapToContinueOut()
    {
        if (tapToContinueCanvasGroup != null)
        {
            yield return FadeCanvasGroup(
                tapToContinueCanvasGroup,
                tapToContinueCanvasGroup.alpha,
                0f,
                tapToContinueFadeSeconds
            );

            yield break;
        }

        if (tapToContinueText != null)
        {
            yield return FadeTMPText(
                tapToContinueText,
                tapToContinueText.color.a,
                0f,
                tapToContinueFadeSeconds
            );
        }
    }

    private IEnumerator FadeScreen(float from, float to, float seconds)
    {
        CreateFadeOverlay();
        BringFadeCanvasToFront();

        fadeCanvasGroup.blocksRaycasts = true;
        fadeCanvasGroup.interactable = true;

        yield return FadeCanvasGroup(fadeCanvasGroup, from, to, seconds);

        bool fullyBlack = to >= 0.999f;
        fadeCanvasGroup.blocksRaycasts = fullyBlack;
        fadeCanvasGroup.interactable = fullyBlack;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float seconds)
    {
        if (group == null)
            yield break;

        if (seconds <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        group.alpha = from;

        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / seconds);
            group.alpha = Mathf.Lerp(from, to, SmoothStep(t));
            yield return null;
        }

        group.alpha = to;
    }

    private IEnumerator FadeTMPText(TMP_Text text, float from, float to, float seconds)
    {
        if (text == null)
            yield break;

        if (seconds <= 0f)
        {
            SetTMPTextAlpha(text, to);
            yield break;
        }

        float elapsed = 0f;
        SetTMPTextAlpha(text, from);

        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / seconds);
            SetTMPTextAlpha(text, Mathf.Lerp(from, to, SmoothStep(t)));
            yield return null;
        }

        SetTMPTextAlpha(text, to);
    }

    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private void SetTMPTextAlpha(TMP_Text text, float alpha)
    {
        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }

    private void SetFadeAlpha(float alpha)
    {
        CreateFadeOverlay();

        fadeCanvasGroup.alpha = alpha;
        fadeCanvasGroup.blocksRaycasts = alpha > 0.99f;
        fadeCanvasGroup.interactable = alpha > 0.99f;
    }

    private void CreateFadeOverlay()
    {
        if (fadeCanvasGroup != null)
            return;

        GameObject canvasObject = new GameObject("SplashTransitionFadeCanvas");
        DontDestroyOnLoad(canvasObject);

        fadeCanvas = canvasObject.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.overrideSorting = true;
        fadeCanvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2340f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject imageObject = new GameObject("FullScreenBlackFade");
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform imageRect = imageObject.AddComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = Vector2.zero;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        fadeImage = imageObject.AddComponent<Image>();
        fadeImage.sprite = null;
        fadeImage.type = Image.Type.Simple;
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = true;

        fadeCanvasGroup = imageObject.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;
    }

    private void BringFadeCanvasToFront()
    {
        if (fadeCanvas == null)
            return;

        fadeCanvas.overrideSorting = true;
        fadeCanvas.sortingOrder = short.MaxValue;

        if (fadeCanvas.transform != null)
            fadeCanvas.transform.SetAsLastSibling();

        if (fadeCanvasGroup != null)
            fadeCanvasGroup.transform.SetAsLastSibling();
    }

    private void CleanupFadeOverlay()
    {
        if (fadeCanvas != null)
        {
            Destroy(fadeCanvas.gameObject);
            fadeCanvas = null;
        }

        fadeCanvasGroup = null;
        fadeImage = null;
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        if (isQuitting)
            return;

        // If the object is destroyed manually before finishing, avoid leaving a stale
        // transition canvas behind.
        if (!isTransitioning)
            CleanupFadeOverlay();
    }
}
