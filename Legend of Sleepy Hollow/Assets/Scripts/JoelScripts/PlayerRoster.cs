using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/*
 * PlayerRoster is the shared player list every minigame plugs into. It lives on
 * the GameSession object, is moved to DontDestroyOnLoad, and follows the session
 * into every minigame scene. The server owns it; the whole list is replicated.
 *
 * Minigame devs: call AddLocalPoints / SubmitLocalResult from your score code.
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

    [Serializable]
    class RosterRows : INetworkSerializable, IEquatable<RosterRows>
    {
        public List<PlayerRosterEntry> Rows = new();

        public bool Equals(RosterRows other) => other != null && Rows.SequenceEqual(other.Rows);

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsWriter)
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Rows.Count);
                foreach (PlayerRosterEntry row in Rows) writer.WriteValueSafe(row);
            }
            else
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out int count);
                Rows.Clear();
                for (int i = 0; i < count; i++)
                {
                    reader.ReadValueSafe(out PlayerRosterEntry row);
                    Rows.Add(row);
                }
            }
        }
    }

    readonly NetworkVariable<RosterRows> _roster = new(
        new RosterRows(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Snapshot for UI and minigame code.
    public PlayerRosterEntry[] Snapshot() => _roster.Value.Rows.ToArray();

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
        _roster.OnValueChanged += HandleRosterChanged;

        if (IsServer)
        {
            SeedDefaults();
            EnsureEntry(NetworkManager!.LocalClientId);
        }

        RaiseRosterChanged();
    }

    public override void OnNetworkDespawn()
    {
        _roster.OnValueChanged -= HandleRosterChanged;
        if (Local == this) Local = null;
        base.OnNetworkDespawn();
    }

    // Every server-side change mutates a copy and commits it once.
    void Commit(Action<List<PlayerRosterEntry>> change)
    {
        RosterRows rows = new();
        rows.Rows.AddRange(_roster.Value.Rows);
        change(rows.Rows);
        _roster.Value = rows;
    }

    void SeedDefaults()
    {
        if (_roster.Value.Rows.Count > 0) return;

        Commit(rows =>
        {
            for (int i = 0; i < _defaultRoster.Count; i++)
            {
                DefaultPlayerRow seat = _defaultRoster[i];
                rows.Add(new PlayerRosterEntry
                {
                    ClientId = UnclaimedClientId,
                    PlayerId = i,
                    Username = seat.username,
                    Team = seat.team,
                    Points = seat.points,
                });
            }
        });
    }

    // Connection plumbing, called from GameSessionManager on the server.
    public void ServerEnsureEntry(ulong clientId)
    {
        if (IsServer) EnsureEntry(clientId);
    }

    public void ServerReleaseEntry(ulong clientId)
    {
        if (!IsServer) return;

        Commit(rows =>
        {
            int index = rows.FindIndex(e => e.ClientId == clientId);
            if (index < 0) return;
            PlayerRosterEntry entry = rows[index];
            entry.ClientId = UnclaimedClientId; // points and id stay; seat opens up for a reconnect
            rows[index] = entry;
        });
    }

    void EnsureEntry(ulong clientId)
    {
        Commit(rows =>
        {
            if (rows.Exists(e => e.ClientId == clientId)) return;

            // Prefer a seeded seat nobody claimed: it keeps its team, name and points.
            int seat = rows.FindIndex(e => e.ClientId == UnclaimedClientId);
            if (seat >= 0)
            {
                PlayerRosterEntry entry = rows[seat];
                entry.ClientId = clientId;
                rows[seat] = entry;
                return;
            }

            rows.Add(new PlayerRosterEntry
            {
                ClientId = clientId,
                PlayerId = rows.Count,
                Username = $"Player {clientId}",
            });
        });
    }

    public PlayerRosterEntry? FindByClientId(ulong clientId)
    {
        foreach (PlayerRosterEntry entry in _roster.Value.Rows)
            if (entry.ClientId == clientId) return entry;
        return null;
    }

    public PlayerRosterEntry? FindByPlayerId(int playerId)
    {
        foreach (PlayerRosterEntry entry in _roster.Value.Rows)
            if (entry.PlayerId == playerId) return entry;
        return null;
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
        Commit(rows =>
        {
            if (playerId >= 0)
            {
                int index = rows.FindIndex(e => e.PlayerId == playerId);
                if (index < 0) Debug.LogWarning($"SubmitResult: no roster entry for player id {playerId}.");
                else
                {
                    PlayerRosterEntry entry = rows[index];
                    entry.Points += playerPoints;
                    rows[index] = entry;
                }
            }

            for (int i = 0; i < rows.Count; i++)
            {
                if (teamPoints == 0 || rows[i].Team != team) continue;
                if (rows[i].ClientId == UnclaimedClientId) continue; // empty seat scores nothing
                PlayerRosterEntry entry = rows[i];
                entry.Points += teamPoints;
                rows[i] = entry;
            }
        });
    }

    // ------------------------------------------------------------------
    // Identity plumbing driven by PlayerIdentity / PlayerRosterSetup
    // ------------------------------------------------------------------

    public static void ServerSetUsername(ulong clientId, string username) => Local?.ApplyUsername(clientId, username);

    public static void ServerSetTeam(int playerId, GameTeam team) => Local?.ApplyTeam(playerId, team);

    void ApplyUsername(ulong clientId, string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return;

        Commit(rows =>
        {
            int index = rows.FindIndex(e => e.ClientId == clientId);
            if (index < 0) return;
            PlayerRosterEntry entry = rows[index];
            entry.Username = username.Trim();
            rows[index] = entry;
        });
    }

    void ApplyTeam(int playerId, GameTeam team)
    {
        Commit(rows =>
        {
            int index = rows.FindIndex(e => e.PlayerId == playerId);
            if (index < 0) return;
            PlayerRosterEntry entry = rows[index];
            entry.Team = team;
            rows[index] = entry;
        });
    }

    void HandleRosterChanged(RosterRows previous, RosterRows next) => RaiseRosterChanged();

    void RaiseRosterChanged() =>
        PlayerRosterUpdated?.Invoke(this, new PlayerRosterUpdatedArgs { playerCount = _roster.Value.Rows.Count });
}
