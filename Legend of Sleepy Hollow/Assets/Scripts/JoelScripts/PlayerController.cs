using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
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

    void Update()
    {
        if (!IsSpawned || !IsOwner) return;

        Vector2 input = _moveAction.action.ReadValue<Vector2>();
        Vector3 motion = Vector3.ClampMagnitude(new Vector3(input.x, 0f, input.y), 1f) * _movementSpeed;

        if (_characterController.isGrounded && _verticalSpeed < 0f) _verticalSpeed = -2f;
        _verticalSpeed += Physics.gravity.y * Time.deltaTime;
        motion.y = _verticalSpeed;
        _characterController.Move(motion * Time.deltaTime);
    }
}
