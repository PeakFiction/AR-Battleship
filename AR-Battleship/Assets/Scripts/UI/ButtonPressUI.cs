using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Animated press feedback for Canvas UI buttons.
/// Combines scale animation, colour swaps, optional background visibility,
/// and audio in a single component that requires a <see cref="Button"/>.
/// </summary>
[RequireComponent(typeof(Button))]
public class CanvasUIButtonFeedback : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{

    [Header("Canvas UI Parts")]

    /// <summary>TMP label on the button.  Its colour is swapped on press.</summary>
    [SerializeField] private TMP_Text label;

    /// <summary>
    /// Small square Image indicator (e.g. a coloured dot).
    /// Its colour is swapped independently of the label.
    /// </summary>
    [SerializeField] private Image indicatorSquare;

    [Header("Clicked Background")]

    /// <summary>
    /// Optional child GameObject that appears while the button is pressed.
    /// Typically a semi-transparent highlight behind the label.
    /// </summary>
    [SerializeField] private GameObject clickedBackground;

    /// <summary>
    /// When true, <see cref="clickedBackground"/> is hidden the moment the pointer
    /// is released.  When false, it stays until <see cref="ApplyVisual"/> is
    /// called with clicked = false (useful for toggle buttons).
    /// </summary>
    [SerializeField] private bool showBackgroundOnlyWhilePressed = true;

    [Header("Visual State")]

    /// <summary>
    /// When true, the button remembers its pressed state across pointer events,
    /// acting as a toggle.  Subsequent taps flip the state.
    /// </summary>
    [SerializeField] private bool holdClickedState;

    /// <summary>Label colour in the default (un-pressed) state.</summary>
    [SerializeField] private Color normalTextColor = Color.white;

    /// <summary>Label colour when the button is pressed or toggled on.</summary>
    [SerializeField] private Color clickedTextColor = Color.black;

    /// <summary>Indicator square colour in the default state.</summary>
    [SerializeField] private Color normalSquareColor = Color.white;

    /// <summary>Indicator square colour when pressed or toggled on.</summary>
    [SerializeField] private Color clickedSquareColor = Color.black;

    [Header("Motion")]

    /// <summary>
    /// Scale multiplier applied to the button's RectTransform while pressed
    /// (e.g. 0.98 = 2 % shrink).  Values &gt; 1 expand the button.
    /// </summary>
    [SerializeField] private float clickedScale = 0.98f;

    /// <summary>
    /// Lerp speed for the scale animation (higher = snappier).
    /// Applied per-frame as: scale = Lerp(current, target, speed * unscaledDt).
    /// </summary>
    [SerializeField] private float animationSpeed = 18f;

    /// <summary>
    /// Seconds to wait after pointer-up before reverting the visual state.
    /// A small delay (≈80 ms) ensures the pressed frame is visible even for
    /// quick taps.
    /// </summary>
    [SerializeField] private float releaseDelay = 0.08f;

    [Header("Audio")]

    /// <summary>
    /// AudioSource for the click chime.  If not assigned, falls back to the
    /// SFXManager singleton's shared source (if present in the scene).
    /// </summary>
    [SerializeField] private AudioSource audioSource;

    /// <summary>AudioClip played on every pointer-down event.</summary>
    [SerializeField] private AudioClip clickChime;

    /// <summary>Volume scalar for PlayOneShot (range 0–1).</summary>
    [SerializeField, Range(0f, 1f)] private float chimeVolume = 0.8f;

    /// <summary>Reference to the sibling Button component for interactable checks.</summary>
    private Button button;

    /// <summary>Cached RectTransform used for scale animation.</summary>
    private RectTransform rectTransform;

    /// <summary>Scale at Awake – used as the target scale when the button is released.</summary>
    private Vector3 normalScale;

    /// <summary>Handle for the currently running scale animation coroutine (if any).</summary>
    private Coroutine visualRoutine;

    /// <summary>Handle for the currently running release-delay coroutine (if any).</summary>
    private Coroutine releaseRoutine;

    /// <summary>True when the button is in its pressed / toggled-on state.</summary>
    private bool isClicked;

    /// <summary>
    /// Caches component references, resolves the AudioSource, and sets the
    /// button to its initial (un-clicked) visual state.
    /// </summary>
    private void Awake()
    {
        button        = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        normalScale   = rectTransform.localScale;

        // Attempt to auto-find the label if not wired up in the Inspector.
        if (label == null)
            label = GetComponentInChildren<TMP_Text>(true);

        // Resolve audio source: explicit assignment > same GameObject > SFXManager.
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null && SFXManager.Instance != null)
                audioSource = SFXManager.Instance.GetAudioSource();
        }

        // Show the button in its default visual state immediately, without animation.
        ApplyVisual(false, instant: true);
    }

    /// <summary>
    /// Plays the click chime and activates the pressed visual state when the
    /// pointer is pressed on the button.  Ignores events when the button is
    /// non-interactable.
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable) return;

        PlayChime();

        if (holdClickedState)
            SetClicked(!isClicked); // Toggle persistent state
        else
            ApplyVisual(true);      // Momentary press visual
    }

    /// <summary>
    /// Starts a short release delay before reverting the visual state.
    /// Skipped in toggle (holdClickedState) mode.
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!button.interactable || holdClickedState) return;
        StartRelease();
    }

    /// <summary>
    /// Cancels the pressed visual when the pointer leaves the button area.
    /// Provides consistent feel when the user drags off the button mid-press.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!button.interactable || holdClickedState) return;
        StartRelease();
    }

    /// <summary>
    /// Programmatically sets the clicked / toggled state.
    /// Updates colours, background, and scale immediately (animated).
    /// </summary>
    public void SetClicked(bool clicked)
    {
        isClicked = clicked;
        ApplyVisual(clicked);
    }

    /// <summary>Inverts the current state and plays the click chime.</summary>
    public void ToggleClicked()
    {
        SetClicked(!isClicked);
        PlayChime();
    }

    /// <summary>Returns the button to the default (un-clicked) state.</summary>
    public void ResetClicked() => SetClicked(false);

    /// <summary>
    /// Stops any existing release coroutine and starts a new one.
    /// Prevents competing coroutines if the pointer-up and pointer-exit both
    /// fire in the same frame.
    /// </summary>
    private void StartRelease()
    {
        if (releaseRoutine != null)
            StopCoroutine(releaseRoutine);

        releaseRoutine = StartCoroutine(ReleaseAfterDelay());
    }

    /// <summary>
    /// Waits for <see cref="releaseDelay"/> real-time seconds then reverts
    /// the visual to the normal state.  The short delay ensures the pressed
    /// frame is visible even for very fast taps.
    /// </summary>
    private IEnumerator ReleaseAfterDelay()
    {
        yield return new WaitForSecondsRealtime(releaseDelay);
        ApplyVisual(false);
    }

    /// <summary>
    /// Applies the full visual state: background visibility, label colour,
    /// indicator colour, and scale animation.
    /// </summary>
    /// <param name="clicked">Target visual state.</param>
    /// <param name="instant">
    /// When true, the scale is set immediately without animation
    /// (used during Awake to avoid a one-frame flicker).
    /// </param>
    private void ApplyVisual(bool clicked, bool instant = false)
    {
        // Show or hide the optional pressed-state background.
        if (clickedBackground != null)
            clickedBackground.SetActive(clicked);

        // Swap label and indicator colours.
        if (label          != null) label.color          = clicked ? clickedTextColor   : normalTextColor;
        if (indicatorSquare != null) indicatorSquare.color = clicked ? clickedSquareColor : normalSquareColor;

        Vector3 targetScale = clicked ? normalScale * clickedScale : normalScale;

        if (instant)
        {
            // Snap to target scale without animation.
            rectTransform.localScale = targetScale;
            return;
        }

        // Cancel any in-flight scale animation and start a new one.
        if (visualRoutine != null)
            StopCoroutine(visualRoutine);

        visualRoutine = StartCoroutine(AnimateScale(targetScale));
    }

    /// <summary>
    /// Lerps the RectTransform's local scale toward <paramref name="targetScale"/>
    /// each frame until within 0.001 units, then snaps to the exact target.
    /// Uses unscaled delta time so Time.timeScale = 0 cannot freeze the animation.
    /// </summary>
    private IEnumerator AnimateScale(Vector3 targetScale)
    {
        while (Vector3.Distance(rectTransform.localScale, targetScale) > 0.001f)
        {
            rectTransform.localScale = Vector3.Lerp(
                rectTransform.localScale,
                targetScale,
                Time.unscaledDeltaTime * animationSpeed
            );
            yield return null;
        }

        // Guarantee the exact target value at the end to avoid floating-point drift.
        rectTransform.localScale = targetScale;
    }

    /// <summary>Plays the click chime if both the source and clip are available.</summary>
    private void PlayChime()
    {
        if (audioSource != null && clickChime != null)
            audioSource.PlayOneShot(clickChime, chimeVolume);
    }
}
