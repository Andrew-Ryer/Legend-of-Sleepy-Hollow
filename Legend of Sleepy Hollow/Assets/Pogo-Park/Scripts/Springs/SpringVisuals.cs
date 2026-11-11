using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Simple script to visualize the compression force and release force of a player's spring
// It's been tweaked so that it searches through the players that exist and gives them the arrow gizmos
public class SpringVisuals : MonoBehaviour
{
    [SerializeField] bool _showVisualizations = true;
    [SerializeField] float _compressionArrowScale = 0.5f;
    [SerializeField] float _releaseArrowScale = 0.2f;
    [SerializeField, Min(0.01f)] float _releaseDecayRate = 1.5f;
    [SerializeField, Min(0.1f)] float _arrowThickness = 3f;
    [SerializeField] Color _compressionColor = Color.red;
    [SerializeField] Color _releaseColor = Color.green;

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !_showVisualizations)
            return;

        SpringPlayerMovementInput[] players = FindObjectsByType<SpringPlayerMovementInput>(FindObjectsSortMode.None);

        foreach (SpringPlayerMovementInput player in players)
        {
            if (player == null || !player.IsSpawned || !player.IsServer)
                continue;

            Vector3 origin = player.SpringOriginCollider != null
                ? player.SpringOriginCollider.bounds.center
                : player.transform.position;

            if (player.IsCharging)
            {
                Vector3 compression = -player.transform.up * (player.CompressionMeters * _compressionArrowScale);
                DrawVector(origin, compression, _compressionColor);
            }
            else if (player.LastLaunchSpeed > 0f)
            {
                float elapsed = Mathf.Max(0f, Time.time - player.LastLaunchTime);
                float currentSpeed = player.LastLaunchSpeed * Mathf.Exp(-_releaseDecayRate * elapsed);
                Vector3 release = player.LastLaunchDirection * (currentSpeed * _releaseArrowScale);
                DrawVector(origin, release, _releaseColor);
            }
        }
    }

    void DrawVector(Vector3 origin, Vector3 vector, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawRay(origin, vector);

#if UNITY_EDITOR
        Handles.color = color;
        Handles.DrawLine(origin, origin + vector, _arrowThickness);
#endif
    }
}
