using Unity.Netcode;
using UnityEngine;
using System;

public class SkyboundGamemanager : NetworkBehaviour
{
    public static SkyboundGamemanager Instance { get; private set; }

    [SerializeField] SpawnPoints _spawnPointsHandler; 
    [SerializeField] SkyboundTimer SkyboundTimer;
    public NetworkVariable<bool> IsGameStarted = new NetworkVariable<bool>(false);

    public static event Action OnGameStarted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SkyboundTimer.OnTimerEnded += EndGame;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            SkyboundTimer.OnTimerEnded -= EndGame;
        }
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
            OnGameStarted?.Invoke();
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

    public void EndGame(int condition)
    {
        if (!IsServer) return;
        
        IsGameStarted.Value = false;

    }
}