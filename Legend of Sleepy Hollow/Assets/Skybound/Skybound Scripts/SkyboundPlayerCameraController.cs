using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;

public class SkyboundPlayerCameraController : NetworkBehaviour
{
    [SerializeField] private CinemachineCamera cam;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (cam == null)
        {
            cam = GetComponent<CinemachineCamera>();
        }
        if (!IsOwner)
        {
            if (cam != null)
            {
                cam.gameObject.SetActive(false);
            }
            enabled = false;
            return;
        }
        if (cam != null)
        {
            cam.Follow = transform;
            cam.LookAt = transform;
            
        }
    }
}