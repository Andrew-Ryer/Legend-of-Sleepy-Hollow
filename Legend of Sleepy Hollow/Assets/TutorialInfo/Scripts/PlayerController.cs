using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    // how fast the plane goes
    [SerializeField] private float moveSpeed = 10f;
    // the rate of altitude change
    [SerializeField] private float verticalSpeed = 10f;
    
    // controls if the plane goes up or down
    float _upDownInput;
    
    private void Update()
    {
        // initialize 
        _upDownInput = 0f;
        float _turnInput = 0f;
        
        // vertical movement of the plane
        
        if (Keyboard.current.wKey.isPressed)
        {
            _upDownInput += 1f;
        }
        if (Keyboard.current.sKey.isPressed)
        {
            _upDownInput -= 1f;
        }
        
        // horizontal movement of the plane
        if (Keyboard.current.dKey.isPressed)
        {
            _turnInput += 1f;
        }
        if (Keyboard.current.aKey.isPressed)
        {
            _turnInput -= 1f;
        }
        
        Vector3 verticalMovement = new Vector3(0, verticalSpeed * _upDownInput, 0);
        Vector3 horizontalMovement = new Vector3(0, 0, _turnInput);
        
        // holding down the space will make the plane go faster 
        // it will be a temporary boost or gradual acceleration until
        // you are no longer holding space
        /*if (Keyboard.current.spaceKey.isPressed)
        {
            moveSpeed;
        }*/
    }
}
