using Unity.Netcode;
using UnityEngine;
using System;

public class SkyboundTimer : NetworkBehaviour
{
    public static SkyboundTimer Instance { get; private set; }

    public static event Action<int> OnTimerEnded;

    [SerializeField] private float timerSeconds = 120f;
    [SerializeField] private float firstFinishTimer = 35f;

    public NetworkVariable<float> CurrentTimer { get; } = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> RaceStarted { get; } = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> RaceEnded { get; } = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base.OnDestroy();
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned) return;
        if (RaceEnded.Value || !RaceStarted.Value) return;

        float nextTime = CurrentTimer.Value - Time.deltaTime;
        if (nextTime <= 0f)
        {
            CurrentTimer.Value = 0f;
            TimeOut();
        }
        else
        {
            CurrentTimer.Value = nextTime;
        }
        Debug.Log(CurrentTimer.Value);
    }

    public void StartRaceServer()
    {
        if (!IsServer || !IsSpawned)
        {
            Debug.LogWarning($"StartRaceServer ignored. IsServer={IsServer}, IsSpawned={IsSpawned}");
            return;
        }

        CurrentTimer.Value = timerSeconds;
        RaceStarted.Value = true;
        RaceEnded.Value = false;
    }

    public void firstFinish()
    {
        if (!IsServer || !IsSpawned || RaceEnded.Value) return;

        CurrentTimer.Value = firstFinishTimer;
        Debug.Log($"Server: First player finished! Grace period timer set to {firstFinishTimer} seconds.");
    }

    private void TimeOut()
    {
        if (!IsServer || RaceEnded.Value) return;

        RaceEnded.Value = true;
        RaceStarted.Value = false;
        OnTimerEnded?.Invoke(1);
        Debug.Log("Out of Time");
    }

    public void StopTimer()
    {
        if (!IsServer || !IsSpawned) return;
        RaceEnded.Value = true;
        RaceStarted.Value = false;
        OnTimerEnded?.Invoke(0);
        Debug.Log("All Players Finished");
    }
}