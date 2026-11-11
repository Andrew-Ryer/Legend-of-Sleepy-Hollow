using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerController_SS : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] CharacterController _characterController;
    // [SerializeField] PlayerHeldItem _heldItem;
    [SerializeField] Camera _camera;

    [Header("Detection")]
    // [SerializeField] float _detectionRadius = 3f;
    // [SerializeField] float _detectionAngle = 60f;

    [Header("Movement")]
    [SerializeField] float _movementSpeed = 4f;
    [SerializeField] float _rightRotateSensitivity = 50f;
    [SerializeField] float _upRotateSensitivity = 50f;
    Vector2 _smoothedInput;


    void Update()
    {        
        //grabs cursor's location on the screen
        Vector3 mousePos = Mouse.current.position.ReadValue();

        //cursor will be zero at center of screen 
        float upOffset = (mousePos.y - Screen.height * 0.5f) / (Screen.height * 0.5f);
        float rightOffset = (mousePos.x - Screen.width * 0.5f) / (Screen.width * 0.5f);

        //calculates rotation direction and amount based on set sensitivity
        float upRotation = upOffset * -_upRotateSensitivity;
        float rightRotation = rightOffset * _rightRotateSensitivity;
        
        //only camera rotates while the player stays upright 
        _camera.transform.localRotation = Quaternion.Euler(Mathf.Clamp(upRotation, -100, 100), 0f, 0f);
        
        //plays body rotates to walk in the pointed direction
        transform.rotation = Quaternion.Euler(0f, rightRotation, 0f);

        //get input from player
        Vector2 input = ReadMovementInput();
        _smoothedInput = Vector2.MoveTowards(_smoothedInput, input, Time.deltaTime * 10f);

        //allows player to turn directions
        _characterController.Move(transform.right * _smoothedInput.x * _movementSpeed * Time.deltaTime);        

        //player movement going forward
        _characterController.Move(transform.forward * _smoothedInput.y * _movementSpeed * Time.deltaTime);
            

    }

    static Vector2 ReadMovementInput()
    {
        //return WASD input as a two-dimensional vector.
        Vector2 input = Vector2.zero;
        if(Keyboard.current.dKey.isPressed) input.x += 1f;
        if(Keyboard.current.aKey.isPressed) input.x -= 1f;
        if(Keyboard.current.wKey.isPressed) input.y += 1f;
        if(Keyboard.current.sKey.isPressed) input.y -= 1f;

        return input;
    }

}
