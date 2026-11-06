using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * This is a modified version of TestServerMovementPlayer which is Demo 06's server-authoritative counterpart to
 * Demo 05 and is reused as the player in Demo 07. 
 */

public class TestServerMovementPlayer : NetworkBehaviour
{
    public float speed = 5f;
    public ulong OwnerClientId => NetworkObject.OwnerClientId;

    const float FirstPlayerLaneZ = 0f;
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
    void Start()
    {
        if (NetworkManager.Singleton != null &&
            !NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartHost();
        }
    }
    void Update()
    {
        if (!IsSpawned) return;
        /*
        if (IsServer)
            MaintainServerLane();
        */

        if (!IsOwner) return;
        if (Keyboard.current == null) return;

        float horizontalDirection = 0f;
        float verticalDirection = 0f;
        if (Keyboard.current.aKey.isPressed) horizontalDirection -= 1f;
        if (Keyboard.current.dKey.isPressed) horizontalDirection += 1f;
        if (Keyboard.current.sKey.isPressed) verticalDirection -= 1f;
        if (Keyboard.current.wKey.isPressed) verticalDirection += 1f;

        Vector3 combinedMovement = new Vector3(horizontalDirection, 0, verticalDirection).normalized;
        if (combinedMovement == Vector3.zero) return;
        Debug.Log($"Sending movement: {horizontalDirection}");
        MoveIntentRpc(combinedMovement);

    }

    [Rpc(SendTo.Server)]
    void MoveIntentRpc(Vector3 direction, RpcParams rpcParams = default)
    {
        Debug.Log($"Server received movement: {direction}");
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        if (senderClientId != OwnerClientId)
        {
            Debug.LogWarning(
                $"[ServerMovement] Rejected movement request for {name} | " +
                $"senderClientId={senderClientId} | ownerClientId={OwnerClientId}");
            return;
        }

        Vector3 clampedDirection = Vector3.ClampMagnitude(direction, 1f);
        if (clampedDirection == Vector3.zero) return;

        transform.Translate(clampedDirection * speed * Time.deltaTime);
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
        float laneZ = FirstPlayerLaneZ - (ownerClientId * PlayerLaneSpacing);
        return new Vector3(0f, 0f, laneZ);
    }

    void MaintainServerLane()
    {
        Vector3 position = transform.position;
        position.y = GetLanePosition(OwnerClientId).y;
        transform.position = position;
    }
}
