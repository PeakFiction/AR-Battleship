using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the basic multiplayer menu buttons for creating a lobby, joining a lobby, or returning to the title screen.
/// </summary>
public class MultiplayerMenuUI : MonoBehaviour
{
	[SerializeField] private string createLobbySceneName = "3LobbySetup";
	[SerializeField] private string joinLobbySceneName = "4JoinLobby";
	[SerializeField] private string mainMenuSceneName = "1TitleScreen";

	/// <summary>
	/// Loads the scene where the host creates a relay lobby.
	/// </summary>
	public void OnCreateLobbyPressed()
	{
		SceneManager.LoadScene(createLobbySceneName);
	}

	/// <summary>
	/// Loads the scene where a client enters a relay join code.
	/// </summary>
	public void OnJoinLobbyPressed()
	{
		SceneManager.LoadScene(joinLobbySceneName);
	}

	/// <summary>
	/// Returns from the multiplayer menu to the title screen.
	/// </summary>
	public void OnBackPressed()
	{
		if (RelayManager.Instance != null)
		{
			RelayManager.Instance.ShutdownRelaySession();
		}

		SceneManager.LoadScene(mainMenuSceneName);
	}
}
