using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TapToContinueAudio : MonoBehaviour
{
    [SerializeField] private AudioClip tapSound;
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;
    [SerializeField] private AudioSource audioSource;

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