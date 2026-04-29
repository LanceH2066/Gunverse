using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef _playerPrefab;
    [SerializeField] private Material _skyboxMaterial;
    [SerializeField] private Light _sunLight;

    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new();
    private NetworkRunner _runner;

    async void StartGame(GameMode mode)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            PlayerCount = 10
        });
    }

    private void OnGUI()
    {
        if (_runner == null)
        {
            if (GUI.Button(new Rect(0, 0, 200, 40), "Host"))
                StartGame(GameMode.Host);
            if (GUI.Button(new Rect(0, 40, 200, 40), "Join"))
                StartGame(GameMode.Client);
        }
        else
        {
            int y = 0;
            GUI.Label(new Rect(10, y += 20, 400, 25), $"Mode: {_runner.GameMode}");
            GUI.Label(new Rect(10, y += 20, 400, 25), $"Players: {_runner.SessionInfo?.PlayerCount}");
            GUI.Label(new Rect(10, y += 20, 400, 25), $"Spawned: {_spawnedCharacters.Count}");
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            Vector3 spawnPos = new Vector3(
                (player.RawEncoded % runner.Config.Simulation.PlayerCount) * 3, 1, 0);
            NetworkObject obj = runner.Spawn(_playerPrefab, spawnPos, Quaternion.identity, player);
            _spawnedCharacters.Add(player, obj);
            Debug.Log($"[BasicSpawner] Spawned player {player.RawEncoded}");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject obj))
        {
            runner.Despawn(obj);
            _spawnedCharacters.Remove(player);
            Debug.Log($"[BasicSpawner] Despawned player {player.RawEncoded}");
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (PlayerMovement.Local == null) return;

        var data = new NetworkInputData();

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            Vector2 dir = Vector2.zero;
            if (keyboard.wKey.isPressed) dir += Vector2.up;
            if (keyboard.sKey.isPressed) dir += Vector2.down;
            if (keyboard.aKey.isPressed) dir += Vector2.left;
            if (keyboard.dKey.isPressed) dir += Vector2.right;
            data.Direction = dir.normalized;

            data.Buttons.Set(InputButtons.Sprint, keyboard.leftShiftKey.isPressed);
        }

        data.Yaw   = PlayerMovement.Local.LocalYaw;
        data.Pitch = FPSCamera.Local != null ? FPSCamera.Local.LocalCameraX : 0f;

        data.Buttons.Set(InputButtons.Jump, PlayerMovement.Local.JumpPending);

        input.Set(data);

        PlayerMovement.Local.JumpPending = false;
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { Debug.Log($"[BasicSpawner] Shutdown: {shutdownReason}"); _runner = null; }
    public void OnConnectedToServer(NetworkRunner runner) { Debug.Log("[BasicSpawner] Connected"); }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { Debug.Log($"[BasicSpawner] Disconnected: {reason}"); }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { Debug.Log($"[BasicSpawner] Connect failed: {reason}"); }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (_skyboxMaterial != null)
            RenderSettings.skybox = _skyboxMaterial;

        if (_sunLight != null)
            RenderSettings.sun = _sunLight;

        DynamicGI.UpdateEnvironment();
    }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}