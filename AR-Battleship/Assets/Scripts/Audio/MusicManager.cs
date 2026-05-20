using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music Clips")]
    [SerializeField] private AudioClip menuAndSetupMusic;
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip dangerMusic;
    [SerializeField] private AudioClip victoryMusic;
    [SerializeField] private AudioClip lossMusic;

    [Header("Settings")]
    [SerializeField, Range(0f, 1f)] private float volume = 0.45f;

    private AudioSource audioSource;
    private AudioClip currentClip;

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
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volume;
    }

    public void PlayMenuAndSetupMusic()
    {
        PlayLoop(menuAndSetupMusic);
    }

    public void PlayGameplayMusic()
    {
        PlayLoop(gameplayMusic);
    }

    public void PlayDangerMusic()
    {
        PlayLoop(dangerMusic);
    }

    public void PlayVictoryMusic()
    {
        PlayLoop(victoryMusic);
    }

    public void PlayLossMusic()
    {
        PlayLoop(lossMusic);
    }

    public void StopMusic()
    {
        audioSource.Stop();
        currentClip = null;
    }

    private void PlayLoop(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("Music clip is missing.");
            return;
        }

        if (currentClip == clip && audioSource.isPlaying)
        {
            return;
        }

        currentClip = clip;
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.volume = volume;
        audioSource.Play();
    }
}