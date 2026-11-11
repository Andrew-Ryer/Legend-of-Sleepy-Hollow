
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Owner input with server-authoritative tilt and spring launches
public class SpringPlayerMovementInput : NetworkBehaviour
{
    [SerializeField] Rigidbody _body;
    [SerializeField] Collider _springOriginCollider;

    [Header("Tilt")]
    [SerializeField] float _turnSpeedDegreesPerSecond = 90f;
    [SerializeField] float _maximumTiltDegrees = 60f;

    [Header("Spring")]
    [SerializeField] FloatSpring _springSettings = new FloatSpring();
    [SerializeField, Min(0.05f)] float _secondsToFullCharge = 0.6f; // 0.6 is a good starting point;
    [SerializeField, Min(0f)] float _maximumCompressionMeters = 3.0f; // 3.0 is a good starting point
    [SerializeField] float _minimumChargeSeconds = 0.05f;
    [SerializeField, Range(0f, 1f)] float _minimumGroundNormalDot = 0.5f;

    float _turnInput;
    bool _spaceHeld;
    bool _spaceWasHeld;
    float _tilt;
    float _chargeTime;
    float _lastSupportTime = float.NegativeInfinity;
    float _lastLaunchSpeed;
    Vector3 _lastLaunchDirection;

    public Rigidbody Body => _body;
    public Collider SpringOriginCollider => _springOriginCollider;
    public bool IsSpringOriginGrounded => _springOriginCollider != null && !_springOriginCollider.isTrigger && SpringOriginGrounded;
    public bool IsCharging => _spaceHeld && IsSpringOriginGrounded && _chargeTime > 0f;
    public float Charge01 => Mathf.Clamp01(_chargeTime / Mathf.Max(0.05f, _secondsToFullCharge));
    public float CompressionMeters => _maximumCompressionMeters * Charge01;
    public float LastLaunchSpeed => _lastLaunchSpeed;
    public Vector3 LastLaunchDirection => _lastLaunchDirection;

    bool SpringOriginGrounded => Time.fixedTime - _lastSupportTime <= Time.fixedDeltaTime * 2.5f;

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

        if (_springOriginCollider == null)
        {
            Transform origin = transform.Find("SpringOrigin");
            if (origin != null)
                _springOriginCollider = origin.GetComponent<Collider>();
        }

        // This is a side-view movement plane: preserve only world X/Y movement and allow the player to tilt around world Z
        _body.constraints &= ~RigidbodyConstraints.FreezeRotationZ;
        _body.constraints |= RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
    }

    void Update()
    {
        if (!IsSpawned || !IsOwner)
            return;

        Keyboard keys = Keyboard.current;
        float turn = 0f;
        bool spaceHeld = false;

        if (keys != null)
        {
            // D tilts the top of the player to the right, making transform.up point rightward for the launch. A tilts the top to the left
            if (keys.aKey.isPressed) turn += 1f;
            if (keys.dKey.isPressed) turn -= 1f;
            spaceHeld = keys.spaceKey.isPressed;
        }

        // The host is already the server, so it can apply its own input directly
        if (IsServer)
            SetInput(turn, spaceHeld);
        else
            SendInputRpc(turn, spaceHeld);
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
    void SendInputRpc(float turn, bool spaceHeld)
    {
        SetInput(turn, spaceHeld);
    }

    void SetInput(float turn, bool spaceHeld)
    {
        _turnInput = Mathf.Clamp(turn, -1f, 1f);
        _spaceHeld = spaceHeld;
    }

    void FixedUpdate()
    {
        if (!IsSpawned || !IsServer)
            return;

        _tilt = Mathf.Clamp(_tilt + _turnInput * _turnSpeedDegreesPerSecond * Time.fixedDeltaTime, -_maximumTiltDegrees, _maximumTiltDegrees);
        _body.MoveRotation(Quaternion.Euler(0f, 0f, _tilt));

        bool grounded = _springOriginCollider != null && !_springOriginCollider.isTrigger && SpringOriginGrounded;

        if (grounded && _spaceHeld)
        {
            _chargeTime = Mathf.Min(
                _chargeTime + Time.fixedDeltaTime,
                Mathf.Max(0.05f, _secondsToFullCharge));
        }
        else if (!grounded)
        {
            // Charge only accumulates while the dedicated support collider is actually in contact with a floor-like surface
            _chargeTime = 0f;
        }

        if (_spaceWasHeld && !_spaceHeld)
        {
            _lastLaunchSpeed = 0f;
            _lastLaunchDirection = Vector3.zero;
            if (grounded && _chargeTime >= _minimumChargeSeconds)
                Launch();

            _chargeTime = 0f;
        }

        _spaceWasHeld = _spaceHeld;
    }

    void Launch()
    {
        if (_springSettings.frequencyHz <= 0f || _maximumCompressionMeters <= 0f)
            return;

        float charge01 = Mathf.Clamp01(_chargeTime / Mathf.Max(0.05f, _secondsToFullCharge));
        float compression = _maximumCompressionMeters * charge01;

        // Compute the release velocity from the existing FloatSpring settings
        // This uses its current values; it does not modify/tune those settings
        FloatSpring spring = new FloatSpring
        {
            equilibrium = _springSettings.equilibrium,
            frequencyHz = _springSettings.frequencyHz,
            dampingRatio = _springSettings.dampingRatio
        };
        spring.SetValue(spring.equilibrium - compression);

        const float step = 1f / 120f;
        float launchSpeed = 0f;
        for (int i = 0; i < 720; i++)
        {
            spring.Advance(step);
            if (spring.Value >= spring.equilibrium)
            {
                launchSpeed = Mathf.Max(0f, spring.Velocity);
                break;
            }
        }

        _lastLaunchSpeed = launchSpeed;
        // The player's Z tilt steers this launch left or right while retaining vertical lift. Z velocity is prevented by the Rigidbody constraint
        if (launchSpeed > 0f)
        {
            _lastLaunchDirection = _body.transform.up;
            _body.linearVelocity = _lastLaunchDirection * launchSpeed;
        }

        _lastSupportTime = float.NegativeInfinity;
    }

    void OnCollisionEnter(Collision collision) => CheckSpringOriginContact(collision);
    void OnCollisionStay(Collision collision) => CheckSpringOriginContact(collision);

    void CheckSpringOriginContact(Collision collision)
    {
        if (!IsServer || !IsSpawned || _springOriginCollider == null)
            return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            // Reject walls and ceilings; only upward-facing support surfaces count
            if (contact.thisCollider == _springOriginCollider && Vector3.Dot(contact.normal, Vector3.up) >= _minimumGroundNormalDot)
            {
                _lastSupportTime = Time.fixedTime;
                return;
            }
        }
    }
}



