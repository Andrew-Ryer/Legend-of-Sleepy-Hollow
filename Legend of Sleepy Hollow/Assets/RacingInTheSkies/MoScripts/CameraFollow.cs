using System;
using Unity.Netcode;
using UnityEngine;

/*
 * FollowCamera holds the camera at a fixed offset from a target. The offset is
 * set in the inspector; the owning player assigns itself as the target when it
 * spawns. Purely local and presentational, so this is a plain MonoBehaviour.
 */

public class CameraFollow : NetworkBehaviour
{
    private Camera camera;
    Vector3 offsetFromPlane;
    Vector3 originPosition;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner == false)
            return;
        camera = Camera.main;
        originPosition = camera.transform.position;
        offsetFromPlane = transform.position - camera.transform.position;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && camera != null)
        {
            camera.transform.position = originPosition;
        }
        base.OnNetworkDespawn();
    }

    private void LateUpdate()
    {
        if (IsOwner && camera != null)
        {
            camera.transform.position = transform.position + offsetFromPlane;
        }
    }
}
