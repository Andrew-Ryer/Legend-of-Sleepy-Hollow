using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class SkyboundPlayerController : NetworkBehaviour
{
    [SerializeField] CharacterController _characterController;
    [SerializeField] InputActionReference _moveAction;
    [SerializeField] float _movementSpeed = 4f;

    float _verticalSpeed;

    public override void OnNetworkSpawn()
    {
        _characterController.enabled = IsOwner;
        if (IsOwner) _moveAction.action.Enable();
    }

    void FixedUpdate()
    {
        Vector3 input = new Vector3(0f,0f,0f);
        if (!IsSpawned || !IsOwner) return;

        if ( Keyboard.current.wKey.isPressed)
        {
            input.x = 1f;
        }
        if ( Keyboard.current.sKey.isPressed)
        {
            input.x = -1f;
        }
        if ( Keyboard.current.aKey.isPressed)
        {
            input.z = 1f;
        }
        if ( Keyboard.current.dKey.isPressed)
        {
            input.z = -1f;
        }
        
        Vector3 motion = Vector3.ClampMagnitude(input, 1f) * _movementSpeed;

        if (_characterController.isGrounded && _verticalSpeed < 0f)
        {
            _verticalSpeed = -2f;
        }

        if (_characterController.isGrounded && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            _verticalSpeed = 5f; 
        }

        _verticalSpeed += Physics.gravity.y * Time.deltaTime;
        motion.y = _verticalSpeed;

        _characterController.Move(motion * Time.deltaTime);
    }
}
