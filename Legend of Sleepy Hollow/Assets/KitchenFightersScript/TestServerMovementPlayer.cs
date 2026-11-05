using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * TestServerMovementPlayer is Demo 06's server-authoritative counterpart to
 * Demo 05 and is reused as the player in Demo 07. The owner sends direction
 * intent through an RPC; the server validates it, moves the object, and places
 * it on a lane from OwnerClientId, not Demo 04's compact spawn slot. The client
 * never sends a final position.
 */

public class TestServerMovementPlayer : NetworkBehaviour
{
    public float speed = 5f;
    public ulong OwnerClientId => NetworkObject.OwnerClientId;

    const float FirstPlayerLaneY = -1.4f;
    const float PlayerLaneSpacing = 1.6f;

    bool _loggedFirstAcceptedMove;

    public override void OnNetworkSpawn()
    {
        name = $"Server Movement Player {OwnerClientId}";

        if (IsServer)
            transform.position = GetLanePosition(OwnerClientId);

        ApplyOwnerColor();

        Debug.Log(
            $"[ServerMovement] {name} spawned | ownerClientId={OwnerClientId} | " +
            $"position={transform.position} | isOwner={IsOwner} | isServer={IsServer} | isClient={IsClient}");
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log($"[ServerMovement] {name} despawned | ownerClientId={OwnerClientId}");
    }

    void Update()
    {
        if (IsServer)
            MaintainServerLane();

        if (!IsOwner) return;
        if (Keyboard.current == null) return;

        float direction = 0f;
        if (Keyboard.current.aKey.isPressed) direction -= 1f;
        if (Keyboard.current.dKey.isPressed) direction += 1f;

        if (Mathf.Approximately(direction, 0f)) return;

        MoveIntentRpc(direction);
    }

    [Rpc(SendTo.Server)]
    void MoveIntentRpc(float direction, RpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        if (senderClientId != OwnerClientId)
        {
            Debug.LogWarning(
                $"[ServerMovement] Rejected movement request for {name} | " +
                $"senderClientId={senderClientId} | ownerClientId={OwnerClientId}");
            return;
        }

        float clampedDirection = Mathf.Clamp(direction, -1f, 1f);
        if (Mathf.Approximately(clampedDirection, 0f)) return;

        transform.Translate(Vector3.right * (clampedDirection * speed * Time.deltaTime));
        MaintainServerLane();

        if (!_loggedFirstAcceptedMove)
        {
            _loggedFirstAcceptedMove = true;
            Debug.Log(
                $"[ServerMovement] Server accepted first movement for {name} | " +
                $"ownerClientId={OwnerClientId}");
        }
    }

    void ApplyOwnerColor()
    {
        if (!TryGetComponent(out Renderer playerRenderer)) return;

        playerRenderer.material.color = Color.HSVToRGB((OwnerClientId * 0.17f) % 1f, 0.75f, 1f);
    }

    static Vector3 GetLanePosition(ulong ownerClientId)
    {
        float laneY = FirstPlayerLaneY - (ownerClientId * PlayerLaneSpacing);
        return new Vector3(0f, laneY, 0f);
    }

    void MaintainServerLane()
    {
        Vector3 position = transform.position;
        position.y = GetLanePosition(OwnerClientId).y;
        position.z = 0f;
        transform.position = position;
    }
}
