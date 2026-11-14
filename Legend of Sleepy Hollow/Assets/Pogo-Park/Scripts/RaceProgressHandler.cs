using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

// Script to handle which player is closest to a finishPoint from the startingPoint
public class RaceProgressTracker : MonoBehaviour
{
    [SerializeField] Transform _startingPoint;
    [SerializeField] Transform _finishPoint;

    const float PrintIntervalSeconds = 1f;
    float _nextPrintTime;

    struct PlayerProgress
    {
        public string PlayerName;
        public float Progress;
    }

    void Update()
    {
        if (Time.time < _nextPrintTime)
            return;

        _nextPrintTime = Time.time + PrintIntervalSeconds;
        PrintLeaderboard();
    }

    void PrintLeaderboard()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (_startingPoint == null || _finishPoint == null || networkManager == null || !networkManager.IsServer)
            return;

        float startX = _startingPoint.position.x;
        float finishX = _finishPoint.position.x;
        if (Mathf.Approximately(startX, finishX))
            return;

        List<PlayerProgress> players = new List<PlayerProgress>();
        foreach (NetworkObject networkObject in networkManager.SpawnManager.SpawnedObjectsList)
        {
            if (networkObject == null || !networkObject.IsPlayerObject ||
                !networkObject.TryGetComponent(out SpringPlayerMovementInput player))
                continue;

            float progress = Mathf.InverseLerp(startX, finishX, player.transform.position.x) * 100f;
            players.Add(new PlayerProgress
            {
                PlayerName = $"Player {networkObject.OwnerClientId + 1}",
                Progress = progress
            });
        }

        if (players.Count == 0)
            return;

        players.Sort((a, b) => b.Progress.CompareTo(a.Progress));

        StringBuilder message = new StringBuilder();
        for (int i = 0; i < players.Count; i++)
        {
            if (i > 0)
                message.Append(" - ");

            int place = i + 1;
            message.Append(place).Append(GetPlaceSuffix(place)).Append(" Place: ")
                .Append(players[i].PlayerName).Append(" (")
                .Append(Mathf.RoundToInt(players[i].Progress)).Append(" units)");
        }

        Debug.Log(message.ToString());
    }

    static string GetPlaceSuffix(int place)
    {
        int lastTwoDigits = place % 100;
        if (lastTwoDigits >= 11 && lastTwoDigits <= 13)
            return "th";

        return (place % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
    }
}
