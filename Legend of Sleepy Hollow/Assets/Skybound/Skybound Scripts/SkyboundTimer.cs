using UnityEngine;
using Unity.Netcode;
using System;
public class SkyboundTimer : NetworkBehaviour
{
    public static SkyboundTimer Instance { get; private set; }
    [SerializeField] private float timerSeconds = 120f;
    [SerializeField] private float firstFinishTimer = 35f;
    private float currentTimer;
    private bool raceStarted = false;
    private bool raceEnded = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!IsServer || raceEnded) return;

        if (raceStarted)
        {
            currentTimer -= Time.deltaTime;

            if (currentTimer <= 0f)
            {
                timeOut();
            }
            Debug.Log(currentTimer);
        }
    }

    public void StartRaceServer()
    {
        if (!IsServer) return;

        currentTimer = timerSeconds;
        raceStarted = true;
        raceEnded = false;

    }

    public void firstFinish()
    {
        if (!IsServer || raceEnded) return;
        currentTimer = Mathf.Min(currentTimer, firstFinishTimer);
        Debug.Log($"Server: First player finished! Grace period timer set to {firstFinishTimer} seconds.");
    }

    private void timeOut()
    {
        if (raceEnded) return;
        raceEnded = true;
        raceStarted = false;

        Debug.Log("Server: Race timer expired!");
    }

    public void StopTimer()
    {
        raceEnded = true;
        raceStarted = false;
    }
}