using UnityEngine;

public class SpawnPoints : MonoBehaviour
{
    [SerializeField] Transform[] _points;

    public Vector3 GetRandomSpawnPosition()
    {
        if (_points == null || _points.Length == 0) return transform.position;
        
        int randomIndex = Random.Range(0, _points.Length);
        return _points[randomIndex].position;
    }
}       