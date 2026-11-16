using Unity.Netcode;
using UnityEngine;

/*
 * GameSessionManager boots the shared session: while the server side of a
 * session is up it spawns the GameSession prefab (the persistent object holding
 * the roster), and opens a roster entry for every client that connects.
 *
 * The NetworkManager and the session prefab are wired in the inspector.
 */

public class GameSessionManager : MonoBehaviour
{
    [SerializeField] NetworkManager _networkManager;
    [SerializeField] GameObject _gameSessionPrefab;

    void OnEnable()
    {
        _networkManager.OnConnectionEvent += HandleConnectionEvent;
    }

    void OnDisable()
    {
        _networkManager.OnConnectionEvent -= HandleConnectionEvent;
    }

    void HandleConnectionEvent(NetworkManager networkManager, ConnectionEventData eventData)
    {
        if (!networkManager.IsServer) return;

        switch (eventData.EventType)
        {
            case ConnectionEvent.ClientConnected:
                // The host's own client gets this event too, so the session
                // object exists as soon as the game is hosting.
                EnsureSessionObject();
                PlayerRoster.Local?.ServerEnsureEntry(eventData.ClientId);
                break;
            case ConnectionEvent.ClientDisconnected:
                PlayerRoster.Local?.ServerReleaseEntry(eventData.ClientId);
                break;
        }
    }

    // Only the server spawns; every client receives the replicated copy.
    void EnsureSessionObject()
    {
        if (PlayerRoster.Local != null) return;

        GameObject session = Instantiate(_gameSessionPrefab);
        session.GetComponent<NetworkObject>().Spawn();
    }
}
