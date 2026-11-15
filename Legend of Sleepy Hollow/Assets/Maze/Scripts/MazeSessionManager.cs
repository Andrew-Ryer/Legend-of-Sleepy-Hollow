using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class MazeSessionManager : MonoBehaviour
{
    [SerializeField]
    NetworkManager _networkManager;

    [SerializeField]
    UnityTransport _transport;

    [SerializeField]
    SpawnManager _spawnManager;

    public static EventHandler<SessionChangedArgs> SessionChanged;

    public class SessionChangedArgs : EventArgs
    {
        public string status;
        public bool connected;
        public bool connecting;
    }

    public SessionChangedArgs State { get; private set; } = new() { status = "Disconnected" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetEvents() => SessionChanged = null;

    void OnEnable()
    {
        MazeSessionHud.SessionRequested += HandleSessionRequested;
        _networkManager.OnClientConnectedCallback += HandleClientConnected;
        _networkManager.OnClientStopped += HandleClientStopped;
        _networkManager.OnTransportFailure += HandleTransportFailure;
        _networkManager.ConnectionApprovalCallback += ApproveConnection;
    }

    void OnDisable()
    {
        MazeSessionHud.SessionRequested -= HandleSessionRequested;
        _networkManager.OnClientConnectedCallback -= HandleClientConnected;
        _networkManager.OnClientStopped -= HandleClientStopped;
        _networkManager.OnTransportFailure -= HandleTransportFailure;
        _networkManager.ConnectionApprovalCallback -= ApproveConnection;
    }

    void HandleSessionRequested(object sender, MazeSessionHud.SessionRequestedArgs e)
    {
        switch (e.action)
        {
            case MazeSessionHud.SessionAction.Host:
                StartHost();
                break;
            case MazeSessionHud.SessionAction.Client:
                StartClient();
                break;
            case MazeSessionHud.SessionAction.Disconnect:
                Disconnect();
                break;
        }
    }

    public void StartHost()
    {
        if (_networkManager.IsListening || State.connecting)
            return;

        ConfigureNetworkManager();

        SetState("Starting host...", false, true);
        try
        {
            if (!_networkManager.StartHost())
                SetState("Could not start host");
        }
        catch (Exception e)
        {
            SetState(e.Message);
        }
    }

    public void StartClient()
    {
        if (_networkManager.IsListening || State.connecting)
            return;

        ConfigureNetworkManager();

        SetState("Connecting...", false, true);
        try
        {
            if (!_networkManager.StartClient())
                SetState("Could not start client");
        }
        catch (Exception e)
        {
            SetState(e.Message);
        }
    }

    public void Disconnect()
    {
        _networkManager.Shutdown();
        SetState("Disconnected");
    }

    void ApproveConnection(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response
    )
    {
        Vector3 spawnPosition = _spawnManager.GetSpawnPosition(request.ClientNetworkId);

        response.Approved = true;
        response.CreatePlayerObject = true;
        response.Position = spawnPosition;
        response.Rotation = Quaternion.identity;
        response.Pending = false;
    }

    void HandleClientConnected(ulong clientId)
    {
        if (clientId == _networkManager.LocalClientId)
        {
            SetState(_networkManager.IsHost ? "Connected: Host" : "Connected: Client", true);
        }
    }

    void HandleClientStopped(bool wasHost)
    {
        string reason = _networkManager.DisconnectReason;
        SetState(string.IsNullOrEmpty(reason) ? "Disconnected" : reason);
    }

    void HandleTransportFailure() => SetState("Connection failed");

    void SetState(string status, bool connected = false, bool connecting = false)
    {
        State = new SessionChangedArgs
        {
            status = status,
            connected = connected,
            connecting = connecting,
        };
        SessionChanged?.Invoke(this, State);
    }

    void ConfigureNetworkManager()
    {
        _networkManager.NetworkConfig.AutoSpawnPlayerPrefabClientSide = false;
    }
}
