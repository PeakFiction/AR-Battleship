using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CreateLobbyUI : MonoBehaviour
{
	[SerializeField] private TMP_Text codeText;
	[SerializeField] private TMP_Text statusText;
	[SerializeField] private TMP_Text playerCountText;

	[SerializeField] private Button startGameButton;
	[SerializeField] private Button refreshCodeButton;
	[SerializeField] private Button backButton;

	[Header("Battle Start Popup")]
	[Tooltip("Root object for the popup shown when the host starts the multiplayer match.")]
	[SerializeField] private GameObject battleStartingPopup;
	[Tooltip("Optional text inside the popup. Leave empty if your popup art already contains the text.")]
	[SerializeField] private TMP_Text battleStartingPopupText;
	[SerializeField] private string battleStartingMessage = "OPPONENT FOUND\nBATTLE STARTING...";
	[SerializeField] private float battleStartingPopupDuration = 1.25f;
	[SerializeField] private bool hideLobbyButtonsWhileStarting = true;
	[SerializeField] private string gameplaySceneName = "6MultiplayerGameplay";

	private bool isStartingGame;

	private async void Start()
	{
		RegisterEvents();
		HideBattleStartingPopup();

		codeText.text = "Creating...";
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
			statusText.text = "AWAITING KEY...";
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
			statusText.text = "AWAITING SYNC";
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
		if (RelayManager.Instance == null || !RelayManager.Instance.IsHost || isStartingGame)
		{
			return;
		}

		StartCoroutine(StartGameplayAfterPopup());
	}

	private IEnumerator StartGameplayAfterPopup()
	{
		isStartingGame = true;

		ShowBattleStartingPopup();
		SetLobbyActionButtonsInteractable(false);

		if (statusText != null)
		{
			statusText.text = "OPPONENT FOUND\nBATTLE STARTING...";
		}

		yield return new WaitForSecondsRealtime(battleStartingPopupDuration);

		if (RelayManager.Instance != null && RelayManager.Instance.IsHost)
		{
			RelayManager.Instance.StartGameplayAsHost();
		}
	}

	public async void OnRefreshCodePressed()
	{
		if (RelayManager.Instance == null || !RelayManager.Instance.IsHost || isStartingGame)
		{
			return;
		}

		SetLobbyActionButtonsInteractable(false);

		codeText.text = "REFRESHING..";
		await RelayManager.Instance.RefreshRelayCodeAsync();

		SetLobbyActionButtonsInteractable(true);
	}

	public void OnBackPressed()
	{
		Debug.Log("Back button pressed");

		if (isStartingGame)
		{
			return;
		}

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
		if (RelayManager.Instance != null)
		{
			RelayManager.Instance.OnStatusChanged += HandleStatusChanged;
			RelayManager.Instance.OnJoinCodeChanged += HandleJoinCodeChanged;
			RelayManager.Instance.OnClientCountChanged += HandleClientCountChanged;
			RelayManager.Instance.OnCanStartGameChanged += SetStartGameButton;
		}

		if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
		{
			NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleNetworkSceneEvent;
		}
	}

	private void UnregisterEvents()
	{
		if (RelayManager.Instance != null)
		{
			RelayManager.Instance.OnStatusChanged -= HandleStatusChanged;
			RelayManager.Instance.OnJoinCodeChanged -= HandleJoinCodeChanged;
			RelayManager.Instance.OnClientCountChanged -= HandleClientCountChanged;
			RelayManager.Instance.OnCanStartGameChanged -= SetStartGameButton;
		}

		if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
		{
			NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleNetworkSceneEvent;
		}
	}

	private void HandleNetworkSceneEvent(SceneEvent sceneEvent)
	{
		if (sceneEvent.SceneEventType != SceneEventType.Load)
		{
			return;
		}

		if (!string.IsNullOrEmpty(gameplaySceneName) && sceneEvent.SceneName != gameplaySceneName)
		{
			return;
		}

		// Clients receive this when the host starts the network scene load.
		// The host already shows the popup before calling StartGameplayAsHost().
		ShowBattleStartingPopup();
		SetLobbyActionButtonsInteractable(false);
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
			codeText.text = "";
			return;
		}

		codeText.text = $"{code}";
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
			startGameButton.interactable = canStart && !isStartingGame;
		}
	}

	private void SetLobbyActionButtonsInteractable(bool interactable)
	{
		bool allowed = interactable && !isStartingGame;

		if (refreshCodeButton != null)
		{
			refreshCodeButton.interactable = allowed;
		}

		if (backButton != null)
		{
			backButton.interactable = allowed;
		}

		if (!allowed)
		{
			SetStartGameButton(false);
		}
	}

	private void ShowBattleStartingPopup()
	{
		if (battleStartingPopupText != null)
		{
			battleStartingPopupText.text = battleStartingMessage;
		}

		if (battleStartingPopup != null)
		{
			battleStartingPopup.SetActive(true);
		}
	}

	private void HideBattleStartingPopup()
	{
		if (battleStartingPopup != null)
		{
			battleStartingPopup.SetActive(false);
		}
	}
}
