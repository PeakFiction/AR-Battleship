using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RelayManager : MonoBehaviour
{
	public static RelayManager Instance { get; private set; }

	[Header("Relay Settings")]
	[SerializeField] private int maxConnections = 1;
	[SerializeField] private string connectionType = "dtls";

	[Header("Scene Names")]
	[SerializeField] private string multiplayerMenuSceneName = "2LobbyScreen";
	[SerializeField] private string createLobbySceneName = "3LobbySetup";
	[SerializeField] private string joinLobbySceneName = "4JoinLobby";
	[SerializeField] private string gameplaySceneName = "6MultiplayerGameplay";

	public string JoinCode { get; private set; }
	public bool IsHost { get; private set; }
	public bool IsClient { get; private set; }

	public event Action<string> OnStatusChanged;
	public event Action<string> OnJoinCodeChanged;
	public event Action<int, int> OnClientCountChanged;
	public event Action<bool> OnCanStartGameChanged;

	private bool servicesInitialized;
	private bool callbacksRegistered;

	private int ExpectedPlayerCount => maxConnections + 1;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	private void OnDestroy()
	{
		UnregisterNetworkCallbacks();

		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void LoadCreateLobbyScene()
	{
		SceneManager.LoadScene(createLobbySceneName);
	}

	public void LoadJoinLobbyScene()
	{
		SceneManager.LoadScene(joinLobbySceneName);
	}

	public void LoadMultiplayerMenuScene()
	{
		SceneManager.LoadScene(multiplayerMenuSceneName);
	}

	private async Task EnsureUnityServicesInitializedAsync()
	{
		if (servicesInitialized)
		{
			return;
		}

		await UnityServices.InitializeAsync();

		if (!AuthenticationService.Instance.IsSignedIn)
		{
			await AuthenticationService.Instance.SignInAnonymouslyAsync();
		}

		servicesInitialized = true;
	}

	public async Task<bool> CreateRelayAsync()
	{
		try
		{
			await PrepareForNewSessionAsync();

			OnStatusChanged?.Invoke("Creating lobby...");

			await EnsureUnityServicesInitializedAsync();

			Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

			UnityTransport transport = GetUnityTransport();

			transport.SetRelayServerData(
				AllocationUtils.ToRelayServerData(allocation, connectionType));

			JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

			bool started = NetworkManager.Singleton.StartHost();

			if (!started)
			{
				OnStatusChanged?.Invoke("Failed to start host.");
				return false;
			}

			IsHost = true;
			IsClient = false;

			RegisterNetworkCallbacks();
			UpdateClientCount();

			OnJoinCodeChanged?.Invoke(JoinCode);
			OnStatusChanged?.Invoke("Lobby created. Share this code with the other player.");

			return true;
		}
		catch (Exception exception)
		{
			OnStatusChanged?.Invoke($"Failed to create lobby: {exception.Message}");
			Debug.LogException(exception);
			return false;
		}
	}

	public async Task<bool> JoinRelayAsync(string code)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(code))
			{
				OnStatusChanged?.Invoke("Please enter a join code.");
				return false;
			}

			await PrepareForNewSessionAsync();

			OnStatusChanged?.Invoke("Joining lobby...");

			await EnsureUnityServicesInitializedAsync();

			JoinAllocation joinAllocation =
				await RelayService.Instance.JoinAllocationAsync(code.Trim().ToUpper());

			UnityTransport transport = GetUnityTransport();

			transport.SetRelayServerData(
				AllocationUtils.ToRelayServerData(joinAllocation, connectionType));

			bool started = NetworkManager.Singleton.StartClient();

			if (!started)
			{
				OnStatusChanged?.Invoke("Failed to start client.");
				return false;
			}

			IsHost = false;
			IsClient = true;
			JoinCode = code.Trim().ToUpper();

			RegisterNetworkCallbacks();

			OnJoinCodeChanged?.Invoke(JoinCode);
			OnStatusChanged?.Invoke("Joined lobby. Waiting for host to start the game.");

			return true;
		}
		catch (Exception exception)
		{
			OnStatusChanged?.Invoke($"Failed to join lobby: {exception.Message}");
			Debug.LogException(exception);
			return false;
		}
	}

	public async Task<bool> RefreshRelayCodeAsync()
	{
		OnStatusChanged?.Invoke("Refreshing lobby code...");

		ShutdownRelaySession();

		await Task.Delay(250);

		return await CreateRelayAsync();
	}

	public void StartGameplayAsHost()
	{
		if (!IsHost || NetworkManager.Singleton == null)
		{
			OnStatusChanged?.Invoke("Only the host can start the game.");
			return;
		}

		if (!NetworkManager.Singleton.IsListening)
		{
			OnStatusChanged?.Invoke("Network session is not active.");
			return;
		}

		int connectedCount = NetworkManager.Singleton.ConnectedClientsIds.Count;

		if (connectedCount < ExpectedPlayerCount)
		{
			OnStatusChanged?.Invoke("Waiting for another player to join.");
			return;
		}

		OnStatusChanged?.Invoke("Starting game...");

		NetworkManager.Singleton.SceneManager.LoadScene(
			gameplaySceneName,
			LoadSceneMode.Single);
	}

	public void ShutdownRelaySession()
	{
		UnregisterNetworkCallbacks();

		if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
		{
			NetworkManager.Singleton.Shutdown();
		}

		JoinCode = null;
		IsHost = false;
		IsClient = false;

		OnJoinCodeChanged?.Invoke("");
		OnClientCountChanged?.Invoke(0, ExpectedPlayerCount);
		OnCanStartGameChanged?.Invoke(false);
		OnStatusChanged?.Invoke("Relay session closed.");
	}

	private async Task PrepareForNewSessionAsync()
	{
		if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
		{
			ShutdownRelaySession();
			await Task.Delay(250);
		}
	}

	private UnityTransport GetUnityTransport()
	{
		if (NetworkManager.Singleton == null)
		{
			throw new InvalidOperationException("NetworkManager.Singleton was not found.");
		}

		UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

		if (transport == null)
		{
			throw new InvalidOperationException("UnityTransport was not found on the NetworkManager object.");
		}

		return transport;
	}

	private void RegisterNetworkCallbacks()
	{
		if (NetworkManager.Singleton == null || callbacksRegistered)
		{
			return;
		}

		NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
		NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;

		callbacksRegistered = true;
	}

	private void UnregisterNetworkCallbacks()
	{
		if (NetworkManager.Singleton == null || !callbacksRegistered)
		{
			return;
		}

		NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
		NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;

		callbacksRegistered = false;
	}

	private void HandleClientConnected(ulong clientId)
	{
		UpdateClientCount();

		if (IsHost)
		{
			OnStatusChanged?.Invoke("A player connected.");
		}
	}

	private void HandleClientDisconnected(ulong clientId)
	{
		UpdateClientCount();

		if (IsHost)
		{
			OnStatusChanged?.Invoke("A player disconnected.");
		}
		else
		{
			OnStatusChanged?.Invoke("Disconnected from host.");
		}
	}

	private void UpdateClientCount()
	{
		int connectedCount = 0;

		if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
		{
			connectedCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
		}

		bool canStartGame = IsHost && connectedCount >= ExpectedPlayerCount;

		OnClientCountChanged?.Invoke(connectedCount, ExpectedPlayerCount);
		OnCanStartGameChanged?.Invoke(canStartGame);
	}
}