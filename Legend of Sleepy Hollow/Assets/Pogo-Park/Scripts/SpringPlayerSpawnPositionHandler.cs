using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

// The script that reads springPlayerPosition and spawns them in a set position
// Works with springPlayerSpawnPositioNHandler
public class SpringPlayerSpawnPositionHandler : MonoBehaviour
{
    [SerializeField] NetworkManager _networkManager;
    [SerializeField] Transform _spawnPoint;
    [SerializeField, Min(0f)] float _spacing = 2f;

    readonly Dictionary<ulong, SpringPlayerPosition> _clientSlots = new();
    readonly HashSet<SpringPlayerPosition> _occupiedSlots = new();
    bool _isSubscribed;

    void Awake()
    {
        if (_networkManager == null)
            _networkManager = NetworkManager.Singleton;

        AssignSlot(NetworkManager.ServerClientId, SpringPlayerPosition.Player1);
    }

    void OnEnable()
    {
        SubscribeToNetworkManager();
    }

    void Start()
    {
        SubscribeToNetworkManager();
    }

    void SubscribeToNetworkManager()
    {
        if (_isSubscribed)
            return;

        if (_networkManager == null)
            _networkManager = NetworkManager.Singleton;

        if (_networkManager != null)
        {
            _networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            _isSubscribed = true;
        }
    }

    void OnDisable()
    {
        if (_networkManager != null && _isSubscribed)
            _networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        _isSubscribed = false;
    }

    public bool TryGetSpawnPosition(ulong clientId, out Vector3 position)
    {
        if (!_clientSlots.TryGetValue(clientId, out SpringPlayerPosition slot))
        {
            if (clientId == NetworkManager.ServerClientId)
            {
                slot = SpringPlayerPosition.Player1;
            }
            else if (!TryGetNextSlot(out slot))
            {
                position = default;
                return false;
            }

            AssignSlot(clientId, slot);
        }

        position = GetCenteredSpawnPosition(slot);
        RecenterSpawnedPlayers();
        return true;
    }

    Vector3 GetCenteredSpawnPosition(SpringPlayerPosition slot)
    {
        int rank = 0;
        foreach (SpringPlayerPosition activeSlot in _clientSlots.Values)
        {
            if ((int)activeSlot < (int)slot)
                rank++;
        }

        float centeredOffset = rank - (_clientSlots.Count - 1) * 0.5f;
        Vector3 origin = _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
        return origin + Vector3.right * (centeredOffset * _spacing);
    }

    void RecenterSpawnedPlayers()
    {
        if (_networkManager == null || !_networkManager.IsServer || _networkManager.SpawnManager == null)
            return;

        foreach (NetworkObject player in _networkManager.SpawnManager.SpawnedObjectsList)
        {
            if (player == null || !player.IsPlayerObject ||
                !_clientSlots.TryGetValue(player.OwnerClientId, out SpringPlayerPosition slot))
                continue;

            Vector3 position = player.transform.position;
            position.x = GetCenteredSpawnPosition(slot).x;

            NetworkTransform networkTransform = player.GetComponent<NetworkTransform>();
            if (networkTransform != null)
            {
                networkTransform.Teleport(position, player.transform.rotation, player.transform.localScale);
            }
            else
            {
                Rigidbody body = player.GetComponent<Rigidbody>();
                if (body != null)
                    body.position = position;
                else
                    player.transform.position = position;
            }
        }
    }

    bool TryGetNextSlot(out SpringPlayerPosition slot)
    {
        for (int i = 1; i < 4; i++)
        {
            SpringPlayerPosition candidate = (SpringPlayerPosition)i;
            if (_occupiedSlots.Contains(candidate))
                continue;

            slot = candidate;
            return true;
        }

        slot = default;
        return false;
    }

    void AssignSlot(ulong clientId, SpringPlayerPosition slot)
    {
        _clientSlots[clientId] = slot;
        _occupiedSlots.Add(slot);
    }

    void HandleClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId)
        {
            _clientSlots.Clear();
            _occupiedSlots.Clear();
            AssignSlot(NetworkManager.ServerClientId, SpringPlayerPosition.Player1);
            return;
        }

        if (_clientSlots.TryGetValue(clientId, out SpringPlayerPosition slot))
        {
            _clientSlots.Remove(clientId);
            _occupiedSlots.Remove(slot);
            RecenterSpawnedPlayers();
        }
    }
}
