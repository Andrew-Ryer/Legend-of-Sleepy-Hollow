using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    //TODO: Take 4 Transforms in to save as player spawnpoints
    public Transform spawnpoint1;
    public Transform spawnpoint2;
    public Transform spawnpoint3;
    public Transform spawnpoint4;

    //TODO: make an enum that is 1-4 for player number. Host should always be 1 and clients 2-4.
    public enum PlayerNumber
    {
        Host = 1,
        Client2 = 2,
        Client3 = 3,
        Client4 = 4,
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() { }

    // Update is called once per frame
    void Update() { }

    public Vector3 GetSpawnPosition(ulong clientId)
    {
        Transform[] spawnPoints =
        {
            spawnpoint1, // Host
            spawnpoint2, // Player 2
            spawnpoint3, // Player 3
            spawnpoint4, // Player 4
        };

        int spawnIndex = clientId == 0 ? 0 : Mathf.Clamp((int)clientId, 1, 3);

        return spawnPoints[spawnIndex].position;
    }
}
