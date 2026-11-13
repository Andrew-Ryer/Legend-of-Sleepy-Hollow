using System.Globalization;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class CameraLockLook : NetworkBehaviour
{
    [SerializeField] private Transform cameraTransform;

    private Rigidbody rb;
    private Quaternion targetRotation;

    private float updateTimer;

    private void Awake()
    {
        rb = GetComponentInChildren<Rigidbody>();
    }
    void Update()
    {
        if (!IsOwner || cameraTransform == null)
            return;
        /*
        updateTimer += Time.deltaTime;

        if (updateTimer < 0.05f)
            return;

        updateTimer = 0f;
        */
        Vector3 direction = cameraTransform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        targetRotation = Quaternion.LookRotation(direction);
        
        if (IsServer)
        {
            transform.rotation = targetRotation;
        }
        RequestRotationServerRpc(targetRotation);
        /*
        else
        {
            RequestRotationServerRpc(targetRotation);
        }
        */
        
    }

    private void FixedUpdate()
    {
        if (!IsServer || rb == null) return;

        rb.MoveRotation(targetRotation);
    }

    [ServerRpc]
    private void RequestRotationServerRpc(Quaternion rotation)
    {
        rb.rotation = rotation;
    }
}
