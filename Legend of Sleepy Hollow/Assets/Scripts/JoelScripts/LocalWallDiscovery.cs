using Unity.Netcode;
using UnityEngine;

public class LocalWallDiscovery : MonoBehaviour
{
    [Header("Discovery")]
    [Tooltip("Distance from the wall in world units, measured across the maze floor.")]
    [Min(0f)]
    [SerializeField] float _revealRadius = 3f;

    [Header("Wall Components")]
    [SerializeField] Renderer _wallRenderer;
    [SerializeField] Collider _wallCollider;

    void Awake()
    {
        _wallRenderer.enabled = false;
    }

    void LateUpdate()
    {
        // Visibility belongs to this client, not to whichever player is nearest on the server.
        NetworkManager manager = NetworkManager.Singleton;
        NetworkObject player = manager != null && manager.IsConnectedClient
            ? manager.LocalClient.PlayerObject : null;
        bool nearby = false;

        if (player != null && player.IsSpawned && player.IsOwner)
        {
            Vector3 offset = _wallCollider.ClosestPoint(player.transform.position) - player.transform.position;
            offset.y = 0f;
            nearby = offset.sqrMagnitude <= _revealRadius * _revealRadius;
        }

        if (_wallRenderer.enabled != nearby)
            _wallRenderer.enabled = nearby;
    }
}
