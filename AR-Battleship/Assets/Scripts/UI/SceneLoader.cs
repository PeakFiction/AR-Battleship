using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Thin wrapper around SceneManager.LoadScene that exposes every project scene
/// as a discrete, Inspector-assignable public method.
///
/// Attach to any persistent GameObject and wire Button onClick events to the
/// required method instead of hardcoding scene names in multiple scripts.
/// </summary>
public class SceneLoader : MonoBehaviour
{

    /// <summary>Loads the main menu (scene "0MainMenuScreen").</summary>
    public void LoadMainMenuScreen()      { SceneManager.LoadScene("0MainMenuScreen"); }

    /// <summary>Loads the animated splash / intro screen (scene "0SplashScreen").</summary>
    public void LoadSplashScreen()        { SceneManager.LoadScene("0SplashScreen"); }


    /// <summary>Loads the title / home screen (scene "1TitleScreen").</summary>
    public void LoadTitleScreen()         { SceneManager.LoadScene("1TitleScreen"); }

    /// <summary>Loads the multiplayer lobby browser (scene "2LobbyScreen").</summary>
    public void LoadLobbyScreen()         { SceneManager.LoadScene("2LobbyScreen"); }

    /// <summary>Loads the lobby setup / host creation screen (scene "3LobbySetup").</summary>
    public void LoadLobbySetup()          { SceneManager.LoadScene("3LobbySetup"); }

    /// <summary>Loads the join-lobby screen for clients (scene "4JoinLobby").</summary>
    public void LoadJoinLobby()           { SceneManager.LoadScene("4JoinLobby"); }

    /// <summary>Loads the singleplayer AR gameplay scene (scene "5Gameplay").</summary>
    public void LoadGameplay()            { SceneManager.LoadScene("5Gameplay"); }

    /// <summary>Loads the multiplayer AR gameplay scene (scene "6MultiplayerGameplay").</summary>
    public void LoadMultiplayerGameplay() { SceneManager.LoadScene("6MultiplayerGameplay"); }

    /// <summary>Loads the difficulty selection screen (scene "7DifficultySelect").</summary>
    public void LoadDifficultySelect()    { SceneManager.LoadScene("7DifficultySelect"); }

    /// <summary>Loads the first page of the in-game guide (scene "8Guide").</summary>
    public void LoadGuide()               { SceneManager.LoadScene("8Guide"); }

    /// <summary>Loads guide page 2 (scene "8GuidePage2").</summary>
    public void LoadGuidePage2()          { SceneManager.LoadScene("8GuidePage2"); }

    /// <summary>Loads guide page 3 (scene "8GuidePage3").</summary>
    public void LoadGuidePage3()          { SceneManager.LoadScene("8GuidePage3"); }

    /// <summary>Loads guide page 4 (scene "8GuidePage4").</summary>
    public void LoadGuidePage4()          { SceneManager.LoadScene("8GuidePage4"); }

    /// <summary>Loads guide page 5 (scene "8GuidePage5").</summary>
    public void LoadGuidePage5()          { SceneManager.LoadScene("8GuidePage5"); }

    /// <summary>Loads guide page 6 (scene "8GuidePage6").</summary>
    public void LoadGuidePage6()          { SceneManager.LoadScene("8GuidePage6"); }

    /// <summary>Loads guide page 7 (scene "8GuidePage7").</summary>
    public void LoadGuidePage7()          { SceneManager.LoadScene("8GuidePage7"); }

    /// <summary>Loads guide page 8 (scene "8GuidePage8").</summary>
    public void LoadGuidePage8()          { SceneManager.LoadScene("8GuidePage8"); }

    /// <summary>Loads guide page 9 (scene "8GuidePage9").</summary>
    public void LoadGuidePage9()          { SceneManager.LoadScene("8GuidePage9"); }

    /// <summary>Loads the scrolling credits screen (scene "9Credits").</summary>
    public void LoadCredits()             { SceneManager.LoadScene("9Credits"); }

    /// <summary>
    /// Quits the application.
    /// In the Unity Editor the play mode is stopped instead of a real quit,
    /// which would have no effect inside the editor.
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        // Application.Quit() is a no-op inside the Editor; stop play mode instead.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
