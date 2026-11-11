
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Owner input with server-authoritative tilt and spring launches.
public class SpringPlayerMovementInput : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] Rigidbody _body;
    [SerializeField] Collider _springOriginCollider;

    [Header("Tilt")]
    [SerializeField, Min(0f)] float _turnSpeedDegreesPerSecond = 90f;
    [SerializeField, Range(0f, 89f)] float _maximumTiltDegrees = 60f;

    [Header("Spring charge")]
    [SerializeField] FloatSpring _springSettings = new FloatSpring();

    [SerializeField, Min(0.05f)] private float _secondsToFullCharge = 0.6f; // 0.6 is a good starting point;
    [SerializeField, Min(0f)] float _maximumCompressionMeters = 3.0f; // 3.0 is a good starting point
    [SerializeField, Min(0f)] float _minimumChargeSeconds = 0.05f;
    [SerializeField, Range(0f, 1f)] float _minimumGroundNormalDot = 0.5f;

    // Written by the server and available for spring visuals on every client.
    readonly NetworkVariable<float> _charge01 = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    float _serverTurnInput;
    bool _serverSpringHeld;
    bool _wasSpringHeld;
    float _chargeSeconds;
    float _tiltDegrees;
    float _lastSpringOriginGroundContactTime = float.NegativeInfinity;

    // Normalized spring charge, replicated for use by visuals.
    public float Charge01 => _charge01.Value;

    bool IsSpringOriginGrounded =>
        Time.fixedTime - _lastSpringOriginGroundContactTime <= Time.fixedDeltaTime * 2.5f;

    void Awake()
    {
        if (_body == null)
            _body = GetComponent<Rigidbody>();

        if (_springOriginCollider == null)
        {
            Transform springOrigin = transform.Find("SpringOrigin");
            if (springOrigin != null)
                _springOriginCollider = springOrigin.GetComponent<Collider>();
        }

        if (_body == null)
        {
            Debug.LogError("SpringPlayerMovementInput requires a Rigidbody on the prefab root.", this);
            enabled = false;
            return;
        }

        if (_springOriginCollider == null)
        {
            Debug.LogWarning(
                "Add a child named SpringOrigin with a non-trigger collider, then assign it to Spring Origin Collider. " +
                "Spring charging will stay disabled until that collider is configured.", this);
        }
        else if (_springOriginCollider.isTrigger)
        {
            Debug.LogWarning("SpringOrigin must use a non-trigger collider for this contact-based ground check.", this);
        }

        // This is a side-view movement plane: preserve only world X/Y movement and allow the player to tilt around world Z.
        _body.constraints &= ~RigidbodyConstraints.FreezeRotationZ;
        _body.constraints |= RigidbodyConstraints.FreezePositionZ
                           | RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationY;
        _body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        if (!IsSpawned || !IsOwner)
            return;

        Keyboard keyboard = Keyboard.current;
        float turn = 0f;
        bool springHeld = false;

        if (keyboard != null)
        {
            // D tilts the top of the player to the right, making transform.up point rightward for the launch. A tilts the top to the left.
            if (keyboard.aKey.isPressed) turn += 1f;
            if (keyboard.dKey.isPressed) turn -= 1f;
            springHeld = keyboard.spaceKey.isPressed;
        }

        // The host is already the server, so it can apply its own input directly.
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

        float deltaTime = Time.fixedDeltaTime;
        _tiltDegrees = Mathf.Clamp(_tiltDegrees + _serverTurnInput * _turnSpeedDegreesPerSecond * deltaTime, -_maximumTiltDegrees, _maximumTiltDegrees);
        _body.MoveRotation(Quaternion.Euler(0f, 0f, _tiltDegrees));

        bool groundedAtSpringOrigin = _springOriginCollider != null && !_springOriginCollider.isTrigger && IsSpringOriginGrounded;

        if (groundedAtSpringOrigin && _serverSpringHeld)
        {
            float fullChargeTime = Mathf.Max(0.05f, _secondsToFullCharge);
            _chargeSeconds = Mathf.Min(_chargeSeconds + deltaTime, fullChargeTime);
            _charge01.Value = Mathf.Clamp01(_chargeSeconds / fullChargeTime);
        }
        else if (!groundedAtSpringOrigin)
        {
            // Charge only accumulates while the dedicated support collider is actually in contact with a floor-like surface.
            ResetCharge();
        }

        if (_wasSpringHeld && !_serverSpringHeld)
        {
            if (groundedAtSpringOrigin && _chargeSeconds >= _minimumChargeSeconds)
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

        // Compute the release velocity from the existing FloatSpring settings.
        // This uses its current values; it does not modify/tune those settings.
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

        if (launchSpeed <= 0f)
            return;

        // The player's Z tilt steers this launch left or right while retaining
        // vertical lift. Z velocity is prevented by the Rigidbody constraint.
        _body.linearVelocity = _body.transform.up * launchSpeed;
        _lastSpringOriginGroundContactTime = float.NegativeInfinity;
    }

    void ResetCharge()
    {
        _chargeSeconds = 0f;
        if (_charge01.Value != 0f)
            _charge01.Value = 0f;
    }

    void OnCollisionEnter(Collision collision) => RecordSpringOriginSupport(collision);
    void OnCollisionStay(Collision collision) => RecordSpringOriginSupport(collision);

    void RecordSpringOriginSupport(Collision collision)
    {
        if (!IsServer || !IsSpawned || _springOriginCollider == null || _springOriginCollider.isTrigger)
            return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            if (contact.thisCollider != _springOriginCollider)
                continue;

            // Reject walls and ceilings; only upward-facing support surfaces count.
            if (Vector3.Dot(contact.normal, Vector3.up) >= _minimumGroundNormalDot)
            {
                _lastSpringOriginGroundContactTime = Time.fixedTime;
                return;
            }
        }
    }
}



