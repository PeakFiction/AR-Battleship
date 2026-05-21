using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    public AudioSource GetAudioSource()
    {
        return audioSource;
    }
}