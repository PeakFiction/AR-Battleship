using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class IntroVideoController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string nextSceneName = "0SplashScreen";

    private bool hasSkipped = false;

    private void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        videoPlayer.loopPointReached += OnVideoEnd;
        videoPlayer.Play();
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
        SceneManager.LoadScene(nextSceneName);
    }
}