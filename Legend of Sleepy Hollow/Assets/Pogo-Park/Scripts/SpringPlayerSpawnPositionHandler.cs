
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// The script that reads SpringPlayerPosition and spawns them in a set position
public class SpringPlayerSpawnPositionHandler : MonoBehaviour
{
    [SerializeField] NetworkManager _networkManager;
    [SerializeField] Vector3 _firstSpawnPosition = Vector3.zero;
    [SerializeField, Min(0f)] float _spacing = 10f;

    readonly Dictionary<ulong, SpringPlayerPosition> _clientSlots = new();
    readonly HashSet<SpringPlayerPosition> _occupiedSlots = new();

    void Awake()
    {
        if (_networkManager == null)
            _networkManager = GetComponent<NetworkManager>();

        AssignSlot(NetworkManager.ServerClientId, SpringPlayerPosition.Player1);
    }

    void OnEnable()
    {
        if (_networkManager != null)
            _networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
    }

    void OnDisable()
    {
        if (_networkManager != null)
            _networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
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

        position = _firstSpawnPosition + Vector3.right * ((int)slot * _spacing);
        return true;
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
        }
    }
}



