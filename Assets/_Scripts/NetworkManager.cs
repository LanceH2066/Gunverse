using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent singleton that owns the NetworkRunner and handles session
/// start/join. Lives in the MainMenu scene and survives scene transitions.
/// Other scripts (BasicSpawner, LobbyManager, etc.) register themselves
/// as callback listeners via AddCallbacks / RemoveCallbacks.
/// </summary>
public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static NetworkManager Instance { get; private set; }

    // ── Public state ─────────────────────────────────────────────────────────
    public NetworkRunner Runner      { get; private set; }
    public string        SessionCode { get; private set; }
    public bool          IsRunning   => Runner != null && Runner.IsRunning;

    // ── Scene build indices – set these to match your project ────────────────
    [Header("Scene Indices")]
    [SerializeField] private int _lobbySceneIndex = 1;
    [SerializeField] private int _gameSceneIndex  = 2;

    // ── Events the UI or other managers can subscribe to ────────────────────
    public event Action          OnSessionStarted;
    public event Action<string>  OnSessionFailed;   // passes an error message
    public event Action          OnSessionEnded;

    // ── External callback listeners (e.g. BasicSpawner, LobbyManager) ───────
    private readonly List<INetworkRunnerCallbacks> _externalListeners = new();

    // ─────────────────────────────────────────────────────────────────────────
    #region Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

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

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Start as host and create a new session with a generated code.</summary>
    public async void HostGame()
    {
        SessionCode = GenerateCode();
        Debug.Log($"[NetworkManager] Hosting session: {SessionCode}");
        await StartSession(GameMode.Host, SessionCode);
    }

    /// <summary>Join an existing session by its room code.</summary>
    public async void JoinGame(string code)
    {
        SessionCode = code.ToUpper().Trim();
        Debug.Log($"[NetworkManager] Joining session: {SessionCode}");
        await StartSession(GameMode.Client, SessionCode);
    }

    /// <summary>Tell the runner to load the game scene (host only).</summary>
    public void LoadGameScene()
    {
        if (Runner == null || !Runner.IsServer) return;
        Runner.LoadScene(SceneRef.FromIndex(_gameSceneIndex));
    }

    /// <summary>Shut down the runner and return to the main menu.</summary>
    public async void Disconnect()
    {
        if (Runner != null)
            await Runner.Shutdown();
    }

    /// <summary>
    /// Let scene-specific scripts (BasicSpawner, LobbyManager) register
    /// to receive Fusion callbacks without owning the runner.
    /// </summary>
    public void AddCallbacks(INetworkRunnerCallbacks listener)
    {
        if (!_externalListeners.Contains(listener))
        {
            _externalListeners.Add(listener);
            Runner?.AddCallbacks(listener);
        }
    }

    public void RemoveCallbacks(INetworkRunnerCallbacks listener)
    {
        _externalListeners.Remove(listener);
        Runner?.RemoveCallbacks(listener);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Session startup
    // ─────────────────────────────────────────────────────────────────────────

    private async System.Threading.Tasks.Task StartSession(GameMode mode, string sessionName)
    {
        // Clean up any existing runner first
        if (Runner != null)
            await Runner.Shutdown();

        Runner               = gameObject.AddComponent<NetworkRunner>();
        Runner.ProvideInput  = true;
        

        // Register ourselves and any already-registered external listeners
        Runner.AddCallbacks(this);
        foreach (var cb in _externalListeners)
            Runner.AddCallbacks(cb);

        var result = await Runner.StartGame(new StartGameArgs
        {
            GameMode     = mode,
            SessionName  = sessionName,
            Scene        = SceneRef.FromIndex(_lobbySceneIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            PlayerCount  = 10,
        });

        if (result.Ok)
        {
            Debug.Log($"[NetworkManager] Session started OK ({mode})");
            OnSessionStarted?.Invoke();
        }
        else
        {
            string err = $"Failed to start session: {result.ShutdownReason}";
            Debug.LogWarning($"[NetworkManager] {err}");
            OnSessionFailed?.Invoke(err);
            Destroy(Runner);
            Runner = null;
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no 0/O/1/I
        var code = new System.Text.StringBuilder(6);
        for (int i = 0; i < 6; i++)
            code.Append(chars[UnityEngine.Random.Range(0, chars.Length)]);
        return code.ToString();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region INetworkRunnerCallbacks – forward everything to external listeners
    // ─────────────────────────────────────────────────────────────────────────
    // NetworkManager is the sole registered callback object on the runner.
    // It forwards each event to any scene-specific listeners so they don't
    // need to own the runner themselves.

    public void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        Debug.Log($"[NetworkManager] Shutdown: {reason}");
        foreach (var cb in _externalListeners) cb.OnShutdown(runner, reason);

        if (Runner != null) { Destroy(Runner); Runner = null; }
        OnSessionEnded?.Invoke();
        SceneManager.LoadScene(0); // back to main menu
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        foreach (var cb in _externalListeners) cb.OnPlayerJoined(runner, player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        foreach (var cb in _externalListeners) cb.OnPlayerLeft(runner, player);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        foreach (var cb in _externalListeners) cb.OnInput(runner, input);
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // Unload MainMenu once we're in lobby or game
        var mainMenu = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(0);
        if (mainMenu.isLoaded)
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(mainMenu);

        foreach (var cb in _externalListeners) cb.OnSceneLoadDone(runner);
    }
    public void OnSceneLoadStart(NetworkRunner runner)
    {
        foreach (var cb in _externalListeners) cb.OnSceneLoadStart(runner);
    }
    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DestroyDuplicates<AudioListener>();
        DestroyDuplicates<UnityEngine.EventSystems.EventSystem>();
    }

    private void DestroyDuplicates<T>() where T : Component
    {
        var all = FindObjectsByType<T>(FindObjectsSortMode.None);
        if (all.Length <= 1) return;

        // Destroy all but the last found — the newest scene's version
        for (int i = 0; i < all.Length - 1; i++)
            Destroy(all[i].gameObject);
    }    

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[NetworkManager] Connected to server");
        foreach (var cb in _externalListeners) cb.OnConnectedToServer(runner);
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"[NetworkManager] Disconnected: {reason}");
        foreach (var cb in _externalListeners) cb.OnDisconnectedFromServer(runner, reason);
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress addr, NetConnectFailedReason reason)
    {
        string err = $"Connect failed: {reason}";
        Debug.LogWarning($"[NetworkManager] {err}");
        OnSessionFailed?.Invoke(err);
        foreach (var cb in _externalListeners) cb.OnConnectFailed(runner, addr, reason);
    }

    public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput i)          { foreach (var cb in _externalListeners) cb.OnInputMissing(r, p, i); }
    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token) { foreach (var cb in _externalListeners) cb.OnConnectRequest(r, req, token); }
    public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr msg)     { foreach (var cb in _externalListeners) cb.OnUserSimulationMessage(r, msg); }
    public void OnSessionListUpdated(NetworkRunner r, List<SessionInfo> list)          { foreach (var cb in _externalListeners) cb.OnSessionListUpdated(r, list); }
    public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> d) { foreach (var cb in _externalListeners) cb.OnCustomAuthenticationResponse(r, d); }
    public void OnHostMigration(NetworkRunner r, HostMigrationToken token)             { foreach (var cb in _externalListeners) cb.OnHostMigration(r, token); }
    public void OnObjectEnterAOI(NetworkRunner r, NetworkObject o, PlayerRef p)        { foreach (var cb in _externalListeners) cb.OnObjectEnterAOI(r, o, p); }
    public void OnObjectExitAOI(NetworkRunner r, NetworkObject o, PlayerRef p)         { foreach (var cb in _externalListeners) cb.OnObjectExitAOI(r, o, p); }
    public void OnReliableDataReceived(NetworkRunner r, PlayerRef p, ReliableKey k, ArraySegment<byte> d) { foreach (var cb in _externalListeners) cb.OnReliableDataReceived(r, p, k, d); }
    public void OnReliableDataProgress(NetworkRunner r, PlayerRef p, ReliableKey k, float progress)       { foreach (var cb in _externalListeners) cb.OnReliableDataProgress(r, p, k, progress); }

    #endregion
}