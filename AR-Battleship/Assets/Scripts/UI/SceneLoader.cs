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

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}