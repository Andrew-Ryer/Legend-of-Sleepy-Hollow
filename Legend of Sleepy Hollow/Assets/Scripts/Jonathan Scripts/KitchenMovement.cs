using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * This is a modified version of TestServerMovementPlayer which is Demo 06's server-authoritative counterpart to
 * Demo 05 and is reused as the player in Demo 07. 
 */

public class KitchenMovement : NetworkBehaviour
{
    [SerializeField] WaterGun_KF waterGun;
    [SerializeField] private GameObject cinemachineCamera;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;
    public float speed = 5f;
    public ulong OwnerClientId => NetworkObject.OwnerClientId;

    const float FirstPlayerLaneZ = 0f;
    const float PlayerLaneSpacing = 1.6f;
    bool _loggedFirstAcceptedMove;

    private Rigidbody rb;

    private Vector3 serverMoveDirection;
    private bool hasMovementInput;
    private void Awake()
    {
        rb = GetComponentInChildren<Rigidbody>();
    }
    public override void OnNetworkSpawn()
    {
        name = $"Server Movement Player {OwnerClientId}";

        if (IsServer)
            transform.position = GetLanePosition(OwnerClientId);

        ApplyOwnerColor();

        if (cinemachineCamera != null)
            cinemachineCamera.SetActive(IsOwner);

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
        HandleShoot();

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        //Vector3 combinedMovement = new Vector3(horizontalDirection, 0, verticalDirection).normalized;
        Vector3 combinedMovement = (forward * verticalDirection + right * horizontalDirection).normalized;
        //if (combinedMovement == Vector3.zero) return;
        //Debug.Log($"Sending movement: {horizontalDirection}");
        MoveIntentRpc(combinedMovement);

    }

    [Rpc(SendTo.Server)]
    void MoveIntentRpc(Vector3 direction, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;

        serverMoveDirection = Vector3.ClampMagnitude(direction, 1f);
        hasMovementInput = serverMoveDirection != Vector3.zero;
        /*
        //Debug.Log($"Server received movement: {direction}");
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

        //Rigidbody rb = GetComponentInChildren<Rigidbody>();

       // if (rb != null)
        //{
            rb.MovePosition(rb.position + clampedDirection * speed * Time.deltaTime);
        //}
        //MaintainServerLane();

        if (!_loggedFirstAcceptedMove)
        {
            _loggedFirstAcceptedMove = true;
            Debug.Log(
                $"[ServerMovement] Server accepted first movement for {name} | " +
                $"ownerClientId={OwnerClientId}");
        }
        */
    }

    void FixedUpdate() 
    {
        if (!IsServer || rb == null)
            return;

        //if (!hasMovementInput)
            //return;

        bool isMoving = hasMovementInput && serverMoveDirection.sqrMagnitude > 0.001f;

        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);
        }

        if (!isMoving)
        {
            return;
        }

        Vector3 movement = serverMoveDirection * speed * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + movement);
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

    //Alyssa contribution
    void HandleShoot()
    {
        if(Keyboard.current.eKey.isPressed) 
            waterGun.Shoot();
        else 
            waterGun.StopShoot();
    }
}
