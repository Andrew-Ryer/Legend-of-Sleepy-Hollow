using Unity.Netcode;
using UnityEngine;

public class SkyboundSpinnerBar : NetworkBehaviour
{
    [SerializeField] bool spinRight = true;
    [SerializeField] float spinSpeed = 90f;
    [SerializeField] float bounceForce = 10f;
    [SerializeField] float verticalMultiplier = 0.25f;
    [SerializeField] float horizontalMultiplier = 5f;

    void FixedUpdate()
    {
        if (!IsServer) return;

        float direction = spinRight ? -1f : -1f;
        transform.Rotate(Vector3.right * spinSpeed * direction * Time.fixedDeltaTime);
    }

    void OnTriggerEnter(Collider other)
    {        
        SkyboundPlayerController player = other.GetComponent<SkyboundPlayerController>();

        if (player != null)
        {
            Vector3 pushDir = (other.transform.position - transform.position);
            pushDir.y = 0f;
            pushDir.Normalize();
            player.Bounce(bounceForce, pushDir, verticalMultiplier, horizontalMultiplier);
        }
    }
}