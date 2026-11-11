using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlaneControllers : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Vector3 speed = new Vector3(10f, 0f, 0f);
    [SerializeField] private Vector3 topSpeed = new Vector3(20f, 0f, 0f);
    // how fast the plane goes
    // the rate of altitude change
    private Vector3 verticalSpeed = new Vector3(0f, 0f, 1f);
    // the bank angle of the plane
    private Vector3 bankAngle = new Vector3(1f, 0f, 0f);
    // the heading of the plane
    private Vector3 yawHeading = new Vector3(0f, 1f, 0f);

    void Start()
    {
        // plane moves forward by default in the x direction
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = speed;
    }
    private void FixedUpdate()
    {
        Debug.Log("Vertical Speed: " + rb.angularVelocity.z);
        Debug.Log("Move Speed: " + rb.linearVelocity.x);
        Debug.Log("Altitude: " + rb.position.y);
        Debug.Log("Heading : "  + rb.rotation.y);
        // This is to prevent the plane from pitching farther
        // without pressing any buttons
        rb.angularVelocity = new Vector3(0f, 0f, 0f);
        // rb.angularVelocity = new Vector3(rb.angularVelocity.x, rb.angularVelocity.y, 0f);
        
        // Rotation z changes nose pitch
        // positive faces up and negative faces down
        // plane goes up
        if (Keyboard.current.wKey.isPressed)
        {
            // feels faster and more immediate
            rb.linearVelocity += transform.up;
            // feels less effective and slower
            
            rb.angularVelocity += verticalSpeed;
            // goes up and up without stopping
            // rb.angularVelocity = verticalSpeed;
        }
        
        // plane goes down
        if (Keyboard.current.sKey.isPressed)
        {
            // feels faster and more immediate
            rb.linearVelocity -= transform.up;
            // feels less effective and slower
            // rb.linearVelocity = -transform.up;
            
            rb.angularVelocity -= verticalSpeed;
            // goes down and down without stopping
            // rb.angularVelocity = -verticalSpeed;
        }
        
        // plane turns right or left in the z direction
        // Rotation x makes the plane bank
        // Positive is to the left and negative is to the right
        // plane goes to the right 
        // the change of the heading is
        // positive y to the right and negative to the left
        if (Keyboard.current.dKey.isPressed)
        {
            // bank angle
            // rb.angularVelocity -= transform.right;
            rb.angularVelocity -= bankAngle;
            
            // yaw heading
            // rb.angularVelocity += transform.up;
            rb.angularVelocity += yawHeading;

            rb.linearVelocity -= transform.forward;
        }
        // plane goes to the left
        if (Keyboard.current.aKey.isPressed)
        {
            // bank angle 
            // rb.angularVelocity += transform.right;
            rb.angularVelocity += bankAngle;
            
            // yaw heading
            // rb.angularVelocity -= transform.up;
            rb.angularVelocity -= yawHeading;
            
            rb.linearVelocity += transform.forward;
            
        }
        
        // The space button will give the plane
        // temporary boost as long as the space button
        // is held or
        // a gradual boost until you hit top speed
        if (Keyboard.current.spaceKey.isPressed)
        {
            if (rb.linearVelocity.x < topSpeed.x)
            {
                rb.linearVelocity += speed;
            }
        }
        
        // I am thinking of adding a button
        // if you press b then the plane slows down
        // it will gradually slow down until 
        // you hit your original speed
        if (Keyboard.current.bKey.isPressed)
        {
            if (rb.linearVelocity.x > speed.x)
            {
                rb.linearVelocity -= speed;
            }
        }
    }
}
