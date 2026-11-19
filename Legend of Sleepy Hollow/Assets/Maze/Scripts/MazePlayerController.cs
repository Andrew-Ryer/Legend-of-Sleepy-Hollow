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
    int _startingCorner = -1;
    int _reachedCornersMask;
    bool _progressResetForPlayingMatch;
    SessionStateMachine _sessionStateMachine;

    public override void OnNetworkSpawn()
    {
        _spawnPosition.OnValueChanged += HandleSpawnPositionChanged;

        if (IsServer)
        {
            _spawnPosition.Value = transform.position;
            SpawnManager spawnManager = FindFirstObjectByType<SpawnManager>();
            if (spawnManager != null)
                _startingCorner = spawnManager.GetSpawnIndex(transform.position);
        }

        _reachedCornersMask = 0;
        _progressResetForPlayingMatch = false;

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
        if (!IsSpawned)
            return;

        if (_sessionStateMachine == null)
            _sessionStateMachine = FindFirstObjectByType<SessionStateMachine>();

        if (_sessionStateMachine == null)
            return;

        SessionPhase phase = _sessionStateMachine.CurrentPhase;
        if (phase == SessionPhase.Playing)
        {
            if (!_progressResetForPlayingMatch)
            {
                _reachedCornersMask = 0;
                _progressResetForPlayingMatch = true;
            }
        }
        else
        {
            _reachedCornersMask = 0;
            _progressResetForPlayingMatch = false;
            if (IsOwner)
                _verticalSpeed = -2f;
            return;
        }

        if (!IsOwner)
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

    [Rpc(SendTo.Server)]
    public void ReportCornerServerRpc(int cornerIndex, RpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;

        if (_sessionStateMachine == null)
            _sessionStateMachine = FindFirstObjectByType<SessionStateMachine>();

        if (_sessionStateMachine == null || _sessionStateMachine.CurrentPhase != SessionPhase.Playing)
            return;

        if (_startingCorner < 0)
        {
            SpawnManager spawnManager = FindFirstObjectByType<SpawnManager>();
            if (spawnManager != null)
                _startingCorner = spawnManager.GetSpawnIndex(_spawnPosition.Value);
        }

        if (cornerIndex < 0 || cornerIndex >= 4)
            return;

        if (cornerIndex == _startingCorner)
        {
            if (IsRequiredCornerMaskComplete())
                _sessionStateMachine.RequestGameOver(OwnerClientId);
            return;
        }

        _reachedCornersMask |= 1 << cornerIndex;
    }

    bool IsRequiredCornerMaskComplete()
    {
        for (int cornerIndex = 0; cornerIndex < 4; cornerIndex++)
        {
            if (cornerIndex == _startingCorner)
                continue;

            if ((_reachedCornersMask & (1 << cornerIndex)) == 0)
                return false;
        }

        return true;
    }
}
