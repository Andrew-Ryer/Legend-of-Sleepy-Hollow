using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public Transform spawnpoint1;
    public Transform spawnpoint2;
    public Transform spawnpoint3;
    public Transform spawnpoint4;

    readonly Dictionary<ulong, int> clientSlots = new();
    readonly HashSet<int> occupiedSlots = new();

    [SerializeField]
    NetworkManager networkManager;

    void Awake()
    {
        if (networkManager == null)
            networkManager = FindFirstObjectByType<NetworkManager>();

        AssignSlot(NetworkManager.ServerClientId, 0);
    }

    void OnEnable()
    {
        if (networkManager != null)
            networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
    }

    void OnDisable()
    {
        if (networkManager != null)
            networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
    }

    public Vector3 GetSpawnPosition(ulong clientId)
    {
        Transform[] spawnPoints = { spawnpoint1, spawnpoint2, spawnpoint3, spawnpoint4 };

        if (!clientSlots.TryGetValue(clientId, out int spawnIndex))
        {
            if (clientId == NetworkManager.ServerClientId)
            {
                spawnIndex = 0;
            }
            else if (!TryGetNextSlot(out spawnIndex))
            {
                return transform.position;
            }

            AssignSlot(clientId, spawnIndex);
        }

        return spawnPoints[spawnIndex].position;
    }

    bool TryGetNextSlot(out int slot)
    {
        for (int i = 1; i < 4; i++)
        {
            if (occupiedSlots.Contains(i))
                continue;

            slot = i;
            return true;
        }

        slot = -1;
        return false;
    }

    void AssignSlot(ulong clientId, int slot)
    {
        clientSlots[clientId] = slot;
        occupiedSlots.Add(slot);
    }

    void HandleClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId)
        {
            clientSlots.Clear();
            occupiedSlots.Clear();
            AssignSlot(NetworkManager.ServerClientId, 0);
            return;
        }

        if (clientSlots.TryGetValue(clientId, out int slot))
        {
            clientSlots.Remove(clientId);
            occupiedSlots.Remove(slot);
        }
    }
}
