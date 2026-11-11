using Unity.Netcode;
using UnityEngine;

// Script to work with the spawnPositionHandler 
public class SpringPlayerSpawnAssignment : NetworkBehaviour
{
    [SerializeField] Rigidbody _body;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        SpringPlayerSpawnPositionHandler spawnPositionHandler =
            FindFirstObjectByType<SpringPlayerSpawnPositionHandler>();

        if (spawnPositionHandler == null)
            return;

        if (!spawnPositionHandler.TryGetSpawnPosition(OwnerClientId, out Vector3 spawnPosition))
        {
            NetworkManager.DisconnectClient(OwnerClientId);
            return;
        }

        if (_body == null)
            _body = GetComponent<Rigidbody>();

        if (_body != null)
        {
            _body.position = spawnPosition;
            _body.rotation = Quaternion.identity;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
        }
        else
        {
            transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
        }
    }
}