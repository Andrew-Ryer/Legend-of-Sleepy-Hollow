
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Owner input and server-authoritative turning / spring launches for SpringPlayer.
public class SpringPlayerMovementInput : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] Rigidbody _body;

    [Header("Turning")]
    [SerializeField, Min(0f)] float _turnSpeedDegreesPerSecond = 180f;

    [Header("Spring charge")]
    [SerializeField] FloatSpring _springSettings = new FloatSpring();
    [SerializeField, Min(0.05f)] float _secondsToFullCharge = 1f;
    [SerializeField, Min(0f)] float _maximumCompressionMeters = 0.8f;
    [SerializeField, Min(0f)] float _minimumChargeSeconds = 0.05f;

    // Server writes this; all clients can read it later to animate a spring visual.
    readonly NetworkVariable<float> _charge01 = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    float _serverTurnInput;
    bool _serverSpringHeld;
    bool _wasSpringHeld;
    bool _isGrounded;
    float _chargeSeconds;

    // Normalized charge, replicated so a spring visual can use it.
    public float Charge01 => _charge01.Value;

    void Awake()
    {
        if (_body == null)
            _body = GetComponent<Rigidbody>();

        if (_body == null)
        {
            Debug.LogError("SpringPlayerMovementInput requires a Rigidbody on the prefab root.", this);
            enabled = false;
            return;
        }

        // Keep the capsule upright while allowing A/D yaw rotation.
        _body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        if (!IsSpawned || !IsOwner || Keyboard.current == null)
            return;

        float turn = 0f;
        if (Keyboard.current.aKey.isPressed) turn -= 1f;
        if (Keyboard.current.dKey.isPressed) turn += 1f;

        bool springHeld = Keyboard.current.spaceKey.isPressed;

        // The host already runs the authoritative simulation locally.
        if (IsServer)
            ReceiveInput(turn, springHeld);
        else
            SubmitInputRpc(turn, springHeld);
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
    void SubmitInputRpc(float turn, bool springHeld)
    {
        ReceiveInput(turn, springHeld);
    }

    void ReceiveInput(float turn, bool springHeld)
    {
        _serverTurnInput = Mathf.Clamp(turn, -1f, 1f);
        _serverSpringHeld = springHeld;
    }

    void FixedUpdate()
    {
        if (!IsSpawned || !IsServer || _body == null)
            return;

        float turnDelta = _serverTurnInput * _turnSpeedDegreesPerSecond * Time.fixedDeltaTime;
        _body.MoveRotation(_body.rotation * Quaternion.Euler(0f, turnDelta, 0f));

        if (_serverSpringHeld && _isGrounded)
        {
            _chargeSeconds = Mathf.Min(_chargeSeconds + Time.fixedDeltaTime, _secondsToFullCharge);
            _charge01.Value = Mathf.Clamp01(_chargeSeconds / _secondsToFullCharge);
        }

        // A release only launches if the player is still supported by the ground.
        if (_wasSpringHeld && !_serverSpringHeld)
        {
            if (_isGrounded && _chargeSeconds >= _minimumChargeSeconds)
                LaunchFromSpring();

            ResetCharge();
        }

        _wasSpringHeld = _serverSpringHeld;
    }

    void LaunchFromSpring()
    {
        if (_maximumCompressionMeters <= 0f || _springSettings.frequencyHz <= 0f)
            return;

        float compression = _maximumCompressionMeters * Mathf.Clamp01(_charge01.Value);
        if (compression <= 0f)
            return;

        // Start a fresh spring at the compressed position and let its configured
        // FloatSpring settings determine the upward release velocity.
        FloatSpring releaseSpring = new FloatSpring
        {
            equilibrium = _springSettings.equilibrium,
            frequencyHz = _springSettings.frequencyHz,
            dampingRatio = _springSettings.dampingRatio
        };
        releaseSpring.SetValue(releaseSpring.equilibrium - compression);

        const float springStep = 1f / 120f;
        float launchSpeed = 0f;
        for (int i = 0; i < 720; i++)
        {
            releaseSpring.Advance(springStep);
            if (releaseSpring.Value >= releaseSpring.equilibrium)
            {
                launchSpeed = Mathf.Max(0f, releaseSpring.Velocity);
                break;
            }
        }

        if (launchSpeed > 0f)
        {
            Vector3 velocity = _body.linearVelocity;
            velocity.y = launchSpeed;
            _body.linearVelocity = velocity;
            _isGrounded = false;
        }
    }

    void ResetCharge()
    {
        _chargeSeconds = 0f;
        _charge01.Value = 0f;
    }

    void OnCollisionEnter(Collision collision) => UpdateGroundedState(collision);
    void OnCollisionStay(Collision collision) => UpdateGroundedState(collision);

    void OnCollisionExit(Collision collision)
    {
        if (IsServer)
            _isGrounded = false;
    }

    void UpdateGroundedState(Collision collision)
    {
        if (!IsServer || !IsSpawned)
            return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            // Only surfaces below the player count as support; a side wall does not.
            if (Vector3.Dot(collision.GetContact(i).normal, Vector3.up) > 0.5f)
            {
                _isGrounded = true;
                return;
            }
        }
    }
}



