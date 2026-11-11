using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlaneControllers : MonoBehaviour
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
        // Rotation z changes nose pitch
        // positive faces up and negative faces down
        // plane goes up
        if (Keyboard.current.wKey.isPressed)
        {
            // feels faster and more immediate
            rb.linearVelocity += transform.up;
            // feels less effective and slower
            // rb.linearVelocity = transform.up;
        }
        // plane goes down
        if (Keyboard.current.sKey.isPressed)
        {
            // feels faster and more immediate
            rb.linearVelocity -= transform.up;
            // feels less effective and slower
            // rb.linearVelocity = -transform.up;
        }
        Debug.Log("Altitude: " + rb.position.y);
        
        // plane turns right or left in the z direction
        // Rotation x makes the plane bank
        // Positive is to the left and negative is to the right
        // plane goes to the right 
        if (Keyboard.current.dKey.isPressed)
        {
            // feels faster and more immediate
            rb.linearVelocity -= transform.forward;
            // feels less effective and slower
            // rb.linearVelocity = -transform.forward;
        }
        // plane goes to the left
        if (Keyboard.current.aKey.isPressed)
        {
            // feels faster and more immediate
            rb.linearVelocity += transform.forward;
            // feels less effective and slower
            // rb.linearVelocity = transform.forward;
        }
        
        /*if (Keyboard.current.spaceKey.isPressed)
        {
           
        }*/
    }
}
