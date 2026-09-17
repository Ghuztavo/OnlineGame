using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;


// Central entry point for photon fusion 2
public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // ── Inspector ──────────────────────────────────────────────────────────

    [Header("Session")]
    [Tooltip("Name of the game scene to load after session starts.")]
    [SerializeField] private string gameSceneName = "GameScene";

    // ── Runtime References ─────────────────────────────────────────────────

    private NetworkRunner  _runner;
    private InputHandler   _inputHandler;
    private PlayerSpawner  _spawner;

    public static NetworkManager Instance { get; private set; }

    // ── Singleton ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _spawner = GetComponent<PlayerSpawner>();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    // Starts the listen-server session, the owner fo the session is both server and client
    public async void StartHost(string sessionName, string nickname)
    {
        PlayerPrefs.SetString("Nickname", nickname);
        await LaunchRunner(GameMode.Host, sessionName);
    }

    // joins a session as a client, the server is hosted by another player
    public async void JoinSession(string sessionName, string nickname)
    {
        PlayerPrefs.SetString("Nickname", nickname);
        await LaunchRunner(GameMode.Client, sessionName);
    }

    // ── Internal Launch ────────────────────────────────────────────────────

    private async Task LaunchRunner(GameMode mode, string sessionName)
    {
        // Create the runner GameObject.
        var runnerGO = new GameObject("NetworkRunner");
        DontDestroyOnLoad(runnerGO);
        _runner = runnerGO.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        // Attach the InputHandler so Fusion can poll it
        _inputHandler = runnerGO.AddComponent<InputHandler>();

        // Register callbacks
        _runner.AddCallbacks(_inputHandler);
        _runner.AddCallbacks(this);
        _runner.AddCallbacks(_spawner);

        // Configure the scene to load.
        var scene = SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath(
            $"Assets/Scenes/{gameSceneName}.unity"
        ));

        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Single);

        // Start the session.
        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode        = mode,
            SessionName     = sessionName,
            Scene           = sceneInfo,
            SceneManager    = gameObject.AddComponent<NetworkSceneManagerDefault>(),
        });

        if (result.Ok)
        {
            Debug.Log($"[NetworkManager] Session '{sessionName}' started in {mode} mode.");
        }
        else
        {
            Debug.LogError($"[NetworkManager] Failed to start session: {result.ShutdownReason}");
        }
    }

    // ── INetworkRunnerCallbacks ─────────────────────────────────────────────

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // After the local player's object spawns, set the nickname via RPC.
        if (player == runner.LocalPlayer)
        {
            if (PlayerRegistry.TryGet(player, out NetworkPlayer np))
            {
                string nickname = PlayerPrefs.GetString("Nickname", "Player");
                np.RPC_SetNickname(nickname);
            }
        }
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[NetworkManager] Runner shut down: {shutdownReason}");
        // Return to lobby scene.
        SceneManager.LoadScene(0);
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"[NetworkManager] Disconnected: {reason}");
        SceneManager.LoadScene(0);
    }

    // ── Unused required callbacks ──────────────────────────────────────────
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ReadOnlySpan<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
}
