using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Plays an intro video and transitions to another scene when it finishes
/// or when the player taps / clicks to skip.
/// </summary>
public class IntroVideoController : MonoBehaviour
{

    /// <summary>
    /// The VideoPlayer component that renders the intro clip.
    /// Auto-detected from the same GameObject if not manually assigned.
    /// </summary>
    [SerializeField] private VideoPlayer videoPlayer;

    /// <summary>
    /// Build name of the scene to load after the intro finishes or is skipped.
    /// Must appear in File → Build Settings → Scenes In Build.
    /// </summary>
    [SerializeField] private string nextSceneName = "0SplashScreen";

    /// <summary>
    /// Full-screen Image used to fade to black before the scene transition.
    /// Leave null to skip the fade and cut immediately.
    /// </summary>
    [SerializeField] private Image fadePanel;

    /// <summary>Duration in seconds of the black fade-out animation.</summary>
    [SerializeField] private float fadeDuration = 0.5f;

    /// <summary>
    /// True once the skip / transition has been triggered.
    /// Prevents multiple concurrent FadeAndLoad coroutines from launching.
    /// </summary>
    private bool hasSkipped = false;

    /// <summary>
    /// Resolves the VideoPlayer, subscribes to its end event, starts playback,
    /// and ensures the fade panel starts at alpha 0 (fully transparent).
    /// </summary>
    private void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        // Subscribe to the loop-point event so the scene transition fires
        // automatically when the clip finishes.
        videoPlayer.loopPointReached += OnVideoEnd;
        videoPlayer.Play();

        // Ensure the fade overlay is invisible at start.
        if (fadePanel != null)
        {
            Color c = fadePanel.color;
            c.a = 0f;
            fadePanel.color = c;
        }
    }

    /// <summary>
    /// Checks for skip input each frame (mouse click or screen touch).
    /// Skips are ignored once the transition has already started.
    /// </summary>
    private void Update()
    {
        if (hasSkipped) return;

        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            SkipToNextScene();
    }

    /// <summary>
    /// VideoPlayer loopPointReached callback – triggers the transition when
    /// the clip reaches its end naturally.
    /// </summary>
    /// <param name="vp">The VideoPlayer that finished playback (unused).</param>
    private void OnVideoEnd(VideoPlayer vp) => SkipToNextScene();

    /// <summary>
    /// Sets the hasSkipped guard and starts the fade-and-load coroutine.
    /// The guard ensures only one transition runs even if both the video end
    /// event and a user tap fire in the same frame.
    /// </summary>
    private void SkipToNextScene()
    {
        if (hasSkipped) return;
        hasSkipped = true;
        StartCoroutine(FadeAndLoad());
    }

    /// <summary>
    /// Fades the screen to black over <see cref="fadeDuration"/> seconds,
    /// then loads <see cref="nextSceneName"/>.
    /// If no fade panel is assigned, the scene loads immediately.
    /// </summary>
    private IEnumerator FadeAndLoad()
    {
        if (fadePanel != null)
        {
            float elapsed = 0f;
            Color c = fadePanel.color;

            // Animate the fade panel from transparent to opaque.
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                fadePanel.color = c;
                yield return null;
            }

            // Guarantee fully opaque before loading.
            c.a = 1f;
            fadePanel.color = c;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
