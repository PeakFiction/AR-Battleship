using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerMenuUI : MonoBehaviour
{
	[SerializeField] private string createLobbySceneName = "3LobbySetup";
	[SerializeField] private string joinLobbySceneName = "4JoinLobby";
	[SerializeField] private string mainMenuSceneName = "1TitleScreen";

	public void OnCreateLobbyPressed()
	{
		SceneManager.LoadScene(createLobbySceneName);
	}

	public void OnJoinLobbyPressed()
	{
		SceneManager.LoadScene(joinLobbySceneName);
	}

	public void OnBackPressed()
	{
		if (RelayManager.Instance != null)
		{
			RelayManager.Instance.ShutdownRelaySession();
		}

		SceneManager.LoadScene(mainMenuSceneName);
	}
}