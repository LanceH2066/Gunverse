using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private TMP_Text  _roomCodeText;
    [SerializeField] private TMP_Text  _playerCountText;
    [SerializeField] private Button    _readyButton;
    [SerializeField] private Button    _startButton; // host only

    private void OnEnable()
    {
        NetworkManager.Instance?.AddCallbacks(this);
    }

    private void OnDisable()
    {
        NetworkManager.Instance?.RemoveCallbacks(this);
    }

    private void Start()
    {
        // Show the room code so players can share it with friends
        if (_roomCodeText != null && NetworkManager.Instance != null)
            _roomCodeText.text = $"Room Code: {NetworkManager.Instance.SessionCode}";

        // Only the host sees the Start button
        bool isHost = NetworkManager.Instance?.Runner?.IsServer ?? false;
        if (_startButton != null)
            _startButton.gameObject.SetActive(isHost);

        _startButton?.onClick.AddListener(() => NetworkManager.Instance.LoadGameScene());
        _readyButton?.onClick.AddListener(OnReadyClicked);
    }

    private void OnReadyClicked()
    {
        // Ready system goes here later
        Debug.Log("[LobbyManager] Player clicked ready");
    }

    // Update the player count label whenever someone joins or leaves
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) => RefreshPlayerCount(runner);
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)   => RefreshPlayerCount(runner);

    private void RefreshPlayerCount(NetworkRunner runner)
    {
        if (_playerCountText != null)
            _playerCountText.text = $"Players: {runner.SessionInfo?.PlayerCount}";
    }

    // Required interface stubs
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason reason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}