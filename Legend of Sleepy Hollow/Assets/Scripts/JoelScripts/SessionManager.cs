using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    [SerializeField] NetworkManager _networkManager;
    [SerializeField] UnityTransport _transport;

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
        SessionHud.SessionRequested += HandleSessionRequested;
        _networkManager.OnClientConnectedCallback += HandleClientConnected;
        _networkManager.OnClientStopped += HandleClientStopped;
        _networkManager.OnTransportFailure += HandleTransportFailure;
        _networkManager.ConnectionApprovalCallback += ApproveConnection;
    }

    void OnDisable()
    {
        SessionHud.SessionRequested -= HandleSessionRequested;
        _networkManager.OnClientConnectedCallback -= HandleClientConnected;
        _networkManager.OnClientStopped -= HandleClientStopped;
        _networkManager.OnTransportFailure -= HandleTransportFailure;
        _networkManager.ConnectionApprovalCallback -= ApproveConnection;
    }

    void HandleSessionRequested(object sender, SessionHud.SessionRequestedArgs e)
    {
        switch (e.action)
        {
            case SessionHud.SessionAction.Host:
                StartHost();
                break;
            case SessionHud.SessionAction.Client:
                StartClient();
                break;
            case SessionHud.SessionAction.Disconnect:
                Disconnect();
                break;
        }
    }

    public void StartHost()
    {
        if (_networkManager.IsListening || State.connecting) return;
        _transport.SetConnectionData("127.0.0.1", _transport.ConnectionData.Port, "0.0.0.0");
        SetState("Starting host...", false, true);
        try
        {
            if (!_networkManager.StartHost()) SetState("Could not start host");
        }
        catch (Exception e)
        {
            SetState(e.Message);
        }
    }

    public void StartClient()
    {
        if (_networkManager.IsListening || State.connecting) return;
        SetState("Connecting...", false, true);
        try
        {
            if (!_networkManager.StartClient()) SetState("Could not start client");
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

    void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        float angle = request.ClientNetworkId * 137.5f * Mathf.Deg2Rad;
        response.Approved = true;
        response.CreatePlayerObject = true;
        response.Position = new Vector3(Mathf.Sin(angle) * 2f, 1.05f, Mathf.Cos(angle) * 2f);
        response.Rotation = Quaternion.identity;
        response.Pending = false;
    }

    void HandleClientConnected(ulong clientId)
    {
        if (clientId != _networkManager.LocalClientId) return;
        SetState(_networkManager.IsHost ? "Connected: Host" : "Connected: Client", true);
    }

    void HandleClientStopped(bool wasHost)
    {
        string reason = _networkManager.DisconnectReason;
        SetState(string.IsNullOrEmpty(reason) ? "Disconnected" : reason);
    }

    void HandleTransportFailure() => SetState("Connection failed");

    void SetState(string status, bool connected = false, bool connecting = false)
    {
        State = new SessionChangedArgs { status = status, connected = connected, connecting = connecting };
        SessionChanged?.Invoke(this, State);
    }
}
