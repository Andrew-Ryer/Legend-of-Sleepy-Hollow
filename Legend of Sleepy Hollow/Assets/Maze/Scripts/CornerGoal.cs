using UnityEngine;

public class CornerGoal : MonoBehaviour
{
    [SerializeField]
    int cornerIndex;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (!other.TryGetComponent(out MazePlayerController player))
            player = other.GetComponentInParent<MazePlayerController>();

        if (player != null && player.IsOwner)
            player.ReportCornerServerRpc(cornerIndex);
    }
}
