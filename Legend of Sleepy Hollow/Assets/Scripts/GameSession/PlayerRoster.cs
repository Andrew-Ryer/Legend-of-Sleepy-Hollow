using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/*
 * PlayerRoster is the shared player list every minigame plugs into. It lives on
 * the GameSession object in the overworld, is kept alive across scene loads with
 * DontDestroyOnLoad, and follows the whole session into every minigame scene.
 *
 * The server owns the list; every client sees the same rows replicated.
 * Minigame devs: call the static methods (AddLocalPoints, SubmitLocalResult)
 * from your score triggers or end-of-round code. Nothing needs wiring there.
 */

public class PlayerRoster : NetworkBehaviour
{
    public static PlayerRoster Local { get; private set; }

    // Host IS client id 0, so empty seats need their own marker.
    public const ulong UnclaimedClientId = ulong.MaxValue;

    public static EventHandler<PlayerRosterUpdatedArgs> PlayerRosterUpdated;

    public class PlayerRosterUpdatedArgs : EventArgs
    {
        public int playerCount;
    }

    [Serializable]
    public class DefaultPlayerRow
    {
        public string username;
        public GameTeam team;
        public int points;
    }

    [Header("Rows seeded the first time the host starts (editable in the inspector)")]
    [SerializeField] List<DefaultPlayerRow> _defaultRoster = new();

    readonly NetworkList<PlayerRosterEntry> _entries = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetEvents() => PlayerRosterUpdated = null;

    // Snapshot for UI and minigame code; the list itself stays write-safe on clients.
    public PlayerRosterEntry[] Snapshot()
    {
        var copy = new PlayerRosterEntry[_entries.Count];
        int index = 0;
        foreach (PlayerRosterEntry entry in _entries) copy[index++] = entry;
        return copy;
    }
    public int LocalPlayerId => FindByClientId(NetworkManager!.LocalClientId)?.PlayerId ?? -1;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // One GameSession per machine: a replicated duplicate steps aside.
        if (Local != null && Local != this)
        {
            Destroy(gameObject);
            return;
        }

        Local = this;
        DontDestroyOnLoad(gameObject);
        _entries.OnListChanged += HandleListChanged;

        if (IsServer)
        {
            SeedDefaultsOnce();
            EnsureEntry(NetworkManager!.LocalClientId);
        }

        // Push the current rows right away so late joiners and late subscribers catch up.
        RaiseRosterChanged();
    }

    public override void OnNetworkDespawn()
    {
        _entries.OnListChanged -= HandleListChanged;
        if (Local == this) Local = null;
        base.OnNetworkDespawn();
    }

    // ------------------------------------------------------------------
    // Server-side roster rules (entry ids are handed out here, never by clients)
    // ------------------------------------------------------------------

    void SeedDefaultsOnce()
    {
        Debug.Assert(IsServer);
        if (_entries.Count > 0) return;

        for (int i = 0; i < _defaultRoster.Count; i++)
        {
            DefaultPlayerRow row = _defaultRoster[i];
            _entries.Add(new PlayerRosterEntry
            {
                ClientId = UnclaimedClientId,
                PlayerId = i,
                Username = row.username,
                Team = row.team,
                Points = row.points,
            });
        }
    }

    // Connection plumbing: called from GameSessionManager on the server so a
    // player id exists for every client the moment it connects.
    public void ServerEnsureEntry(ulong clientId)
    {
        if (IsServer) EnsureEntry(clientId);
    }

    public void ServerReleaseEntry(ulong clientId)
    {
        if (!IsServer) return;

        int index = IndexOfClientId(clientId);
        if (index < 0) return;

        PlayerRosterEntry entry = _entries[index];
        entry.ClientId = UnclaimedClientId; // points and id stay, the seat opens up for a reconnect
        _entries[index] = entry;
    }

    PlayerRosterEntry EnsureEntry(ulong clientId)
    {
        Debug.Assert(IsServer);

        PlayerRosterEntry? existing = FindByClientId(clientId);
        if (existing.HasValue) return existing.Value;

        // Prefer a seeded seat that nobody has claimed yet: it keeps its
        // team, username and points, so a fresh joiner inherits the slot.
        int openSeat = IndexOfClientId(UnclaimedClientId);
        if (openSeat >= 0)
        {
            PlayerRosterEntry seat = _entries[openSeat];
            seat.ClientId = clientId;
            _entries[openSeat] = seat;
            return seat;
        }

        var entry = new PlayerRosterEntry
        {
            ClientId = clientId,
            PlayerId = _entries.Count,
            Username = $"Player {clientId}",
        };
        _entries.Add(entry);
        return entry;
    }

    public PlayerRosterEntry? FindByClientId(ulong clientId)
    {
        for (int i = 0; i < _entries.Count; i++)
            if (_entries[i].ClientId == clientId) return _entries[i];
        return null;
    }

    public PlayerRosterEntry? FindByPlayerId(int playerId)
    {
        for (int i = 0; i < _entries.Count; i++)
            if (_entries[i].PlayerId == playerId) return _entries[i];
        return null;
    }

    int NextTeamSeat(GameTeam team)
    {
        int seat = 0;
        for (int i = 0; i < _entries.Count; i++)
            if (_entries[i].Team == team) seat++;
        return seat;
    }

    // ------------------------------------------------------------------
    // Minigame-facing API: points move through one server RPC so every
    // machine ends the round with the same numbers.
    // ------------------------------------------------------------------

    /// <summary>Add points to one player by their stable player id.</summary>
    public static void AddPoints(int playerId, int points) => SubmitLocalResult(playerId, points, GameTeam.Mortal, 0);

    /// <summary>Add points for the local player only.</summary>
    public static void AddLocalPoints(int points) => SubmitLocalResult(-1, points, GameTeam.Mortal, 0);

    // End-of-minigame submit: playerPoints land on the local player, teamPoints
    // on every entry of the local player's team. playerId -1 means "the local player".
    public static void SubmitLocalResult(int playerId, int playerPoints, GameTeam team, int teamPoints)
    {
        PlayerRoster roster = Local;
        if (roster == null)
        {
            Debug.LogError("PlayerRoster not spawned: join or host a session first (the GameSession object follows you into minigames).");
            return;
        }

        if (playerId < 0) playerId = roster.LocalPlayerId;
        if (playerId < 0 && teamPoints == 0)
        {
            Debug.LogWarning("SubmitLocalResult: local player is not on the roster yet.");
            return;
        }

        roster.SubmitResultServerRpc(playerId, playerPoints, team, teamPoints);
    }

    [Rpc(SendTo.Server)]
    void SubmitResultServerRpc(int playerId, int playerPoints, GameTeam team, int teamPoints)
    {
        if (playerId >= 0) ApplyPointsToPlayer(playerId, playerPoints);

        if (teamPoints != 0)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Team != team) continue;
                PlayerRosterEntry entry = _entries[i];
                entry.Points += teamPoints;
                _entries[i] = entry;
            }
        }
    }

    void ApplyPointsToPlayer(int playerId, int points)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].PlayerId != playerId) continue;
            PlayerRosterEntry entry = _entries[i];
            entry.Points += points;
            _entries[i] = entry;
            return;
        }
        Debug.LogWarning($"SubmitResult: no roster entry for player id {playerId}.");
    }

    // ------------------------------------------------------------------
    // Identity plumbing (username, team) driven by PlayerIdentity / PlayerRosterSetup
    // ------------------------------------------------------------------

    public static void ServerSetUsername(ulong clientId, string username) => Local?.ApplyUsername(clientId, username);

    public static void ServerSetTeam(int playerId, GameTeam team) => Local?.ApplyTeam(playerId, team);

    void ApplyUsername(ulong clientId, string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return;

        PlayerRosterEntry? entry = FindByClientId(clientId);
        if (!entry.HasValue) return;

        PlayerRosterEntry updated = entry.Value;
        updated.Username = username.Trim();
        int index = IndexOfClientId(clientId);
        _entries[index] = updated;
    }

    void ApplyTeam(int playerId, GameTeam team)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].PlayerId != playerId) continue;
            PlayerRosterEntry entry = _entries[i];
            if (entry.Team == team) return;
            entry.Team = team;
            _entries[i] = entry;
            return;
        }
    }

    int IndexOfClientId(ulong clientId)
    {
        for (int i = 0; i < _entries.Count; i++)
            if (_entries[i].ClientId == clientId) return i;
        return -1;
    }

    // ------------------------------------------------------------------
    // Events
    // ------------------------------------------------------------------

    void HandleListChanged(NetworkListEvent<PlayerRosterEntry> changeEvent) => RaiseRosterChanged();

    void RaiseRosterChanged() =>
        PlayerRosterUpdated?.Invoke(this, new PlayerRosterUpdatedArgs { playerCount = _entries.Count });
}
