using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef _playerPrefab;
    [SerializeField] private Material         _skyboxMaterial;
    [SerializeField] private Light            _sunLight;

    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new();

    private bool _fireLatch;
    private bool _reloadLatch;

    // ── Register / unregister with the persistent NetworkManager ─────────────

    private void OnEnable()
    {
        NetworkManager.Instance?.AddCallbacks(this);

        // If we're late registering (scene just loaded), catch up on
        // any players that already joined before we could listen
        var runner = NetworkManager.Instance?.Runner;
        if (runner != null && runner.IsServer)
        {
            foreach (var player in runner.ActivePlayers)
            {
                if (!_spawnedCharacters.ContainsKey(player))
                {
                    Vector3 spawnPos = new Vector3(
                        (player.RawEncoded % runner.Config.Simulation.PlayerCount) * 3, 1, 0);
                    NetworkObject obj = runner.Spawn(_playerPrefab, spawnPos, Quaternion.identity, player);
                    _spawnedCharacters.Add(player, obj);
                    Debug.Log($"[BasicSpawner] Late-spawned player {player.RawEncoded}");
                }
            }
        }
    }

    private void OnDisable()
    {
        NetworkManager.Instance?.RemoveCallbacks(this);
    }

    // ── Input latching (still lives here since this is the game scene) ────────

    private void Update()
    {
        var mouse    = Mouse.current;
        var keyboard = Keyboard.current;

        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            _fireLatch = true;

        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            _reloadLatch = true;
    }

    // ── INetworkRunnerCallbacks ───────────────────────────────────────────────

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

        var data     = new NetworkInputData();
        var keyboard = Keyboard.current;
        var mouse    = Mouse.current;

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
        PlayerMovement.Local.JumpPending = false;

        bool fireHeld = mouse != null && mouse.leftButton.isPressed;
        data.Buttons.Set(InputButtons.Fire, fireHeld || _fireLatch);
        _fireLatch = false;

        data.Buttons.Set(InputButtons.Reload, _reloadLatch);
        _reloadLatch = false;

        input.Set(data);
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (_skyboxMaterial != null) RenderSettings.skybox = _skyboxMaterial;
        if (_sunLight != null)       RenderSettings.sun    = _sunLight;
        DynamicGI.UpdateEnvironment();
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason reason) { Debug.Log($"[BasicSpawner] Shutdown: {reason}"); }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}