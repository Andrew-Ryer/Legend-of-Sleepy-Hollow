using Unity.Netcode;
using UnityEngine;

public class SkyboundGamemanager : NetworkBehaviour
{
    public static SkyboundGamemanager Instance { get; private set; }

    [SerializeField] SpawnPoints _spawnPointsHandler; 
    public NetworkVariable<bool> IsGameStarted = new NetworkVariable<bool>(false);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public Vector3 GetRandomSpawnPosition()
    {
        if (_spawnPointsHandler != null)
        {
            return _spawnPointsHandler.GetRandomSpawnPosition();
        }

        return Vector3.up;
    }

    public void StartGame()
    {
        if (IsServer)
        {
            IsGameStarted.Value = true;
            Debug.Log("Game Started! Players can now move.");

            if (SkyboundTimer.Instance != null)
            {
                SkyboundTimer.Instance.StartRaceServer();
            }
            else
            {
                Debug.LogWarning("SkyboundTimer instance not found!");
            }
        }
    }
}