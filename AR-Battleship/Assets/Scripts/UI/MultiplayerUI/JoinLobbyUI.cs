using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class JoinLobbyUI : MonoBehaviour
{
	[SerializeField] private TMP_InputField joinCodeInput;
	[SerializeField] private TMP_Text statusText;

	[SerializeField] private Button joinButton;
	[SerializeField] private Button backButton;

	private void OnEnable()
	{
		if (RelayManager.Instance != null)
		{
			RelayManager.Instance.OnStatusChanged += HandleStatusChanged;
		}
	}

	private void OnDisable()
	{
		if (RelayManager.Instance != null)
		{
			RelayManager.Instance.OnStatusChanged -= HandleStatusChanged;
		}
	}

	public async void OnJoinPressed()
	{
		if (RelayManager.Instance == null)
		{
			SetStatusText("RelayManager was not found. Start from the lobby menu scene.");
			return;
		}

		if (joinCodeInput == null || string.IsNullOrWhiteSpace(joinCodeInput.text))
		{
			SetStatusText("Please enter a lobby code.");
			return;
		}

		SetJoinButtonInteractable(false);

		string code = joinCodeInput.text.Trim().ToUpper();

		SetStatusText("Joining lobby...");

		bool joined = await RelayManager.Instance.JoinRelayAsync(code);

		if (!joined)
		{
			SetStatusText("Failed to join lobby. Check the code and try again.");
			SetJoinButtonInteractable(true);
			return;
		}

		SetStatusText("Joined lobby. Waiting for host to start the game.");
	}

	public void OnBackPressed()
	{
		Debug.Log("Join lobby back button pressed");

		if (RelayManager.Instance != null)
		{
			RelayManager.Instance.ShutdownRelaySession();
			RelayManager.Instance.LoadMultiplayerMenuScene();
			return;
		}

		SceneManager.LoadScene("2LobbyScreen");
	}

	private void HandleStatusChanged(string message)
	{
		SetStatusText(message);
	}

	private void SetStatusText(string message)
	{
		if (statusText != null)
		{
			statusText.text = message;
		}
	}

	private void SetJoinButtonInteractable(bool interactable)
	{
		if (joinButton != null)
		{
			joinButton.interactable = interactable;
		}

		if (backButton != null)
		{
			backButton.interactable = true;
		}
	}
}