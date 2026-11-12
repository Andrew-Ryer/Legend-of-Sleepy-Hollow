using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

// Script to work with the spawnPositionHandler
public class SpringPlayerSpawnAssignment : NetworkBehaviour
{
    [SerializeField] Rigidbody _body;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            StartCoroutine(AssignSpawnPosition());
    }

    IEnumerator AssignSpawnPosition()
    {
        SpringPlayerSpawnPositionHandler spawnPositionHandler = FindFirstObjectByType<SpringPlayerSpawnPositionHandler>();

        if (spawnPositionHandler == null)
            yield break;

        if (!spawnPositionHandler.TryGetSpawnPosition(OwnerClientId, out Vector3 spawnPosition))
        {
            NetworkManager.DisconnectClient(OwnerClientId);
            yield break;
        }

        yield return new WaitForFixedUpdate();

        if (!IsSpawned)
            yield break;

        if (_body == null)
            _body = GetComponent<Rigidbody>();

        NetworkTransform networkTransform = GetComponent<NetworkTransform>();
        if (networkTransform != null)
        {
            networkTransform.Teleport(spawnPosition, Quaternion.identity, transform.localScale);
        }
        else if (_body != null)
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