using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;

public class SkyboundPlayerCameraController : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner) return;

        CinemachineCamera cam = FindAnyObjectByType<CinemachineCamera>();

        if (cam != null)
        {
            cam.Follow = transform;
            cam.LookAt = transform;
        }
    }
}
