
using UnityEngine;

// Simple script to visualize the compression force and release force of a player's spring
public class SpringVisuals : MonoBehaviour
{
    [SerializeField] bool _showVisualizations = true;
    [SerializeField] float _compressionArrowScale = 0.5f;
    [SerializeField] float _releaseArrowScale = 0.2f;
    [SerializeField] float _maximumArrowLength = 4f;
    [SerializeField] float _arrowHeadLength = 0.25f;
    [SerializeField] Color _compressionColor = Color.red;
    [SerializeField] Color _releaseColor = Color.green;

    static readonly SpringPlayerMovementInput[] EmptyPlayers = new SpringPlayerMovementInput[0];

    void OnDrawGizmos()
    {
        if (!_showVisualizations)
            return;

        SpringPlayerMovementInput[] players = FindObjectsByType<SpringPlayerMovementInput>(FindObjectsSortMode.None);

        if (players == null || players.Length == 0)
            players = EmptyPlayers;

        foreach (SpringPlayerMovementInput player in players)
        {
            if (player == null || !player.IsSpawned || !player.IsServer)
                continue;

            Vector3 origin = player.SpringOriginCollider != null ? player.SpringOriginCollider.bounds.center : player.transform.position;

            if (player.IsCharging)
            {
                Vector3 compression = LimitVector(-player.transform.up * (player.CompressionMeters * _compressionArrowScale));
                DrawArrow(origin, compression, _compressionColor);
            }
            else if (player.LastLaunchSpeed > 0f)
            {
                Vector3 release = LimitVector(player.LastLaunchDirection * (player.LastLaunchSpeed * _releaseArrowScale));
                DrawArrow(origin, release, _releaseColor);
            }
        }
    }

    Vector3 LimitVector(Vector3 vector)
    {
        return vector.magnitude > _maximumArrowLength ? vector.normalized * _maximumArrowLength : vector;
    }

    void DrawArrow(Vector3 start, Vector3 vector, Color color)
    {
        if (vector.sqrMagnitude < 0.0001f)
            return;

        Gizmos.color = color;
        Vector3 end = start + vector;
        Gizmos.DrawLine(start, end);

        Vector3 direction = vector.normalized;
        Vector3 side = Vector3.Cross(direction, Vector3.forward).normalized;
        if (side.sqrMagnitude < 0.0001f)
            side = Vector3.Cross(direction, Vector3.up).normalized;

        float headLength = Mathf.Min(_arrowHeadLength, vector.magnitude * 0.3f);
        Gizmos.DrawLine(end, end - direction * headLength + side * headLength * 0.5f);
        Gizmos.DrawLine(end, end - direction * headLength - side * headLength * 0.5f);
    }
}



