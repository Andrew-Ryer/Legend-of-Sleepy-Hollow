using UnityEngine;

public class ForestObstacleSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject [] forestObstacles;

    [Header("SpawnerSpawnPoints")]
    [SerializeField] private Transform [] spawnPoints;

    private void Start()
    {
        SpawnObstacles();
    }

    private void SpawnObstacles()
    {
        foreach (Transform point in spawnPoints)
        {
            int randomObstacle = Random.Range(0, forestObstacles.Length);

            Instantiate(forestObstacles[randomObstacle], point.position, Quaternion.identity);
        }
    }
}