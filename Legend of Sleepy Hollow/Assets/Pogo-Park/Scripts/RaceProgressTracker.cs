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
    [SerializeField] Timer _timer;
    [SerializeField] PointsEventScript _pointsEventScript;

    const float PrintIntervalSeconds = 1f;
    float _nextPrintTime;
    bool _raceFinished;

    readonly Dictionary<ulong, PlayerProgress> _players = new();
    readonly Dictionary<ulong, float> _finishTimes = new();
    readonly List<PlayerProgress> _rankedPlayers = new();

    public struct PlayerProgress
    {
        public ulong ClientId;
        public string PlayerName;
        public float Progress;
        public int Points;
    }

    public IReadOnlyList<PlayerProgress> RankedPlayers => _rankedPlayers;

    void Awake()
    {
        if (_timer == null)
            _timer = FindFirstObjectByType<Timer>();

        if (_pointsEventScript == null)
            _pointsEventScript = GetComponent<PointsEventScript>();
        if (_pointsEventScript == null)
            _pointsEventScript = gameObject.AddComponent<PointsEventScript>();
    }

    void OnEnable()
    {
        if (_timer != null)
            _timer.TimerExpired += HandleTimerExpired;
    }

    void OnDisable()
    {
        if (_timer != null)
            _timer.TimerExpired -= HandleTimerExpired;
    }

    void Update()
    {
        if (_raceFinished || _spawnPoint == null || _finishPoint == null || _timer == null)
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
        if (_raceFinished || _timer == null || !_timer.GameplayStarted ||
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
            Progress = 100f
        };

        _players[clientId] = finishedPlayer;
        _finishTimes[clientId] = _timer.ElapsedTime;
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
                Progress = progress
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
        _timer.StopTimer();

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
            player.Points = _pointsEventScript.CalculatePoints(player.Progress);
            _players[clientId] = player;
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
        players.Sort((a, b) =>
        {
            bool aFinished = _finishTimes.TryGetValue(a.ClientId, out float aTime);
            bool bFinished = _finishTimes.TryGetValue(b.ClientId, out float bTime);

            if (aFinished && bFinished)
                return aTime.CompareTo(bTime);
            if (aFinished)
                return -1;
            if (bFinished)
                return 1;

            int progressOrder = b.Progress.CompareTo(a.Progress);
            return progressOrder != 0 ? progressOrder : a.ClientId.CompareTo(b.ClientId);
        });

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
