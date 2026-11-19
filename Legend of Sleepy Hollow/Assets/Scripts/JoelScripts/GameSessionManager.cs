using Unity.Netcode;
using UnityEngine;

/*
 * GameSessionManager boots the shared session. It lives in the overworld scene
 * only: while the server side of a session is up it spawns the GameSession
 * prefab, the one object that carries the roster and the scene trips into every
 * minigame scene. From then on, keeping seats claimed and scores safe is the
 * GameSession object's own job.
 *
 * The NetworkManager and the session prefab are wired in the inspector.
 */

public class GameSessionManager : MonoBehaviour
{
    [SerializeField] NetworkManager _networkManager;
    [SerializeField] GameObject _gameSessionPrefab;

    void OnEnable()
    {
        // A trip back to the overworld loads a second copy of this scene. The
        // session already has a bootstrap, so this copy steps aside.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton != _networkManager)
        {
            Destroy(gameObject);
            return;
        }

        // The overworld scene closes during a trip, so this object has to stay
        // alive to spawn the session object again when the host hosts again.
        DontDestroyOnLoad(gameObject);

        _networkManager.OnConnectionEvent += HandleConnectionEvent;
    }

    void OnDisable()
    {
        _networkManager.OnConnectionEvent -= HandleConnectionEvent;
    }

    void HandleConnectionEvent(NetworkManager networkManager, ConnectionEventData eventData)
    {
        if (!networkManager.IsServer) return;
        if (eventData.EventType != ConnectionEvent.ClientConnected) return;

        // The host's own client gets this event too, so the session object
        // exists as soon as the game is hosting.
        EnsureSessionObject();
    }

    // Only the server spawns; every client receives the replicated copy.
    void EnsureSessionObject()
    {
        if (PlayerRoster.Local != null) return;

        GameObject session = Instantiate(_gameSessionPrefab);
        session.GetComponent<NetworkObject>().Spawn();
    }
}
