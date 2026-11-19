using UnityEngine;

public class WagonHubInfo_SS : MonoBehaviour
{
    // public Transform wheelVisual;
    [SerializeField] float wheelRadius = 0.4f;
    public float WheelRadius => wheelRadius;
    public Vector3 SuspensionForce { get; set; }
    public Vector3 GripForce { get; set; }
    public Vector3 DriveForce { get; set; }
}

