using UnityEngine;

// Temporary script that will eventually trigger points when touched (only glows for now)
[RequireComponent(typeof(Renderer))]
public class PointsEventScript : MonoBehaviour
{
    public GameObject triggerObject; // Object to detect collision with

    public Color defaultGlowColor = Color.red;
    public Color triggeredGlowColor = Color.green;

    private Renderer cubeRenderer;
    private Material cubeMaterial;

    void Start()
    {
        // Get the cube's renderer and material
        cubeRenderer = GetComponent<Renderer>();
        cubeMaterial = cubeRenderer.material;

        // Enable emission keyword so glow works
        cubeMaterial.EnableKeyword("_EMISSION");

        // Set initial glow color
        SetGlow(defaultGlowColor);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Validate triggerObject before comparing via tag
        if (collision.gameObject.CompareTag("SpringPlayer"))
        {
            SetGlow(triggeredGlowColor);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // Reset glow when trigger object stops touching
        if (triggerObject != null && collision.gameObject == triggerObject)
        {
            SetGlow(defaultGlowColor);
        }
    }

    // Sets the glow
    private void SetGlow(Color glowColor)
    {
        cubeMaterial.SetColor("_EmissionColor", glowColor);
    }
}


