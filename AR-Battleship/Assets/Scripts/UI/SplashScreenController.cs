using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the full splash-screen flow: initial fade-in, wait for input,
/// crossfade out, async scene load, and final fade-in on the main menu.
/// </summary>
public class SplashScreenController : MonoBehaviour
{

    [Header("Scene")]

    /// <summary>
    /// Build name of the main-menu scene to load after the splash.
    /// Must appear in File → Build Settings → Scenes In Build.
    /// </summary>
    [SerializeField] private string mainMenuSceneName = "0MainMenuScreen";

    [Header("Tap To Continue")]

    /// <summary>
    /// Optional TMP label that displays "TAP TO CONTINUE" or similar.
    /// If <see cref="tapToContinueCanvasGroup"/> is assigned, this field
    /// is ignored and the CanvasGroup controls the fade instead.
    /// </summary>
    [SerializeField] private TMP_Text tapToContinueText;

    /// <summary>
    /// Optional CanvasGroup wrapping the tap-to-continue artwork.
    /// When assigned, the CanvasGroup's alpha is animated rather than
    /// modifying the TMP_Text color directly.
    /// </summary>
    [SerializeField] private CanvasGroup tapToContinueCanvasGroup;

    /// <summary>Duration in seconds for the tap-to-continue element to fade out.</summary>
    [SerializeField] private float tapToContinueFadeSeconds = 0.45f;

    /// <summary>
    /// Short hold between the tap-to-continue fade finishing and the full-screen
    /// fade beginning.  Prevents the two fades from feeling simultaneous.
    /// </summary>
    [SerializeField] private float afterTapFadeDelaySeconds = 0.08f;

    [Header("Screen Fade")]

    /// <summary>When true, the screen starts black and fades to clear on Start.</summary>
    [SerializeField] private bool fadeInOnStart = true;

    /// <summary>Duration of the initial fade from black to transparent at scene open.</summary>
    [SerializeField] private float initialFadeInSeconds = 0.65f;

    /// <summary>Duration of the fade to black that begins immediately after the player taps.</summary>
    [SerializeField] private float fadeOutToMainMenuSeconds = 1.15f;

    /// <summary>
    /// Seconds the screen stays fully black between the fade-out finishing and the
    /// async scene load beginning.  Provides a brief rest to prevent a jarring cut.
    /// </summary>
    [SerializeField] private float blackHoldBeforeLoadSeconds = 0.15f;

    /// <summary>Duration of the fade from black into the main menu once it has loaded.</summary>
    [SerializeField] private float fadeInMainMenuSeconds = 0.85f;

    /// <summary>
    /// Number of frames to keep the fade overlay fully black after the main menu
    /// scene has finished loading.  Prevents a single-frame flash of the menu
    /// before the fade-in begins.
    /// </summary>
    [SerializeField] private int framesToWaitBeforeMainMenuFadeIn = 3;

    [Header("Input")]

    /// <summary>Allow any mouse button or keyboard key to trigger the transition.</summary>
    [SerializeField] private bool allowKeyboardOrMouseInput = true;

    /// <summary>Allow a touch-screen tap to trigger the transition.</summary>
    [SerializeField] private bool allowTouchInput = true;

    /// <summary>The runtime-generated Canvas used for the full-screen black overlay.</summary>
    private Canvas fadeCanvas;

    /// <summary>CanvasGroup on the overlay image; controls alpha and raycasting.</summary>
    private CanvasGroup fadeCanvasGroup;

    /// <summary>Full-screen black Image inside the fade canvas.</summary>
    private Image fadeImage;

    /// <summary>True while the coroutine is actively transitioning; blocks duplicate triggers.</summary>
    private bool isTransitioning;

    /// <summary>Set by OnApplicationQuit to prevent CleanupFadeOverlay from running on quit.</summary>
    private bool isQuitting;

    /// <summary>
    /// Creates the persistent fade overlay and sets its initial alpha
    /// based on the fadeInOnStart setting.
    /// </summary>
    private void Awake()
    {
        CreateFadeOverlay();

        // Start fully black if we intend to fade in; otherwise start transparent.
        if (fadeInOnStart)
            SetFadeAlpha(1f);
        else
            SetFadeAlpha(0f);
    }

    /// <summary>
    /// Runs the optional fade-in from black when the splash scene opens.
    /// Uses a coroutine return type so the fade plays on the first frame.
    /// </summary>
    private IEnumerator Start()
    {
        if (fadeInOnStart)
            yield return FadeScreen(1f, 0f, initialFadeInSeconds);
    }

    /// <summary>
    /// Polls for player input each frame and triggers the transition once
    /// the player taps or presses any key (provided no transition is in flight).
    /// </summary>
    private void Update()
    {
        // Skip input polling while a transition is already running.
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

    /// <summary>
    /// Starts the transition coroutine that fades out the splash content,
    /// loads the main menu scene, then fades in.
    /// Safe to call from a Button onClick in addition to the Update polling.
    /// </summary>
    public void ContinueToMainMenu()
    {
        if (isTransitioning)
            return;

        StartCoroutine(ContinueToMainMenuRoutine());
    }

    /// <summary>
    /// Full transition pipeline from splash to main menu:
    ///   fade tap text → hold → fade to black → async load → wait frames → fade in.
    /// </summary>
    private IEnumerator ContinueToMainMenuRoutine()
    {
        isTransitioning = true;

        // Keep this controller and its generated fade canvas alive through the scene load.
        DontDestroyOnLoad(gameObject);

        CreateFadeOverlay();
        BringFadeCanvasToFront();

        // 1. Fade out the "Tap to Continue" element.
        yield return FadeTapToContinueOut();

        // 2. Brief pause between the tap indicator fading and the full-screen blackout.
        if (afterTapFadeDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(afterTapFadeDelaySeconds);

        // 3. Fade the entire splash scene to black.
        yield return FadeScreen(0f, 1f, fadeOutToMainMenuSeconds);

        // 4. Hold on black before initiating the scene load.
        if (blackHoldBeforeLoadSeconds > 0f)
            yield return new WaitForSecondsRealtime(blackHoldBeforeLoadSeconds);

        // 5. Keep the overlay fully black while loading the next scene.
        SetFadeAlpha(1f);
        BringFadeCanvasToFront();

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(mainMenuSceneName);
        if (loadOperation == null)
        {
            // Scene name may not be in Build Settings; log and recover gracefully.
            Debug.LogError($"SplashScreenController could not load scene: {mainMenuSceneName}");
            yield return FadeScreen(1f, 0f, fadeInMainMenuSeconds);
            isTransitioning = false;
            yield break;
        }

        // Wait for the scene to finish loading in the background.
        while (!loadOperation.isDone)
            yield return null;

        // 6. The new scene is loaded now. Keep black for a few frames so the first visible
        //    frame is the fade-in instead of an instant flash of the menu.
        SetFadeAlpha(1f);
        BringFadeCanvasToFront();

        int waitFrames = Mathf.Max(1, framesToWaitBeforeMainMenuFadeIn);
        for (int i = 0; i < waitFrames; i++)
            yield return null;

        // 7. Fade from black into the main menu.
        yield return FadeScreen(1f, 0f, fadeInMainMenuSeconds);

        // 8. Clean up the overlay canvas and this controller object.
        CleanupFadeOverlay();

        // This object only exists for the splash transition. Destroy it after the
        // main menu has faded in so it does not clutter the next scene.
        Destroy(gameObject);
    }

    /// <summary>
    /// Fades out the tap-to-continue element, preferring the CanvasGroup
    /// approach over direct TMP color manipulation if a CanvasGroup is assigned.
    /// </summary>
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

        // Fallback: animate the TMP_Text color alpha directly.
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

    /// <summary>
    /// Animates the full-screen fade overlay from <paramref name="from"/>
    /// to <paramref name="to"/> alpha over <paramref name="seconds"/> seconds.
    /// Blocks raycasts while the overlay is opaque.
    /// </summary>
    private IEnumerator FadeScreen(float from, float to, float seconds)
    {
        CreateFadeOverlay();
        BringFadeCanvasToFront();

        // Block input during any fade so the player cannot double-trigger.
        fadeCanvasGroup.blocksRaycasts = true;
        fadeCanvasGroup.interactable   = true;

        yield return FadeCanvasGroup(fadeCanvasGroup, from, to, seconds);

        // Only leave raycasts blocked when the overlay is fully black.
        bool fullyBlack = to >= 0.999f;
        fadeCanvasGroup.blocksRaycasts = fullyBlack;
        fadeCanvasGroup.interactable   = fullyBlack;
    }

    /// <summary>
    /// Generic coroutine that lerps <paramref name="group"/>.alpha from
    /// <paramref name="from"/> to <paramref name="to"/> using SmoothStep easing
    /// over <paramref name="seconds"/> real-time seconds.
    /// </summary>
    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float seconds)
    {
        if (group == null) yield break;

        if (seconds <= 0f)
        {
            // Zero duration: snap immediately without waiting a frame.
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        group.alpha = from;

        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime; // Unscaled so pausing doesn't break fades
            float t = Mathf.Clamp01(elapsed / seconds);
            group.alpha = Mathf.Lerp(from, to, SmoothStep(t));
            yield return null;
        }

        group.alpha = to; // Guarantee exact final value
    }

    /// <summary>
    /// Lerps the alpha channel of a TMP_Text's color without touching RGB.
    /// Used as a fallback when no CanvasGroup is assigned to the tap-to-continue UI.
    /// </summary>
    private IEnumerator FadeTMPText(TMP_Text text, float from, float to, float seconds)
    {
        if (text == null) yield break;

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

    /// <summary>
    /// Hermite cubic smoothstep: f(t) = t² (3 − 2t).
    /// Produces an ease-in / ease-out curve without Unity's Mathf.SmoothStep
    /// which clamps its input differently.
    /// </summary>
    /// <param name="t">Normalised time in [0, 1].</param>
    /// <returns>Eased value in [0, 1].</returns>
    private float SmoothStep(float t) => t * t * (3f - 2f * t);

    /// <summary>Sets only the alpha channel of a TMP_Text, leaving RGB unchanged.</summary>
    private void SetTMPTextAlpha(TMP_Text text, float alpha)
    {
        Color color = text.color;
        color.a     = alpha;
        text.color  = color;
    }

    /// <summary>
    /// Immediately sets the fade overlay to the given alpha and updates
    /// raycasting to match (blocks all input while fully opaque).
    /// </summary>
    private void SetFadeAlpha(float alpha)
    {
        CreateFadeOverlay();
        fadeCanvasGroup.alpha          = alpha;
        fadeCanvasGroup.blocksRaycasts = alpha > 0.99f;
        fadeCanvasGroup.interactable   = alpha > 0.99f;
    }

    /// <summary>
    /// Creates the full-screen black fade Canvas if it does not already exist.
    /// The Canvas is marked DontDestroyOnLoad so it persists into the main menu.
    /// Sorting order is set to short.MaxValue to guarantee it renders above
    /// everything else, including any other Canvases added later.
    /// </summary>
    private void CreateFadeOverlay()
    {
        // Guard: only create once per session.
        if (fadeCanvasGroup != null)
            return;

        // Create the top-level Canvas object that will persist across scenes.
        GameObject canvasObject = new GameObject("SplashTransitionFadeCanvas");
        DontDestroyOnLoad(canvasObject);

        fadeCanvas                 = canvasObject.AddComponent<Canvas>();
        fadeCanvas.renderMode      = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.overrideSorting = true;
        fadeCanvas.sortingOrder    = short.MaxValue; // Always on top

        // CanvasScaler ensures the overlay covers the full screen on all devices.
        CanvasScaler scaler              = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode               = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution       = new Vector2(1080f, 2340f);
        scaler.screenMatchMode           = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight        = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        // Create a full-screen black Image that the CanvasGroup will fade.
        GameObject imageObject = new GameObject("FullScreenBlackFade");
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform imageRect       = imageObject.AddComponent<RectTransform>();
        imageRect.anchorMin           = Vector2.zero;
        imageRect.anchorMax           = Vector2.one;
        imageRect.pivot               = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition    = Vector2.zero;
        imageRect.sizeDelta           = Vector2.zero;
        imageRect.offsetMin           = Vector2.zero;
        imageRect.offsetMax           = Vector2.zero;

        fadeImage              = imageObject.AddComponent<Image>();
        fadeImage.sprite       = null;
        fadeImage.type         = Image.Type.Simple;
        fadeImage.color        = Color.black;
        fadeImage.raycastTarget = true;

        // CanvasGroup on the image controls alpha and raycast blocking.
        fadeCanvasGroup                = imageObject.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha          = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable   = false;
    }

    /// <summary>
    /// Ensures the fade Canvas has the highest sorting order and is the last
    /// sibling in the hierarchy.  Must be called after any scene load that
    /// might introduce new Canvases.
    /// </summary>
    private void BringFadeCanvasToFront()
    {
        if (fadeCanvas == null) return;

        fadeCanvas.overrideSorting = true;
        fadeCanvas.sortingOrder    = short.MaxValue;

        if (fadeCanvas.transform != null)
            fadeCanvas.transform.SetAsLastSibling();

        if (fadeCanvasGroup != null)
            fadeCanvasGroup.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Destroys the fade Canvas and nulls all references.
    /// Called after the main menu has fully faded in.
    /// </summary>
    private void CleanupFadeOverlay()
    {
        if (fadeCanvas != null)
        {
            Destroy(fadeCanvas.gameObject);
            fadeCanvas = null;
        }

        fadeCanvasGroup = null;
        fadeImage       = null;
    }

    /// <summary>Records that the application is quitting so OnDestroy skips cleanup.</summary>
    private void OnApplicationQuit() => isQuitting = true;

    /// <summary>
    /// If the object is destroyed manually mid-transition (e.g. during scene reload
    /// in the Editor), cleans up the standalone fade Canvas to avoid orphaned objects.
    /// No-ops during normal application quit.
    /// </summary>
    private void OnDestroy()
    {
        if (isQuitting) return;

        // If the object is destroyed manually before finishing, avoid leaving a stale
        // transition canvas behind.
        if (!isTransitioning)
            CleanupFadeOverlay();
    }
}
