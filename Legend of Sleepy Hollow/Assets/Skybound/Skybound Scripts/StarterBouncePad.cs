using UnityEngine;
public class StarterBouncePad : MonoBehaviour
{
    [SerializeField] float bounceForce = 12f;
    [SerializeField] float verticalMultiplier = 1.2f;
    [SerializeField] float horizontalMultiplier = 2.0f;

    private void OnTriggerEnter(Collider other)
    {
        SkyboundPlayerController player = other.GetComponent<SkyboundPlayerController>();
        
        if (player != null)
        {
            // Pass the force, direction, and specific multipliers into the player
            player.Bounce(bounceForce, transform.up, verticalMultiplier, horizontalMultiplier);
        }
    }
}