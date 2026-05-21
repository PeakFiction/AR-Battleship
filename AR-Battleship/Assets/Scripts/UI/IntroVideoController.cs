using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class IntroVideoController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string nextSceneName = "0SplashScreen";
    [SerializeField] private Image fadePanel;
    [SerializeField] private float fadeDuration = 0.5f;

    private bool hasSkipped = false;

    private void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        videoPlayer.loopPointReached += OnVideoEnd;
        videoPlayer.Play();

        if (fadePanel != null)
        {
            Color c = fadePanel.color;
            c.a = 0f;
            fadePanel.color = c;
        }
    }

    private void Update()
    {
        if (hasSkipped) return;

        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            SkipToNextScene();
        }
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        SkipToNextScene();
    }

    private void SkipToNextScene()
    {
        if (hasSkipped) return;
        hasSkipped = true;
        StartCoroutine(FadeAndLoad());
    }

    private IEnumerator FadeAndLoad()
    {
        if (fadePanel != null)
        {
            float elapsed = 0f;
            Color c = fadePanel.color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                fadePanel.color = c;
                yield return null;
            }

            c.a = 1f;
            fadePanel.color = c;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}