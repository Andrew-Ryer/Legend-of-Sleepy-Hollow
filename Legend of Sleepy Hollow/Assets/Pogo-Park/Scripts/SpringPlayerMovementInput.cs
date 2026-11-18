
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Owner input with server-authoritative tilt and spring launches
public class SpringPlayerMovementInput : NetworkBehaviour
{
    [SerializeField] Rigidbody _body;
    [SerializeField] Collider _springOriginCollider;

    [Header("Tilt")]
    float _turnSpeedDegreesPerSecond = 180;
    float _maximumTiltDegrees = 60f;
    float _uprightRecoverySpeedDegreesPerSecond = 180f;
    float _uprightRecoveryDistance = 2f;

    [Header("Air Gravity")] // Preferably do not change these for now, they are gravity
    [SerializeField, Min(1f)] float _risingGravityMultiplier = 1.15f; // Don't go below 1
    [SerializeField, Min(1f)] float _fallingGravityMultiplier = 2.0f; // Don't go below 1

    [Header("Spring")]
    float _postLaunchGroundPause = 0.5f; // Delay before it starts trying to fix the player upright
    [SerializeField] FloatSpring _springSettings = new FloatSpring();
    [SerializeField, Min(0.05f)] float _secondsToFullCharge = 0.6f; // 0.6 is a good starting point;
    [SerializeField, Min(0f)] float _maximumCompressionMeters = 3.0f; // 3.0 is a good starting point
    [SerializeField] float _minimumChargeSeconds = 0.05f;
    float _minimumGroundNormalDot = 0.5f;

    float _turnInput;
    bool _spaceHeld;
    bool _spaceWasHeld;
    float _tilt;
    float _chargeTime;
    float _lastSupportTime = float.NegativeInfinity;
    float _groundResponsePausedUntil;
    float _lastLaunchSpeed;
    float _lastLaunchTime;
    Vector3 _lastLaunchDirection;

    public Rigidbody Body => _body;
    public Collider SpringOriginCollider => _springOriginCollider;
    public bool IsSpringOriginGrounded => _springOriginCollider != null && !_springOriginCollider.isTrigger && SpringOriginGrounded;
    public bool IsCharging => _spaceHeld && IsSpringOriginGrounded && _chargeTime > 0f;
    public float Charge01 => Mathf.Clamp01(_chargeTime / Mathf.Max(0.05f, _secondsToFullCharge));
    public float CompressionMeters => _maximumCompressionMeters * Charge01;
    public float LastLaunchSpeed => _lastLaunchSpeed;
    public float LastLaunchTime => _lastLaunchTime;
    public Vector3 LastLaunchDirection => _lastLaunchDirection;

    bool SpringOriginGrounded => Time.fixedTime >= _groundResponsePausedUntil &&
        Time.fixedTime - _lastSupportTime <= Time.fixedDeltaTime * 2.5f;

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

        bool grounded = _springOriginCollider != null && !_springOriginCollider.isTrigger && SpringOriginGrounded;
        float normalizedTilt = Mathf.DeltaAngle(0f, _tilt);
        bool outsideSpringTilt = Mathf.Abs(normalizedTilt) > _maximumTiltDegrees;
        bool groundResponsePaused = Time.fixedTime < _groundResponsePausedUntil;
        bool recovering = !groundResponsePaused && outsideSpringTilt && (grounded || IsNearGround());

        if (!grounded)
        {
            float gravityMultiplier = _body.linearVelocity.y > 0f
                ? _risingGravityMultiplier
                : _fallingGravityMultiplier;
            float extraGravityMultiplier = _body.useGravity ? gravityMultiplier - 1f : gravityMultiplier;
            _body.AddForce(Physics.gravity * extraGravityMultiplier, ForceMode.Acceleration);
        }

        if (recovering)
        {
            float targetTilt = Mathf.Clamp(normalizedTilt, -_maximumTiltDegrees, _maximumTiltDegrees);
            _tilt = Mathf.MoveTowardsAngle(_tilt, targetTilt, _uprightRecoverySpeedDegreesPerSecond * Time.fixedDeltaTime);
        }
        else
        {
            _tilt += _turnInput * _turnSpeedDegreesPerSecond * Time.fixedDeltaTime;
            if (grounded)
                _tilt = Mathf.Clamp(Mathf.DeltaAngle(0f, _tilt), -_maximumTiltDegrees, _maximumTiltDegrees);
            else
                _tilt = Mathf.Repeat(_tilt + 180f, 360f) - 180f;
        }

        _body.MoveRotation(Quaternion.Euler(0f, 0f, _tilt));

        if (grounded)
        {
            _body.linearVelocity = new Vector3(0f, _body.linearVelocity.y, _body.linearVelocity.z);
            _body.angularVelocity = Vector3.zero;
        }

        bool canSpring = grounded && Mathf.Abs(Mathf.DeltaAngle(0f, _tilt)) <= _maximumTiltDegrees + 0.1f;

        if (canSpring && _spaceHeld)
        {
            _chargeTime = Mathf.Min(
                _chargeTime + Time.fixedDeltaTime,
                Mathf.Max(0.05f, _secondsToFullCharge));
        }
        else if (!canSpring)
        {
            // Charge only accumulates while the dedicated support collider is actually in contact with a floor-like surface
            _chargeTime = 0f;
        }

        if (_spaceWasHeld && !_spaceHeld)
        {
            _lastLaunchSpeed = 0f;
            _lastLaunchDirection = Vector3.zero;
            if (canSpring && _chargeTime >= _minimumChargeSeconds)
                Launch();

            _chargeTime = 0f;
        }

        _spaceWasHeld = _spaceHeld;
    }

    bool IsNearGround()
    {
        RaycastHit[] hits = Physics.RaycastAll(
            _body.worldCenterOfMass,
            Vector3.down,
            _uprightRecoveryDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (hit.rigidbody == _body || Vector3.Dot(hit.normal, Vector3.up) < _minimumGroundNormalDot)
                continue;

            return true;
        }

        return false;
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
            _lastLaunchTime = Time.time;
            _body.linearVelocity = _lastLaunchDirection * launchSpeed;
            _groundResponsePausedUntil = Time.fixedTime + _postLaunchGroundPause;
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



