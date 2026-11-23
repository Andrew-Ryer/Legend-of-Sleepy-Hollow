using UnityEngine;
using UnityEngine.InputSystem;

public class WagonHubs_SS : MonoBehaviour
{
    [Header("Car")]
    [SerializeField] Rigidbody carRigidBody;
    [SerializeField] WagonHubInfo_SS[] hubDataList;

    [Header("Suspension")]
    [SerializeField] float suspensionRestDist = 1f;
    [SerializeField] float raycastLength = 1.5f;
    [SerializeField] float stiffness = 10f;
    [SerializeField] float damping = 10f;
    [SerializeField] LayerMask groundLayers = ~0;

    [Header("Grip")]
    [SerializeField] float gripStrength = 10f;
    public WagonHubInfo_SS[] Hubs => hubDataList;

    void Awake()
{
    if (carRigidBody == null)
    {
        carRigidBody = GetComponent<Rigidbody>();
    }

    if (carRigidBody != null)
    {
        carRigidBody.isKinematic = false;
    }
}

    void FixedUpdate()
    {
        foreach (WagonHubInfo_SS hub in hubDataList)
        {
            hub.SuspensionForce = hub.GripForce = Vector3.zero;  

            Ray groundRay = new Ray(hub.transform.position, -hub.transform.up);

            Debug.DrawRay(hub.transform.position, -hub.transform.up * raycastLength, Color.red);
            if (!Physics.Raycast(groundRay, out RaycastHit groundhit, raycastLength, groundLayers)) continue;

            Vector3 hubVelocity = carRigidBody.GetPointVelocity(hub.transform.position);

            float displacement = groundhit.distance - suspensionRestDist;
            float restoringForce = -stiffness * displacement;
            float suspensionSpeed = Vector3.Dot(hub.transform.up, hubVelocity);
            float dampingForce = -damping * suspensionSpeed;

            hub.SuspensionForce = (restoringForce + dampingForce) * hub.transform.up;
  
            // hub.wheelVisual.transform.position = groundhit.point + hub.WheelRadius * hub.transform.up;

            // float speedRatio = Mathf.Clamp01(Mathf.Abs(Vector3.Dot(carRigidBody.velocity, transform.forward)) / topSpeed);
            
            // float rollingSpeed = Vector3.Dot(hubVelocity, hub.transform.forward);
            // float rollingDistance = rollingSpeed * Time.fixedDeltaTime;
            // float wheelCircumference = hub.WheelRadius * 2f * Mathf.PI;
            // float wheelRevolution = rollingDistance / wheelCircumference;
            // float spinDegrees = wheelRevolution * 360f;
            // hub.wheelVisual.Rotate(spinDegrees, 0f, 0f, Space.Self);

            float slippingSpeed = Vector3.Dot(hubVelocity, hub.transform.right);
            float gripForce = -slippingSpeed * gripStrength;
            hub.GripForce = gripForce * hub.transform.right;

            Vector3 totalForce = hub.SuspensionForce + hub.GripForce;
            carRigidBody.AddForceAtPosition(totalForce, hub.transform.position);
            Debug.Log(Mathf.Floor(carRigidBody.linearVelocity.magnitude) + " mph");
        }
    }
}
