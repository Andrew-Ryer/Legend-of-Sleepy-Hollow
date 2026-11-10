using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float speed = 10f;
    // how fast the plane goes
    // [SerializeField] private float moveSpeed = 10f;
    // the rate of altitude change
    // [SerializeField] private float verticalSpeed = 10f;
    
    // controls if the plane goes up or down
    // float _upDownInput;

    void Start()
    {
        // plane moves forward by default in the x direction
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = transform.right * speed;
    }
    private void FixedUpdate()
    {
        // plane faces up or down in the y direction
        // plane goes up
        if (Keyboard.current.wKey.isPressed)
        {
            rb.linearVelocity += transform.up;
        }
        // plane goes down
        if (Keyboard.current.sKey.isPressed)
        {
            rb.linearVelocity -= transform.up;
        }
        Debug.Log("Altitude: " + rb.position.y);
        
        // plane turns right or left in the z direction
        // plane goes to the right 
        if (Keyboard.current.dKey.isPressed)
        {
            rb.linearVelocity -= transform.forward;
        }
        // plane goes to the left
        if (Keyboard.current.aKey.isPressed)
        {
            rb.linearVelocity += transform.forward;
        }
        
        /*if (Keyboard.current.spaceKey.isPressed)
        {
           
        }*/
    }
}
