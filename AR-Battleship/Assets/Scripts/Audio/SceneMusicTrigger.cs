using System.Collections;
using UnityEngine;

public class SceneMusicTrigger : MonoBehaviour
{
    public enum MusicType
    {
        MenuAndSetup,
        Gameplay,
        Stop
    }

    [SerializeField] private MusicType musicType = MusicType.MenuAndSetup;

    private IEnumerator Start()
    {
        yield return null;

        if (MusicManager.Instance == null)
        {
            Debug.LogWarning("No MusicManager found in scene.");
            yield break;
        }

        switch (musicType)
        {
            case MusicType.MenuAndSetup:
                MusicManager.Instance.PlayMenuAndSetupMusic();
                Debug.Log("SceneMusicTrigger: playing MenuAndSetup music.");
                break;

            case MusicType.Gameplay:
                MusicManager.Instance.PlayGameplayMusic();
                Debug.Log("SceneMusicTrigger: playing Gameplay music.");
                break;

            case MusicType.Stop:
                MusicManager.Instance.StopMusic();
                Debug.Log("SceneMusicTrigger: stopping music.");
                break;
        }
    }
}