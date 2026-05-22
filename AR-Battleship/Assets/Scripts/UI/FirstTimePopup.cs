using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a "welcome / guide" popup exactly once on first launch, then
/// remembers the visit using PlayerPrefs so it is never shown again.
/// </summary>
public class FirstTimePopup : MonoBehaviour
{

    /// <summary>
    /// Root GameObject of the popup panel.  Hidden by default; shown
    /// programmatically on first launch.
    /// </summary>
    [SerializeField] private GameObject popupPanel;

    /// <summary>
    /// "Close" or "Skip" button inside the popup.
    /// Dismisses the popup and proceeds to the title screen.
    /// </summary>
    [SerializeField] private Button closeButton;

    /// <summary>
    /// "Guide" or "Tutorial" button inside the popup.
    /// Closes the popup and navigates to the guide scene via SceneLoader.
    /// </summary>
    [SerializeField] private Button guideButton;

    /// <summary>
    /// PlayerPrefs key used to record whether the first-time popup has been seen.
    /// An integer value of 1 means the popup has already been displayed.
    /// </summary>
    private const string FIRST_TIME_KEY = "HasSeenGuidePopup";

    /// <summary>
    /// Wires button listeners and ensures the popup starts hidden
    /// (it is shown only when OnStartButtonPressed is called for the first time).
    /// </summary>
    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseAndProceed);

        if (guideButton != null)
            guideButton.onClick.AddListener(OpenGuideAndClose);

        // Hide by default; visibility is controlled by OnStartButtonPressed.
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    /// <summary>
    /// Called by the main Start button's onClick event.
    /// Shows the popup on the first run; skips directly to the title screen
    /// on subsequent runs.
    /// </summary>
    public void OnStartButtonPressed()
    {
        if (!PlayerPrefs.HasKey(FIRST_TIME_KEY))
        {
            // First time: show the popup and record the visit.
            ShowPopup();
            PlayerPrefs.SetInt(FIRST_TIME_KEY, 1);
            PlayerPrefs.Save(); // Flush immediately so it persists even if the app crashes
        }
        else
        {
            // Subsequent runs: skip the popup entirely.
            ProceedToGame();
        }
    }

    /// <summary>Makes the popup panel visible.</summary>
    private void ShowPopup()
    {
        if (popupPanel != null)
            popupPanel.SetActive(true);
    }

    /// <summary>
    /// Hides the popup and navigates to the title screen.
    /// Wired to the "Close" button inside the popup.
    /// </summary>
    private void CloseAndProceed()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);

        ProceedToGame();
    }

    /// <summary>
    /// Hides the popup and loads the guide scene so new players can learn
    /// the game before their first match.
    /// Wired to the "Guide" button inside the popup.
    /// </summary>
    private void OpenGuideAndClose()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);

        // Delegates scene navigation to the centralised SceneLoader component.
        FindObjectOfType<SceneLoader>()?.LoadGuide();
    }

    /// <summary>
    /// Navigates to the title screen via the SceneLoader component.
    /// Used by both CloseAndProceed and the "already seen" path in
    /// OnStartButtonPressed.
    /// </summary>
    private void ProceedToGame()
    {
        FindObjectOfType<SceneLoader>()?.LoadTitleScreen();
    }

    /// <summary>
    /// Deletes the PlayerPrefs key so the popup will appear again on the
    /// next call to OnStartButtonPressed.
    /// FOR TESTING ONLY – call from the Unity Editor console or a debug menu.
    /// </summary>
    public void ResetFirstTime()
    {
        PlayerPrefs.DeleteKey(FIRST_TIME_KEY);
        Debug.Log("First time flag reset");
    }
}
