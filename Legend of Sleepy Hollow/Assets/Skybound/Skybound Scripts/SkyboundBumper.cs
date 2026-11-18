using UnityEngine;

public class SkyboundBumper : MonoBehaviour
{
    [SerializeField] float bounceForce = 20f;
    [SerializeField] float verticalMultiplier = .15f;
    [SerializeField] float horizontalMultiplier = 2.5f;

    private void OnTriggerEnter(Collider other)
    {
        SkyboundPlayerController player = other.GetComponent<SkyboundPlayerController>();
        
        if (player != null)
        {
            Vector3 pushDir = other.transform.position - transform.position;
            pushDir.y = 0f;
            pushDir.Normalize();

            player.Bounce(bounceForce, pushDir, verticalMultiplier, horizontalMultiplier);
        }
    }
}