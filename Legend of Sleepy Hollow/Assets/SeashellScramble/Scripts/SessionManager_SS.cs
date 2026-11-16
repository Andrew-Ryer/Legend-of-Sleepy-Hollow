using UnityEngine;
using Unity.Netcode;

public class SessionManager_SS : NetworkBehaviour
{
    public void Disconnect() => NetworkManager.Singleton.Shutdown();

    public void StartClient() => NetworkManager.Singleton.StartClient();

    public void StartHost() => NetworkManager.Singleton.StartHost();
}
