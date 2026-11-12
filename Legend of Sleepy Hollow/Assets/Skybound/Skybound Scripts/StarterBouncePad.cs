using UnityEngine;

public class StarterBouncePad : MonoBehaviour
{
    [SerializeField] float bounceForce = 10f;

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 currentVelocity = rb.linearVelocity;
            currentVelocity.y = 0f;
            rb.linearVelocity = currentVelocity;
            rb.AddForce(Vector3.up * bounceForce, ForceMode.Impulse);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        SkyboundPlayerController player = other.GetComponent<SkyboundPlayerController>();
        if (player != null)
        {
            player.Bounce( bounceForce );
        }
    }
}