using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Unity.Netcode;
using UnityEngine;

// Script to handle which player is closest to a finishPoint from the startingPoint
// Updated to work with the leaderboard UI to track player positions
// Updated to now handle points per player
public class RaceProgressTracker : MonoBehaviour
{
    [SerializeField] Transform _spawnPoint;
    [SerializeField] Transform _finishPoint;
    [SerializeField] SpringTimer _springTimer;
    [SerializeField] PointsEventScript _pointsEventScript;

    const float PrintIntervalSeconds = 1f;
    float _nextPrintTime;
    bool _raceFinished;

    readonly Dictionary<ulong, PlayerProgress> _players = new();
    readonly Dictionary<ulong, float> _finishTimes = new();
    readonly List<PlayerProgress> _rankedPlayers = new();
    readonly List<PlayerProgress> _finalPlayers = new();

    public struct PlayerProgress
    {
        public ulong ClientId;
        public string PlayerName;
        public float Progress;
        public int Points;
        public bool HasFinished;
        public float FinishTime;
    }

    public IReadOnlyList<PlayerProgress> RankedPlayers => _rankedPlayers;
    public bool RaceFinished => _raceFinished;

    public IReadOnlyList<PlayerProgress> FinalPlayers
    {
        get
        {
            if (!_raceFinished)
            {
                _finalPlayers.Clear();
                return _finalPlayers;
            }

            CaptureFinalProgress();
            _finalPlayers.Clear();
            foreach (PlayerProgress player in _players.Values)
                _finalPlayers.Add(player);

            _finalPlayers.Sort(CompareFinalResults);
            return _finalPlayers;
        }
    }

    void Awake()
    {
        if (_springTimer == null)
            _springTimer = FindFirstObjectByType<SpringTimer>();

        if (_pointsEventScript == null)
            _pointsEventScript = GetComponent<PointsEventScript>();
        if (_pointsEventScript == null)
            _pointsEventScript = FindFirstObjectByType<PointsEventScript>();
    }

    void OnEnable()
    {
        if (_springTimer != null)
            _springTimer.TimerExpired += HandleTimerExpired;
    }

    void OnDisable()
    {
        if (_springTimer != null)
            _springTimer.TimerExpired -= HandleTimerExpired;
    }

    void Update()
    {
        if (_raceFinished || _spawnPoint == null || _finishPoint == null || _springTimer == null)
            return;

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening)
            return;

        float startX = _spawnPoint.position.x;
        float finishX = _finishPoint.position.x;
        if (Mathf.Approximately(startX, finishX))
            return;

        CapturePlayerProgress(networkManager, startX, finishX);

        RefreshRankedPlayers();

        if (_rankedPlayers.Count == 0)
            return;

        if (AllPlayersFinished())
        {
            FinishRace(true, networkManager.IsServer);
            return;
        }

        if (networkManager.IsServer && Time.time >= _nextPrintTime)
        {
            _nextPrintTime = Time.time + PrintIntervalSeconds;
            Debug.Log(BuildLiveLeaderboard());
        }
    }

    public void RegisterPlayerFinished(SpringPlayerMovementInput player)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (_raceFinished || _springTimer == null || !_springTimer.GameplayStarted ||
            networkManager == null || !networkManager.IsServer || player == null)
            return;

        NetworkObject networkObject = player.GetComponent<NetworkObject>();
        if (networkObject == null || !player.MarkFinished())
            return;

        ulong clientId = networkObject.OwnerClientId;
        PlayerProgress finishedPlayer = new PlayerProgress
        {
            ClientId = clientId,
            PlayerName = $"Player {clientId + 1}",
            Progress = 100f,
            HasFinished = true,
            FinishTime = _springTimer.ElapsedTime
        };

        _players[clientId] = finishedPlayer;
        _finishTimes[clientId] = _springTimer.ElapsedTime;
        player.SetFinishTime(_springTimer.ElapsedTime);
        RefreshRankedPlayers();
    }

    void RefreshRankedPlayers()
    {
        _rankedPlayers.Clear();
        _rankedPlayers.AddRange(_players.Values);
        _rankedPlayers.Sort((a, b) =>
        {
            int progressOrder = b.Progress.CompareTo(a.Progress);
            return progressOrder != 0 ? progressOrder : a.ClientId.CompareTo(b.ClientId);
        });
    }

    void CapturePlayerProgress(NetworkManager networkManager, float startX, float finishX)
    {
        foreach (NetworkObject networkObject in networkManager.SpawnManager.SpawnedObjectsList)
        {
            if (networkObject == null || !networkObject.IsPlayerObject ||
                !networkObject.TryGetComponent(out SpringPlayerMovementInput player))
                continue;

            ulong clientId = networkObject.OwnerClientId;
            float progress = player.HasFinished ? 100f : Mathf.Min(99f, Mathf.InverseLerp(startX, finishX, player.transform.position.x) * 100f);

            _players[clientId] = new PlayerProgress
            {
                ClientId = clientId,
                PlayerName = $"Player {clientId + 1}",
                Progress = progress,
                Points = player.FinalPoints,
                HasFinished = player.HasFinished,
                FinishTime = player.FinishTime
            };
        }
    }

    bool AllPlayersFinished()
    {
        if (_rankedPlayers.Count == 0)
            return false;

        foreach (PlayerProgress player in _rankedPlayers)
        {
            if (player.Progress < 100f)
                return false;
        }

        return true;
    }

    void HandleTimerExpired()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        FinishRace(false, networkManager == null || networkManager.IsServer);
    }

    void FinishRace(bool allPlayersFinished, bool shouldLog)
    {
        if (_raceFinished)
            return;

        CaptureFinalProgress();
        AssignFinalPoints();
        RefreshRankedPlayers();
        _raceFinished = true;
        _springTimer.StopTimer();

        if (!shouldLog)
            return;

        StringBuilder message = new StringBuilder();
        if (allPlayersFinished)
            message.AppendLine("All players reached the end!");

        message.Append(BuildFinalResults());
        Debug.Log(message.ToString());
    }

    void CaptureFinalProgress()
    {
        if (_spawnPoint == null || _finishPoint == null)
            return;

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening || networkManager.SpawnManager == null)
            return;

        float startX = _spawnPoint.position.x;
        float finishX = _finishPoint.position.x;
        if (!Mathf.Approximately(startX, finishX))
            CapturePlayerProgress(networkManager, startX, finishX);
    }

    void AssignFinalPoints()
    {
        List<ulong> clientIds = new List<ulong>(_players.Keys);
        foreach (ulong clientId in clientIds)
        {
            PlayerProgress player = _players[clientId];
            player.Points = _pointsEventScript != null
                ? _pointsEventScript.CalculatePoints(player.Progress)
                : Mathf.Clamp(Mathf.RoundToInt(player.Progress), 0, 100);
            _players[clientId] = player;

            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || networkManager.SpawnManager == null)
                continue;

            foreach (NetworkObject networkObject in networkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject == null || !networkObject.IsPlayerObject || networkObject.OwnerClientId != clientId)
                    continue;

                if (networkObject.TryGetComponent(out SpringPlayerMovementInput movement))
                    movement.SetFinalPoints(player.Points);
                break;
            }
        }
    }

    string BuildLiveLeaderboard()
    {
        StringBuilder message = new StringBuilder();
        for (int i = 0; i < _rankedPlayers.Count; i++)
        {
            if (i > 0)
                message.Append(" - ");

            int place = i + 1;
            message.Append(place).Append(GetPlaceSuffix(place)).Append(" Place: ")
                .Append(_rankedPlayers[i].PlayerName).Append(" (")
                .Append(Mathf.RoundToInt(_rankedPlayers[i].Progress)).Append("%)");
        }

        return message.ToString();
    }

    string BuildFinalResults()
    {
        List<PlayerProgress> players = new List<PlayerProgress>(_players.Values);
        players.Sort(CompareFinalResults);

        StringBuilder message = new StringBuilder();
        for (int i = 0; i < players.Count; i++)
        {
            int place = i + 1;
            message.Append(place).Append(GetPlaceSuffix(place)).Append(" Place: ")
                .Append(players[i].PlayerName).Append(" (")
                .Append(Mathf.RoundToInt(players[i].Progress)).Append("%, ")
                .Append(players[i].Points).Append(" points, ");

            if (_finishTimes.TryGetValue(players[i].ClientId, out float finishTime))
                message.Append(finishTime.ToString("F3", CultureInfo.InvariantCulture)).Append('s');
            else
                message.Append("n/a");

            message.AppendLine(")");
        }

        return message.ToString();
    }

    static int CompareFinalResults(PlayerProgress a, PlayerProgress b)
    {
        if (a.HasFinished && b.HasFinished)
        {
            int timeOrder = a.FinishTime.CompareTo(b.FinishTime);
            return timeOrder != 0 ? timeOrder : a.ClientId.CompareTo(b.ClientId);
        }

        if (a.HasFinished)
            return -1;
        if (b.HasFinished)
            return 1;

        int progressOrder = b.Progress.CompareTo(a.Progress);
        return progressOrder != 0 ? progressOrder : a.ClientId.CompareTo(b.ClientId);
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
