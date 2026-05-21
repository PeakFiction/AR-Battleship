using UnityEngine;
using UnityEngine.UI;

public class FirstTimePopup : MonoBehaviour
{
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button guideButton;
    
    private const string FIRST_TIME_KEY = "HasSeenGuidePopup";
    private bool shouldProceed = false;

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseAndProceed);
        
        if (guideButton != null)
            guideButton.onClick.AddListener(OpenGuideAndClose);
            
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    public void OnStartButtonPressed()
    {
        if (!PlayerPrefs.HasKey(FIRST_TIME_KEY))
        {
            ShowPopup();
            PlayerPrefs.SetInt(FIRST_TIME_KEY, 1);
            PlayerPrefs.Save();
        }
        else
        {
            ProceedToGame();
        }
    }

    private void ShowPopup()
    {
        if (popupPanel != null)
            popupPanel.SetActive(true);
    }

    private void CloseAndProceed()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
        ProceedToGame();
    }

    private void OpenGuideAndClose()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
        FindObjectOfType<SceneLoader>()?.LoadGuide();
    }

    private void ProceedToGame()
    {
        FindObjectOfType<SceneLoader>()?.LoadTitleScreen();
    }

    // FOR TESTING - call this to reset first-time flag
    public void ResetFirstTime()
    {
        PlayerPrefs.DeleteKey(FIRST_TIME_KEY);
        Debug.Log("First time flag reset");
    }
}