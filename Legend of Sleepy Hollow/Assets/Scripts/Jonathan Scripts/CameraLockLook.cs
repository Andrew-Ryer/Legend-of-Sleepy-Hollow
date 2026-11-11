using System.Globalization;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class CameraLockLook : NetworkBehaviour
{
    [SerializeField] private Transform cameraTransform;

    void LateUpdate()
    {
        if (!IsOwner)
            return;

        if (cameraTransform == null)
            return;

        Vector3 direction = cameraTransform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

}
