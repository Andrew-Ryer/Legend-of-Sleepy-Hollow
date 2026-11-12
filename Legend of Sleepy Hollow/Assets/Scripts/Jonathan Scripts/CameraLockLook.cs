using System.Globalization;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class CameraLockLook : NetworkBehaviour
{
    [SerializeField] private Transform cameraTransform;

    private float updateTimer;
    void Update()
    {
        if (!IsOwner || cameraTransform == null)
            return;

        updateTimer += Time.deltaTime;

        if (updateTimer < 0.05f)
            return;

        updateTimer = 0f;

        Vector3 direction = cameraTransform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        if (IsServer)
        {
            transform.rotation = targetRotation;
        }
        else
        {
            RequestRotationServerRpc(targetRotation);
        }
    }
    [ServerRpc]
    private void RequestRotationServerRpc(
        Quaternion targetRotation)
    {
        transform.rotation = targetRotation;
    }
}
