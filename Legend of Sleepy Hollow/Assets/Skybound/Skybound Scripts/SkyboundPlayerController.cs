using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class SkyboundPlayerController : NetworkBehaviour
{
    [SerializeField] CharacterController _characterController;
    [SerializeField] InputActionReference _moveAction;
    [SerializeField] float _movementSpeed = 4f;
    [SerializeField] float _rotationSpeed = 10f;

    float _verticalSpeed;
    Transform _cameraTransform;

    public override void OnNetworkSpawn()
    {
        _characterController.enabled = IsOwner;
        if (IsOwner) _moveAction.action.Enable();

        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }

    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && _moveAction != null)
        {
            _moveAction.action.Disable();
        }
    }

    void FixedUpdate()
    {
        if (!IsSpawned || !IsOwner) return;

        Vector3 input = Vector3.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.z += 1f;
            if (Keyboard.current.sKey.isPressed) input.z -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        }

        Vector3 inputDir = new Vector3(input.x, 0f, input.z).normalized;
        Vector3 motion = Vector3.zero;

        if (inputDir.magnitude >= 0.1f && _cameraTransform != null)
        {
            Vector3 cameraForward = _cameraTransform.forward;
            Vector3 cameraRight = _cameraTransform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDir = (cameraForward * inputDir.z + cameraRight * inputDir.x).normalized;
            motion = moveDir * _movementSpeed;
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime);
        }

        if (_characterController.isGrounded && _verticalSpeed < 0f)
        {
            _verticalSpeed = -2f;
        }

        if (_characterController.isGrounded && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            _verticalSpeed = 5f;
        }

        _verticalSpeed += Physics.gravity.y * Time.fixedDeltaTime;
        motion.y = _verticalSpeed;
        _characterController.Move(motion * Time.fixedDeltaTime);
    }
    public void Bounce(float force)
    {
        _verticalSpeed = force;
    }
}


