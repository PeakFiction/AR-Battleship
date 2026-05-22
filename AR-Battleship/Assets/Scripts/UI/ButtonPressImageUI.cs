using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Swaps two child GameObjects to represent the normal and clicked visual
/// states of a button.  Implements IPointerDownHandler, IPointerUpHandler, and
/// IPointerExitHandler to receive Unity EventSystem callbacks directly.
/// </summary>
public class ChildClickedStateButton : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{

    [Header("State Objects")]

    /// <summary>
    /// The default / unpressed visual child.
    /// Typically the child named "Button" in the hierarchy.
    /// Active when <see cref="isClicked"/> is false.
    /// </summary>
    [Tooltip("The normal button visual. Usually the child named Button.")]
    [SerializeField] private GameObject normalObject;

    /// <summary>
    /// The pressed / toggled-on visual child.
    /// Typically named "StartButtonClicked" or "BackButtonClicked".
    /// Active when <see cref="isClicked"/> is true.
    /// </summary>
    [Tooltip("The clicked/pressed visual. Usually the child named StartButtonClicked or BackButtonClicked.")]
    [SerializeField] private GameObject clickedObject;

    [Header("Behavior")]

    /// <summary>
    /// When true, each pointer-down toggles the clicked state persistently
    /// (suitable for on/off toggle buttons such as audio mute).
    /// When false, the clicked state reverts on pointer-up or pointer-exit.
    /// </summary>
    [SerializeField] private bool holdClickedState = false;

    /// <summary>
    /// When true and <see cref="holdClickedState"/> is false, moving the pointer
    /// off the button while still pressed reverts the visual to the normal state.
    /// Set to false for drag-style interactions.
    /// </summary>
    [SerializeField] private bool resetOnPointerExit = true;

    [Header("Optional Audio")]

    /// <summary>
    /// AudioSource used to play the click chime.
    /// Automatically resolved from the same GameObject if left unassigned.
    /// </summary>
    [SerializeField] private AudioSource audioSource;

    /// <summary>AudioClip played on every pointer-down event.</summary>
    [SerializeField] private AudioClip clickChime;

    /// <summary>Volume scalar passed to AudioSource.PlayOneShot (range 0–1).</summary>
    [SerializeField, Range(0f, 1f)] private float chimeVolume = 0.8f;

    /// <summary>True when the button is currently in its clicked/toggled-on state.</summary>
    private bool isClicked;

    /// <summary>
    /// Resolves the AudioSource from the same GameObject if not manually assigned,
    /// then initialises the button to its default (un-clicked) visual state.
    /// </summary>
    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Always start in the normal (unclicked) state.
        ApplyState(false);
    }

    /// <summary>
    /// Plays the click chime and toggles or activates the clicked visual state
    /// when the pointer is pressed down over this button.
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        PlayChime();

        if (holdClickedState)
            SetClicked(!isClicked); // Toggle the persistent state
        else
            SetClicked(true);       // Momentary press
    }

    /// <summary>
    /// Returns the button to its normal state on pointer release,
    /// unless <see cref="holdClickedState"/> keeps it toggled.
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (holdClickedState) return;
        SetClicked(false);
    }

    /// <summary>
    /// Resets to the normal state when the pointer leaves the button area,
    /// respecting the <see cref="holdClickedState"/> and <see cref="resetOnPointerExit"/> flags.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (holdClickedState || !resetOnPointerExit) return;
        SetClicked(false);
    }

    /// <summary>
    /// Programmatically sets the clicked state.
    /// Can be called from other scripts to synchronise button visuals with
    /// application state (e.g. reflecting a saved setting on scene load).
    /// </summary>
    /// <param name="clicked">True to show the clicked visual; false for normal.</param>
    public void SetClicked(bool clicked)
    {
        isClicked = clicked;
        ApplyState(clicked);
    }

    /// <summary>
    /// Inverts the current clicked state and plays the click chime.
    /// Convenient for linking to a Toggle or secondary button via UnityEvent.
    /// </summary>
    public void ToggleClicked()
    {
        SetClicked(!isClicked);
        PlayChime();
    }

    /// <summary>
    /// Returns the button to the normal (un-clicked) state without playing audio.
    /// Useful for resetting a toggle group when another option is selected.
    /// </summary>
    public void ResetClicked() => SetClicked(false);

    /// <summary>
    /// Activates the appropriate child GameObject based on the clicked state.
    /// Null-checks on each object prevent errors if they are not assigned in
    /// the Inspector.
    /// </summary>
    /// <param name="clicked">True = show clickedObject; false = show normalObject.</param>
    private void ApplyState(bool clicked)
    {
        if (normalObject  != null) normalObject.SetActive(!clicked);
        if (clickedObject != null) clickedObject.SetActive(clicked);
    }

    /// <summary>
    /// Plays <see cref="clickChime"/> as a one-shot through <see cref="audioSource"/>
    /// if both are assigned.  PlayOneShot allows overlapping playback without
    /// interrupting music or other UI sounds.
    /// </summary>
    private void PlayChime()
    {
        if (audioSource != null && clickChime != null)
            audioSource.PlayOneShot(clickChime, chimeVolume);
    }
}
