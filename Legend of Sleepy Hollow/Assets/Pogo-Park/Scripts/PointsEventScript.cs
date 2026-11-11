using UnityEngine;

// Temporary script that will eventually trigger points when touched (only glows for now)
[RequireComponent(typeof(Renderer))]
public class CubeGlowByTag : MonoBehaviour
{
    public string triggerTag = "SpringPlayer"; // Tag to detect

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
        // Check if the colliding object has the trigger tag
        if (collision.gameObject.CompareTag(triggerTag))
        {
            SetGlow(triggeredGlowColor);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // Reset glow when tagged object stops touching
        if (collision.gameObject.CompareTag(triggerTag))
        {
            SetGlow(defaultGlowColor);
        }
    }

    // Sets the cubee's glow
    void SetGlow(Color glowColor)
    {
        cubeMaterial.SetColor("_EmissionColor", glowColor);
    }
}


