using Unity.Netcode;
using UnityEngine;

// Script to make sure that the player does a full 360 spin (springOrigin goes around player 360 degrees fully) to then give them a jump boost
[RequireComponent(typeof(SpringPlayerMovementInput))]
public class SpringPlayerCheckForBoost : NetworkBehaviour
{
    [SerializeField] Transform _springOrigin;
    [SerializeField, Min(1f)] float _degreesForBoost = 300f; // 300 is a good default value
    [SerializeField, Min(1f)] float _boostMultiplier = 1.25f;

    SpringPlayerMovementInput _movement;
    int _lastLaunchSequence;
    bool _trackingSpin;
    float _lastOriginAngle;
    float _spinDegrees;

    void Awake()
    {
        _movement = GetComponent<SpringPlayerMovementInput>();
        if (_springOrigin == null)
        {
            Transform origin = transform.Find("SpringOrigin");
            if (origin != null)
                _springOrigin = origin;
        }
    }

    public override void OnNetworkSpawn()
    {
        _lastLaunchSequence = _movement.LaunchSequence;
    }

    void FixedUpdate()
    {
        if (!IsSpawned || !IsServer || _movement == null || _springOrigin == null)
            return;

        if (_movement.LaunchSequence != _lastLaunchSequence)
        {
            _lastLaunchSequence = _movement.LaunchSequence;
            _trackingSpin = true;
            _spinDegrees = 0f;
            _lastOriginAngle = _springOrigin.eulerAngles.z;
        }

        if (!_trackingSpin)
            return;

        // The movement check includes the brief pause after launch so lingering launch contact is ignored like how it is for standard jumping
        if (_movement.IsSpringOriginGrounded)
        {
            _trackingSpin = false;
            _spinDegrees = 0f;
            return;
        }

        float currentAngle = _springOrigin.eulerAngles.z;
        _spinDegrees += Mathf.DeltaAngle(_lastOriginAngle, currentAngle);
        _lastOriginAngle = currentAngle;

        if (Mathf.Abs(_spinDegrees) >= _degreesForBoost)
        {
            bool boostWasReady = _movement.HasNextBounceBoost;
            _movement.GrantNextBounceBoost(_boostMultiplier);
            if (!boostWasReady && _movement.HasNextBounceBoost)
                Debug.Log("Player did full spin, jump boost granted", this);

            _trackingSpin = false;
            _spinDegrees = 0f;
        }
    }
}
