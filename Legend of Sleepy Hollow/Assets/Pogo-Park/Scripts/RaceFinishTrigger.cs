using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RaceFinishTrigger : MonoBehaviour
{
    [SerializeField] RaceProgressTracker _raceProgressTracker;
    Collider _triggerCollider;

    void Awake()
    {
        _triggerCollider = GetComponent<Collider>();

        if (_triggerCollider == null || !_triggerCollider.isTrigger)
        {
            Debug.LogError("RaceFinishTrigger needs a collider with Is Trigger enabled.", this);
            enabled = false;
            return;
        }

        if (_raceProgressTracker == null)
            _raceProgressTracker = FindFirstObjectByType<RaceProgressTracker>();
    }

    void OnTriggerEnter(Collider other) => CheckForFinishedPlayer(other);
    void OnTriggerStay(Collider other) => CheckForFinishedPlayer(other);

    void CheckForFinishedPlayer(Collider other)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer || _raceProgressTracker == null)
            return;

        SpringPlayerMovementInput player = other.GetComponentInParent<SpringPlayerMovementInput>();
        if (player != null && IsPlayerFullyInside(player))
            _raceProgressTracker.RegisterPlayerFinished(player);
    }

    bool IsPlayerFullyInside(SpringPlayerMovementInput player)
    {
        Collider[] playerColliders = player.GetComponentsInChildren<Collider>();
        bool foundCollider = false;

        foreach (Collider playerCollider in playerColliders)
        {
            if (playerCollider == null || !playerCollider.enabled || playerCollider.isTrigger)
                continue;

            foundCollider = true;
            Bounds playerBounds = playerCollider.bounds;
            if (!IsBoundsInsideTrigger(playerBounds))
                return false;
        }

        return foundCollider;
    }

    bool IsBoundsInsideTrigger(Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z)
        };

        foreach (Vector3 corner in corners)
        {
            Vector3 closestPoint = _triggerCollider.ClosestPoint(corner);
            if ((closestPoint - corner).sqrMagnitude > 0.0001f)
                return false;
        }

        return true;
    }
}
