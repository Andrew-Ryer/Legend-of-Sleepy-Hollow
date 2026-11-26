using System;
using UnityEngine;
using Unity.Netcode;

public class HammerObstacle : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] float spinSpeed = 10f;
    [SerializeField] bool spinRight = true;
    

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!IsServer) return;

        float direction = spinRight ? 1f : -1f;
        transform.Rotate(Vector3.forward * spinSpeed * direction * Time.fixedDeltaTime);

    }
}
