using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the client join-lobby screen, validates the relay code input, joins the relay session, and waits for the host to start the match.
/// </summary>
public class JoinLobbyUI : MonoBehaviour
{
	[SerializeField] private TMP_InputField joinCodeInput;
	[SerializeField] private TMP_Text statusText;

	[SerializeField] private Button joinButton;
	[SerializeField] private Button backButton;

	[Header("Enter Code UI")]
	[Tooltip("Root object for the ENTER CODE box/panel. This will be hidden after the player successfully joins a lobby.")]
	[SerializeField] private GameObject enterCodeBoxRoot;

	[Header("Battle Start Popup")]
	[Tooltip("Root object for the popup shown when the host starts the multiplayer match.")]
	[SerializeField] private GameObject battleStartingPopup;
	[Tooltip("Optional text inside the popup. Leave empty if your popup art already contains the text.")]
	[SerializeField] private TMP_Text battleStartingPopupText;
	[SerializeField] private string battleStartingMessage = "OPPONENT FOUND\nBATTLE STARTING...";
	[SerializeField] private string gameplaySceneName = "6MultiplayerGameplay";

	[Header("Client Scene Protection")]
	[Tooltip("Keeps the joining player on the Join Lobby screen if Netcode briefly syncs them into the host lobby scene.")]
	[SerializeField] private bool keepClientOnJoinLobbyScreen = true;
	[SerializeField] private string createLobbySceneName = "3LobbySetup";
	[SerializeField] private string joinLobbySceneName = "4JoinLobby";

	private bool isWaitingForHost;
	private bool isStartingGame;

	/// <summary>
	/// Initialises the join screen and registers for relay status and scene-loading callbacks.
	/// </summary>
	private void Start()
	{
		HideBattleStartingPopup();

		if (RelayManager.Instance != null && RelayManager.Instance.IsClient && !RelayManager.Instance.IsHost)
		{
			isWaitingForHost = true;
			SetJoinButtonInteractable(false);
			SetEnterCodeBoxVisible(false);
			SetStatusText("Joined lobby. Waiting for host to start the game.");

			if (keepClientOnJoinLobbyScreen)
			{
				JoinLobbyClientSceneGuard.Ensure(
					joinLobbySceneName,
					createLobbySceneName,
					gameplaySceneName);
			}
		}
	}

	/// <summary>
	/// Subscribes to relay status events when the join screen becomes active.
	/// </summary>
	private void OnEnable()
	{
		if (RelayManager.Instance != null)
		{
			RelayManager.Instance.OnStatusChanged += HandleStatusChanged;
		}

		SceneManager.sceneLoaded += HandleUnitySceneLoaded;
		RegisterNetworkSceneEvents();
	}

	/// <summary>
	/// Unsubscribes from relay status events when the join screen is hidden or destroyed.
	/// </summary>
	private void OnDisable()
	{
		if (RelayManager.Instance != null)
		{
			RelayManager.Instance.OnStatusChanged -= HandleStatusChanged;
		}

		SceneManager.sceneLoaded -= HandleUnitySceneLoaded;
		UnregisterNetworkSceneEvents();
	}

	/// <summary>
	/// Validates the entered join code, attempts to join the relay session, and switches to the waiting state on success.
	/// </summary>
	public async void OnJoinPressed()
	{
		if (isWaitingForHost || isStartingGame)
		{
			return;
		}

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
			SetEnterCodeBoxVisible(true);
			SetJoinButtonInteractable(true);
			return;
		}

		isWaitingForHost = true;
		SetEnterCodeBoxVisible(false);
		SetStatusText("Joined lobby. Waiting for host to start the game.");

		if (keepClientOnJoinLobbyScreen)
		{
			JoinLobbyClientSceneGuard.Ensure(
				joinLobbySceneName,
				createLobbySceneName,
				gameplaySceneName);
		}

		RegisterNetworkSceneEvents();
	}

	/// <summary>
	/// Cancels the join flow and returns to the multiplayer menu or fallback lobby scene.
	/// </summary>
	public void OnBackPressed()
	{
		Debug.Log("Join lobby back button pressed");

		if (isStartingGame)
		{
			return;
		}

		JoinLobbyClientSceneGuard.DestroyExisting();

		if (RelayManager.Instance != null)
		{
			RelayManager.Instance.ShutdownRelaySession();
			RelayManager.Instance.LoadMultiplayerMenuScene();
			return;
		}

		SceneManager.LoadScene("2LobbyScreen");
	}

	/// <summary>
	/// Registers callbacks used to detect when the networked gameplay scene has loaded.
	/// </summary>
	private void RegisterNetworkSceneEvents()
	{
		if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
		{
			NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleNetworkSceneEvent;
			NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleNetworkSceneEvent;
		}
	}

	/// <summary>
	/// Removes network and Unity scene callbacks to prevent duplicate scene-load handling.
	/// </summary>
	private void UnregisterNetworkSceneEvents()
	{
		if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
		{
			NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleNetworkSceneEvent;
		}
	}

	/// <summary>
	/// Responds to Netcode scene-load notifications and hides the waiting popup after gameplay loads.
	/// </summary>
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

		ShowBattleStartingPopup();
		SetEnterCodeBoxVisible(false);
		SetLobbyButtonsInteractable(false);
		isStartingGame = true;

		JoinLobbyClientSceneGuard.DestroyExisting();
	}

	/// <summary>
	/// Handles direct Unity scene loads as a fallback for the join lobby flow.
	/// </summary>
	private void HandleUnitySceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (!keepClientOnJoinLobbyScreen)
		{
			return;
		}

		if (RelayManager.Instance == null || !RelayManager.Instance.IsClient || RelayManager.Instance.IsHost)
		{
			return;
		}

		if (scene.name == createLobbySceneName)
		{
			// Netcode may sync the joining client into the host's lobby scene.
			// This client should stay on the Join Lobby waiting screen instead.
			SceneManager.LoadScene(joinLobbySceneName);
		}
	}

	/// <summary>
	/// Forwards relay status messages into the join-screen status label.
	/// </summary>
	private void HandleStatusChanged(string message)
	{
		SetStatusText(message);
	}

	/// <summary>
	/// Safely updates the join-screen status text if the label is assigned.
	/// </summary>
	private void SetStatusText(string message)
	{
		if (statusText != null)
		{
			statusText.text = message;
		}
	}

	/// <summary>
	/// Enables or disables the join button during async relay operations.
	/// </summary>
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

	/// <summary>
	/// Controls both join and back buttons while joining or loading scenes.
	/// </summary>
	private void SetLobbyButtonsInteractable(bool interactable)
	{
		if (joinButton != null)
		{
			joinButton.interactable = interactable;
		}

		if (backButton != null)
		{
			backButton.interactable = interactable;
		}
	}

	/// <summary>
	/// Shows or hides the enter-code panel after the player joins successfully.
	/// </summary>
	private void SetEnterCodeBoxVisible(bool visible)
	{
		if (enterCodeBoxRoot != null)
		{
			enterCodeBoxRoot.SetActive(visible);
			return;
		}

		if (joinCodeInput != null)
			joinCodeInput.gameObject.SetActive(visible);

		if (joinButton != null)
			joinButton.gameObject.SetActive(visible);
	}

	/// <summary>
	/// Shows the waiting popup while the client waits for host-controlled scene loading.
	/// </summary>
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

	/// <summary>
	/// Hides the waiting popup after gameplay starts or the flow is cancelled.
	/// </summary>
	private void HideBattleStartingPopup()
	{
		if (battleStartingPopup != null)
		{
			battleStartingPopup.SetActive(false);
		}
	}
}

/// <summary>
/// Keeps joined clients on the waiting screen until the host loads the shared gameplay scene.
/// </summary>
public class JoinLobbyClientSceneGuard : MonoBehaviour
{
	private static JoinLobbyClientSceneGuard instance;

	private string joinLobbySceneName;
	private string createLobbySceneName;
	private string gameplaySceneName;
	private bool redirecting;

	/// <summary>
	/// Creates a single persistent scene guard if one does not already exist.
	/// </summary>
	public static void Ensure(string joinLobbySceneName, string createLobbySceneName, string gameplaySceneName)
	{
		if (instance != null)
		{
			instance.Configure(joinLobbySceneName, createLobbySceneName, gameplaySceneName);
			return;
		}

		GameObject guardObject = new GameObject("JoinLobbyClientSceneGuard");
		instance = guardObject.AddComponent<JoinLobbyClientSceneGuard>();
		instance.Configure(joinLobbySceneName, createLobbySceneName, gameplaySceneName);
		DontDestroyOnLoad(guardObject);
	}

	/// <summary>
	/// Removes the persistent scene guard when it is no longer needed.
	/// </summary>
	public static void DestroyExisting()
	{
		if (instance == null)
		{
			return;
		}

		JoinLobbyClientSceneGuard guard = instance;
		instance = null;

		if (guard != null)
		{
			Destroy(guard.gameObject);
		}
	}

	/// <summary>
	/// Stores scene names used by the guard to decide which scene transitions are allowed.
	/// </summary>
	private void Configure(string joinLobbySceneName, string createLobbySceneName, string gameplaySceneName)
	{
		this.joinLobbySceneName = joinLobbySceneName;
		this.createLobbySceneName = createLobbySceneName;
		this.gameplaySceneName = gameplaySceneName;
	}

	/// <summary>
	/// Subscribes to relay status events when the join screen becomes active.
	/// </summary>
	private void OnEnable()
	{
		SceneManager.sceneLoaded += HandleSceneLoaded;
	}

	/// <summary>
	/// Unsubscribes from relay status events when the join screen is hidden or destroyed.
	/// </summary>
	private void OnDisable()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;
	}

	/// <summary>
	/// Redirects joined clients away from lobby screens until the gameplay scene is loaded by the host.
	/// </summary>
	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (RelayManager.Instance == null || !RelayManager.Instance.IsClient || RelayManager.Instance.IsHost)
		{
			DestroyExisting();
			return;
		}

		if (!string.IsNullOrEmpty(gameplaySceneName) && scene.name == gameplaySceneName)
		{
			DestroyExisting();
			return;
		}

		if (redirecting)
		{
			redirecting = false;
			return;
		}

		if (!string.IsNullOrEmpty(createLobbySceneName) && scene.name == createLobbySceneName)
		{
			redirecting = true;
			SceneManager.LoadScene(joinLobbySceneName);
		}
	}
}
