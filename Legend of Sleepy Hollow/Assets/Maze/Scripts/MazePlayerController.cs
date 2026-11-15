using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class MazePlayerController : NetworkBehaviour
{
    [SerializeField]
    CharacterController _characterController;

    [SerializeField]
    InputActionReference _moveAction;

    [SerializeField]
    float _movementSpeed = 4f;

    readonly NetworkVariable<Vector3> _spawnPosition = new(
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    float _verticalSpeed;

    public override void OnNetworkSpawn()
    {
        _spawnPosition.OnValueChanged += HandleSpawnPositionChanged;

        if (IsServer)
            _spawnPosition.Value = transform.position;

        ApplySpawnPosition(_spawnPosition.Value);
        _characterController.enabled = IsOwner;
        if (IsOwner)
            _moveAction.action.Enable();
    }

    public override void OnNetworkDespawn()
    {
        _spawnPosition.OnValueChanged -= HandleSpawnPositionChanged;
    }

    void HandleSpawnPositionChanged(Vector3 previous, Vector3 current)
    {
        if (IsOwner)
            ApplySpawnPosition(current);
    }

    void ApplySpawnPosition(Vector3 position)
    {
        bool wasEnabled = _characterController.enabled;
        _characterController.enabled = false;
        transform.SetPositionAndRotation(position, Quaternion.identity);
        _characterController.enabled = wasEnabled;
    }

    void Update()
    {
        if (!IsSpawned || !IsOwner)
            return;

        Vector2 input = _moveAction.action.ReadValue<Vector2>();
        Vector3 motion =
            Vector3.ClampMagnitude(new Vector3(input.x, 0f, input.y), 1f) * _movementSpeed;

        if (_characterController.isGrounded && _verticalSpeed < 0f)
            _verticalSpeed = -2f;
        _verticalSpeed += Physics.gravity.y * Time.deltaTime;
        motion.y = _verticalSpeed;
        _characterController.Move(motion * Time.deltaTime);
    }
}
