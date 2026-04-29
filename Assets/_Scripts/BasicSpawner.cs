using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef _playerPrefab;
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private NetworkRunner _runner;

    // Detect if this is a server build (headless) or a client
    private bool IsHeadless => SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;

    async void Start()
    {
        await StartGame(GameMode.Server);
    }

    async Task StartGame(GameMode mode)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();

        // Server doesn't provide input — only clients do
        _runner.ProvideInput = (mode != GameMode.Server);

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Only the server spawns player objects
        if (runner.IsServer)
        {
            Vector3 spawnPosition = new Vector3((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 3, 1, 0);
            NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
            _spawnedCharacters.Add(player, networkPlayerObject);
        }
    }

    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }

    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Only called on clients, not on the dedicated server
        var data = new NetworkInputData();
        if (Input.GetKey(KeyCode.W)) data.Direction += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) data.Direction += Vector3.back;
        if (Input.GetKey(KeyCode.A)) data.Direction += Vector3.left;
        if (Input.GetKey(KeyCode.D)) data.Direction += Vector3.right;
        input.Set(data);
    }

    private void OnGUI()
    {
        if (_runner == null)
        {
            if (!IsHeadless)
            {
                if (GUI.Button(new Rect(0, 0, 200, 40), "Join"))
                {
                    _ = StartGame(GameMode.Client);
                }
            }
        }
        else
        {
            // Debug info visible in both server and client builds
            int y = 0;
            GUI.Label(new Rect(10, y += 20, 400, 25), $"Mode: {_runner.GameMode}");
            GUI.Label(new Rect(10, y += 20, 400, 25), $"Is Server: {_runner.IsServer}");
            GUI.Label(new Rect(10, y += 20, 400, 25), $"Is Client: {_runner.IsClient}");
            GUI.Label(new Rect(10, y += 20, 400, 25), $"Session: {_runner.SessionInfo?.Name}");
            GUI.Label(new Rect(10, y += 20, 400, 25), $"Players connected: {_runner.SessionInfo?.PlayerCount}");
            GUI.Label(new Rect(10, y += 20, 400, 25), $"Spawned characters: {_spawnedCharacters.Count}");
        }
    }

    // --- Required stubs ---
    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) {  Debug.Log($"[Fusion] Shutdown: {shutdownReason}"); _runner = null; }
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { Debug.Log("[Fusion] Connected to server!"); }
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) {   Debug.Log($"[Fusion] Disconnected: {reason}"); }
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

}