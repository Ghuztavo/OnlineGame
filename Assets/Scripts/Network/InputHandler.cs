using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

// Collects raw Unity input each frame and packages it into a Network Input Data struct

public class InputHandler : MonoBehaviour, INetworkRunnerCallbacks //INetworkInput
{
    // ── Accumulated state between polls ────────────────────────────────────
    private Vector2  _moveDirection;
    private float    _cameraYaw;
    private float    _cameraPitch;
    private NetworkButtons _buttons;

    // Tracks raw accumulated look delta so we can pass absolute angles
    //private float _yRotation;
    //private float _xRotation;

    [Header("Mouse Sensitivity")]
    [SerializeField] private float sensX = 300f;
    [SerializeField] private float sensY = 300f;

    private void Update()
    {
        // ── Movement axes ────────────────────────────────────────────────
        _moveDirection = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        // ── Look accumulation ────────────────────────────────────────────
        // Read directly from PlayerCam's authoritative static accumulators
        _cameraYaw   = PlayerCam.YRotation;
        _cameraPitch = PlayerCam.XRotation;

        // ── Button presses ────────────────────────────────────────────────
        _buttons.Set(NetworkInputData.BUTTON_FIRE,   Input.GetMouseButton(0));
        _buttons.Set(NetworkInputData.BUTTON_JUMP,   Input.GetKey(KeyCode.Space));
        _buttons.Set(NetworkInputData.BUTTON_SPRINT, Input.GetKey(KeyCode.LeftShift));
        _buttons.Set(NetworkInputData.BUTTON_CROUCH, Input.GetKey(KeyCode.LeftControl));
    }

    // Called by Fusion just before each simulation tick.
    // Writes the accumulated frame state into the Network Input Data struct.
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData
        {
            MoveDirection = _moveDirection,
            CameraYaw     = _cameraYaw,
            CameraPitch   = _cameraPitch,
            Buttons       = _buttons,
        };

        input.Set(data);
    }

    // ── Unused INetworkRunnerCallbacks(required by interface) ─────────────
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ReadOnlySpan<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }

}
