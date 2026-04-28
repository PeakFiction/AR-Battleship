using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_InputField joinInput;
    [SerializeField] private TextMeshProUGUI codeText;

    [Header("Scenes")]
    [SerializeField] private string backSceneName = "2LobbyScreen";
    [SerializeField] private string multiplayerSceneName = "5Gameplay";
    // If you want players to go to a multiplayer lobby/setup scene first,
    // change this to "3LobbySetup" instead.

    private bool servicesReady = false;
    private bool sceneTransitionStarted = false;

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (NetworkManager.Singleton != null)
        {
            DontDestroyOnLoad(NetworkManager.Singleton.gameObject);
        }

        await InitializeUnityServices();
    }

    private void Start()
    {
        if (hostButton != null)
        {
            hostButton.onClick.RemoveAllListeners();
            hostButton.onClick.AddListener(CreateLobby);
        }

        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(JoinLobbyFromInput);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(BackToMenu);
        }

        RegisterNetworkCallbacks();

        if (codeText != null && string.IsNullOrWhiteSpace(codeText.text))
        {
            codeText.text = "Code:";
        }
    }

    private async System.Threading.Tasks.Task InitializeUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            servicesReady = true;
            Debug.Log("Unity Services ready.");
        }
        catch (Exception e)
        {
            servicesReady = false;
            Debug.LogError("Failed to initialize Unity Services.");
            Debug.LogException(e);
        }
    }

    private void RegisterNetworkCallbacks()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton not found.");
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    public async void CreateLobby()
    {
        if (!servicesReady)
        {
            Debug.LogWarning("Services are not ready yet.");
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is missing.");
            return;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("A network session is already running.");
            return;
        }

        try
        {
            SetButtonsInteractable(false);

            // For Battleship 1v1, you only need one joining player.
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            if (codeText != null)
            {
                codeText.text = "Code: " + joinCode;
            }

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

            bool started = NetworkManager.Singleton.StartHost();

            if (!started)
            {
                Debug.LogError("Failed to start host.");
                SetButtonsInteractable(true);
                return;
            }

            Debug.Log("Host started. Waiting for another player to join...");
        }
        catch (Exception e)
        {
            Debug.LogError("CreateLobby failed.");
            Debug.LogException(e);
            SetButtonsInteractable(true);
        }
    }

    public void JoinLobbyFromInput()
    {
        string joinCode = joinInput != null ? joinInput.text.Trim() : "";
        JoinLobby(joinCode);
    }

    public async void JoinLobby(string joinCode)
    {
        if (!servicesReady)
        {
            Debug.LogWarning("Services are not ready yet.");
            return;
        }

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogWarning("Join code is empty.");
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is missing.");
            return;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("A network session is already running.");
            return;
        }

        try
        {
            SetButtonsInteractable(false);

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));

            bool started = NetworkManager.Singleton.StartClient();

            if (!started)
            {
                Debug.LogError("Failed to start client.");
                SetButtonsInteractable(true);
                return;
            }

            Debug.Log("Client started. Waiting for host to move both players...");
        }
        catch (Exception e)
        {
            Debug.LogError("JoinLobby failed.");
            Debug.LogException(e);
            SetButtonsInteractable(true);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");

        if (NetworkManager.Singleton == null)
        {
            return;
        }

        // Only the host/server should trigger scene changes.
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        // Prevent double scene loads.
        if (sceneTransitionStarted)
        {
            return;
        }

        // Host + 1 client = 2 total players for Battleship.
        if (NetworkManager.Singleton.ConnectedClientsIds.Count == 2)
        {
            sceneTransitionStarted = true;
            Debug.Log("Two players connected. Loading multiplayer scene...");

            NetworkManager.Singleton.SceneManager.LoadScene(
                multiplayerSceneName,
                LoadSceneMode.Single
            );
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client disconnected: {clientId}");

        sceneTransitionStarted = false;
        SetButtonsInteractable(true);

        if (codeText != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            codeText.text = "Code: waiting for player...";
        }
    }

    public void BackToMenu()
    {
        // If a network session is active, stop it first.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        sceneTransitionStarted = false;
        SetButtonsInteractable(true);

        if (joinInput != null)
        {
            joinInput.text = "";
        }

        if (codeText != null)
        {
            codeText.text = "Code:";
        }

        SceneManager.LoadScene(backSceneName);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (hostButton != null) hostButton.interactable = interactable;
        if (joinButton != null) joinButton.interactable = interactable;
    }
}