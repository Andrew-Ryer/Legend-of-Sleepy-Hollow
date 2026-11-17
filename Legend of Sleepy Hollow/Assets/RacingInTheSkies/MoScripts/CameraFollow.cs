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
    Vector3 planePosition;
    Vector3 cameraPosition;

    /*private void Update()
    {
        Debug.Log("Camera: " + camera.transform.position);
        Debug.Log("Plane: " + transform.position);
    }*/

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner == false)
            return;
        
        camera = Camera.main;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && camera != null)
        {
            // camera.transform.position = originPosition;
        }
        
        base.OnNetworkDespawn();
    }

    private void LateUpdate()
    {
        if (IsOwner && camera != null)
        {
            planePosition = transform.position;
            cameraPosition = planePosition - transform.right * 5f + transform.up * 1f; 
            camera.transform.position = cameraPosition;
            Vector3 lookDirection = planePosition - cameraPosition;
            camera.transform.rotation =  Quaternion.LookRotation(lookDirection);
        }
    }
}

// [SerializeField] Vector3 offset;
//
// public Transform Target { get; set; }
//
// void LateUpdate()
// {
//     if (Target == null) return;
//     
//     transform.position = Target.position + offset;
//     // Debug.Log(transform.position);
//     // Debug.Log("Target: " + Target);
// }