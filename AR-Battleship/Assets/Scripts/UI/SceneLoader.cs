using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadTitleScreen() { SceneManager.LoadScene("1TitleScreen"); }
    public void LoadLobbyScreen() { SceneManager.LoadScene("2LobbyScreen"); }
    public void LoadLobbySetup() { SceneManager.LoadScene("3LobbySetup"); }
    public void LoadJoinLobby() { SceneManager.LoadScene("4JoinLobby"); }
    public void LoadGameplay() { SceneManager.LoadScene("5Gameplay"); }
    public void LoadMultiplayerGameplay()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("Network session is not active.");
            return;
        }

        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("Only the host/server should load multiplayer scenes.");
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene("6MultiplayerGameplay", LoadSceneMode.Single);
    }
}

    

