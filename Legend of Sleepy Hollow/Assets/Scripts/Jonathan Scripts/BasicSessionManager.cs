using Unity.Netcode;
using UnityEngine;

/*
 * BasicSessionManager is Demo 01's connection layer and later demos reuse it.
 * It starts a host or client and disconnects. Only the server writes the
 * replicated player count; the host is also one of the connected clients.
 */

public class BasicSessionManager : NetworkBehaviour
{
    public bool IsConnected => NetworkManager!.IsClient || NetworkManager!.IsServer;
    public int PlayerCount => _playerCount.Value;
    public string LocalRole
    {
        get
        {
            if (NetworkManager!.IsHost) return "Host";
            if (NetworkManager!.IsServer) return "Server";
            if (NetworkManager!.IsClient) return "Client";

            return "Disconnected";
        }
    }

    readonly NetworkVariable<int> _playerCount = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;
        
        UpdatePlayerCount();
        NetworkManager.OnConnectionEvent += HandleConnectionEvent;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (!NetworkManager!.IsServer) return;
        
        NetworkManager.OnConnectionEvent -= HandleConnectionEvent;
        _playerCount.Value = 0;
    }

    public void StartHost() => NetworkManager!.StartHost();

    public void StartClient() => NetworkManager!.StartClient();

    public void Disconnect() => NetworkManager!.Shutdown();

    void HandleConnectionEvent(NetworkManager networkManager, ConnectionEventData eventData)
    {
        Debug.Assert(IsServer);
        UpdatePlayerCount();
    }

    void UpdatePlayerCount()
    {
        // Only the server writes NetworkVariables. Clients receive the updated value.
        _playerCount.Value = NetworkManager.ConnectedClientsIds.Count;
    }
}
