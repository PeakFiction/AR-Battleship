using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays a one-shot audio clip when a Button on the same GameObject is pressed.
/// Requires a Button component on the same GameObject.
/// </summary>
[RequireComponent(typeof(Button))]
public class TapToContinueAudio : MonoBehaviour
{

    /// <summary>AudioClip to play when the button is tapped.</summary>
    [SerializeField] private AudioClip tapSound;

    /// <summary>Volume scalar for PlayOneShot (range 0–1).</summary>
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

    /// <summary>
    /// AudioSource used to play the tap sound.
    /// If not assigned in the Inspector, Awake will attempt to find one on
    /// the same GameObject.
    /// </summary>
    [SerializeField] private AudioSource audioSource;

    /// <summary>
    /// Resolves the AudioSource and registers the tap-sound listener on the
    /// Button component.  Runs before Start so the listener is active even if
    /// the button triggers immediate scene destruction.
    /// </summary>
    private void Awake()
    {
        Debug.Log("TapToContinueAudio Awake");

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        Debug.Log($"AudioSource found: {audioSource != null}");

        Button button = GetComponent<Button>();
        Debug.Log($"Button found: {button != null}");

        if (button != null)
        {
            button.onClick.AddListener(PlayTapSound);
            Debug.Log("Listener added to button");
        }
    }

    /// <summary>
    /// Plays <see cref="tapSound"/> as a one-shot clip through <see cref="audioSource"/>.
    /// Logs a warning if either the source or clip is missing.
    /// </summary>
    private void PlayTapSound()
    {
        Debug.Log("PlayTapSound called");
        Debug.Log($"audioSource: {audioSource}, tapSound: {tapSound}");

        if (audioSource != null && tapSound != null)
        {
            audioSource.PlayOneShot(tapSound, volume);
            Debug.Log("Sound played");
        }
        else
        {
            Debug.LogWarning("Missing audioSource or tapSound");
        }
    }
}
