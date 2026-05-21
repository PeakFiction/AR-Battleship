using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadMainMenuScreen()        { SceneManager.LoadScene("0MainMenuScreen"); }
    public void LoadSplashScreen()          { SceneManager.LoadScene("0SplashScreen"); }
    public void LoadTitleScreen()           { SceneManager.LoadScene("1TitleScreen"); }
    public void LoadLobbyScreen()           { SceneManager.LoadScene("2LobbyScreen"); }
    public void LoadLobbySetup()            { SceneManager.LoadScene("3LobbySetup"); }
    public void LoadJoinLobby()             { SceneManager.LoadScene("4JoinLobby"); }
    public void LoadGameplay()              { SceneManager.LoadScene("5Gameplay"); }
    public void LoadMultiplayerGameplay()   { SceneManager.LoadScene("6MultiplayerGameplay"); }
    public void LoadDifficultySelect()      { SceneManager.LoadScene("7DifficultySelect"); }
    public void LoadGuide()                 { SceneManager.LoadScene("8Guide"); }
    public void LoadCredits()               { SceneManager.LoadScene("9Credits"); }

    public void LoadGuidePage2()               { SceneManager.LoadScene("8GuidePage2"); }

    public void LoadGuidePage3()               { SceneManager.LoadScene("8GuidePage3"); }
    public void LoadGuidePage4()               { SceneManager.LoadScene("8GuidePage4"); }
    public void LoadGuidePage5()               { SceneManager.LoadScene("8GuidePage5"); }

    public void LoadGuidePage6()               { SceneManager.LoadScene("8GuidePage6"); }
    public void LoadGuidePage7()               { SceneManager.LoadScene("8GuidePage7"); }
    public void LoadGuidePage8()               { SceneManager.LoadScene("8GuidePage8"); }
    public void LoadGuidePage9()               { SceneManager.LoadScene("8GuidePage9"); }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}