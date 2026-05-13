using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CreateLobbyUI : MonoBehaviour
{
	[SerializeField] private TMP_Text codeText;
	[SerializeField] private TMP_Text statusText;
	[SerializeField] private TMP_Text playerCountText;

	[SerializeField] private Button startGameButton;
	[SerializeField] private Button refreshCodeButton;
	[SerializeField] private Button backButton;

	private async void Start()
	{
		RegisterEvents();

		codeText.text = "Code: Creating...";
		statusText.text = "";
		playerCountText.text = "Player Count: 0 / 2";

		SetStartGameButton(false);

		if (RelayManager.Instance == null)
		{
			statusText.text = "RelayManager was not found. Start from the lobby menu scene.";
			return;
		}

		if (RelayManager.Instance.IsClient && !RelayManager.Instance.IsHost)
		{
			ShowClientWaitingUI();
			return;
		}

		ShowHostLobbyUI();

		if (string.IsNullOrEmpty(RelayManager.Instance.JoinCode))
		{
			await RelayManager.Instance.CreateRelayAsync();
		}
		else
		{
			HandleJoinCodeChanged(RelayManager.Instance.JoinCode);
		}
	}

	private void ShowHostLobbyUI()
	{
		if (codeText != null)
		{
			codeText.gameObject.SetActive(true);
		}

		if (playerCountText != null)
		{
			playerCountText.gameObject.SetActive(true);
		}

		if (refreshCodeButton != null)
		{
			refreshCodeButton.gameObject.SetActive(true);
		}

		if (startGameButton != null)
		{
			startGameButton.gameObject.SetActive(true);
			startGameButton.interactable = false;
		}

		if (statusText != null)
		{
			statusText.text = "Creating lobby...";
		}
	}

	private void ShowClientWaitingUI()
	{
		if (codeText != null)
		{
			codeText.gameObject.SetActive(false);
		}

		if (playerCountText != null)
		{
			playerCountText.gameObject.SetActive(false);
		}

		if (refreshCodeButton != null)
		{
			refreshCodeButton.gameObject.SetActive(false);
		}

		if (startGameButton != null)
		{
			startGameButton.gameObject.SetActive(false);
		}

		if (statusText != null)
		{
			statusText.text = "Joined lobby. Waiting for host to start the game.";
		}

		if (backButton != null)
		{
			backButton.interactable = true;
		}
	}

	private void OnDestroy()
	{
		UnregisterEvents();
	}

	public void OnStartGamePressed()
	{
		if (RelayManager.Instance == null || !RelayManager.Instance.IsHost)
		{
			return;
		}

		RelayManager.Instance.StartGameplayAsHost();
	}

	public async void OnRefreshCodePressed()
	{
		if (RelayManager.Instance == null || !RelayManager.Instance.IsHost)
		{
			return;
		}

		SetLobbyActionButtonsInteractable(false);

		codeText.text = "Code: Refreshing...";
		await RelayManager.Instance.RefreshRelayCodeAsync();

		SetLobbyActionButtonsInteractable(true);
	}

	public void OnBackPressed()
	{
		Debug.Log("Back button pressed");

		if (RelayManager.Instance != null)
		{
			RelayManager.Instance.ShutdownRelaySession();
			RelayManager.Instance.LoadMultiplayerMenuScene();
			return;
		}

		Debug.LogWarning("RelayManager not found. Loading lobby screen directly.");
		SceneManager.LoadScene("2LobbyScreen");
	}

	private void RegisterEvents()
	{
		if (RelayManager.Instance == null)
		{
			return;
		}

		RelayManager.Instance.OnStatusChanged += HandleStatusChanged;
		RelayManager.Instance.OnJoinCodeChanged += HandleJoinCodeChanged;
		RelayManager.Instance.OnClientCountChanged += HandleClientCountChanged;
		RelayManager.Instance.OnCanStartGameChanged += SetStartGameButton;
	}

	private void UnregisterEvents()
	{
		if (RelayManager.Instance == null)
		{
			return;
		}

		RelayManager.Instance.OnStatusChanged -= HandleStatusChanged;
		RelayManager.Instance.OnJoinCodeChanged -= HandleJoinCodeChanged;
		RelayManager.Instance.OnClientCountChanged -= HandleClientCountChanged;
		RelayManager.Instance.OnCanStartGameChanged -= SetStartGameButton;
	}

	private void HandleStatusChanged(string message)
	{
		if (statusText != null)
		{
			statusText.text = message;
		}
	}

	private void HandleJoinCodeChanged(string code)
	{
		if (codeText == null)
		{
			return;
		}

		if (string.IsNullOrEmpty(code))
		{
			codeText.text = "Code:";
			return;
		}

		codeText.text = $"Code: {code}";
	}

	private void HandleClientCountChanged(int connectedCount, int expectedCount)
	{
		if (playerCountText != null)
		{
			playerCountText.text = $"Player Count: {connectedCount} / {expectedCount}";
		}
	}

	private void SetStartGameButton(bool canStart)
	{
		if (startGameButton != null)
		{
			startGameButton.interactable = canStart;
		}
	}

	private void SetLobbyActionButtonsInteractable(bool interactable)
	{
		if (refreshCodeButton != null)
		{
			refreshCodeButton.interactable = interactable;
		}

		if (backButton != null)
		{
			backButton.interactable = true;
		}

		SetStartGameButton(false);
	}
}