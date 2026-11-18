using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * PlayerRoster is the shared player list every minigame plugs into. It lives on
 * the GameSession object, is moved to DontDestroyOnLoad, and follows the session
 * into every minigame scene. The server owns it; the whole list is replicated.
 *
 * Minigame devs: call AddLocalPoints / SubmitLocalResult from your score code,
 * and GoTo / ReturnToOverworld when the round needs a different scene.
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

    [Header("Where players land after a trip back to the overworld")]
    [SerializeField] float _landingRadius = 3f;
    [SerializeField] float _landingHeight = 1.05f;

    public const string OverworldScene = "OverworldJ";

    // True while the session is sitting in the overworld scene.
    public static bool InOverworld => SceneManager.GetActiveScene().name == OverworldScene;

    // Only one trip at a time. Reset every time this object spawns.
    bool _tripUnderway;

    // Fires on every machine when the session's scene trip is over, including a
    // trip where somebody took too long.
    public static EventHandler<SceneArrivedArgs> SceneArrived;

    public class SceneArrivedArgs : EventArgs
    {
        public string sceneName;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetTravelEvents() => SceneArrived = null;

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

        // One GameSession per machine. Only the server may remove a replicated
        // object; a client-side duplicate just steps aside and waits for the
        // server's despawn to reach it.
        if (Local != null && Local != this)
        {
            if (IsServer) Destroy(gameObject);
            return;
        }

        Local = this;
        DontDestroyOnLoad(gameObject);
        _roster.OnValueChanged += HandleRosterChanged;
        _tripUnderway = false;

        // Connections keep being claimed and released even while the session is
        // sitting in a minigame scene, because this object is the one that stays.
        NetworkManager.OnConnectionEvent += HandleConnectionEvent;
        NetworkManager.SceneManager.OnLoadEventCompleted += HandleSceneTripFinished;
        NetworkManager.SceneManager.OnLoadComplete += HandleLocalArrival;
        NetworkManager.SceneManager.OnSynchronizeComplete += HandleLocalSync;

        if (IsServer)
        {
            SeedDefaults();
            EnsureEntry(NetworkManager!.LocalClientId);
        }

        RaiseRosterChanged();
    }

    void HandleConnectionEvent(NetworkManager networkManager, ConnectionEventData eventData)
    {
        if (!IsServer) return;

        switch (eventData.EventType)
        {
            case ConnectionEvent.ClientConnected:
                EnsureEntry(eventData.ClientId);
                break;
            case ConnectionEvent.ClientDisconnected:
                ServerReleaseEntry(eventData.ClientId);
                break;
        }
    }

    public override void OnNetworkDespawn()
    {
        _roster.OnValueChanged -= HandleRosterChanged;
        NetworkManager.OnConnectionEvent -= HandleConnectionEvent;
        NetworkManager.SceneManager.OnLoadEventCompleted -= HandleSceneTripFinished;
        NetworkManager.SceneManager.OnLoadComplete -= HandleLocalArrival;
        NetworkManager.SceneManager.OnSynchronizeComplete -= HandleLocalSync;
        _tripUnderway = false;
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

    // Connection plumbing: only the server may change seats.
    public void ServerEnsureEntry(ulong clientId)
    {
        if (IsServer) EnsureEntry(clientId);
    }

    void ServerReleaseEntry(ulong clientId)
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
            Debug.LogWarning("SubmitLocalResult: not on roster yet.");
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
                if (index < 0) Debug.LogWarning($"SubmitResult: no roster entry {playerId}.");
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

    // ------------------------------------------------------------------
    // Minigame-facing API: one call moves the whole session. The connection,
    // the seats, and the scores all ride along, there and back.
    // ------------------------------------------------------------------

    /// <summary>Send every connected player into the named scene.</summary>
    public static void GoTo(string sceneName)
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !(manager.IsServer || manager.IsClient))
        {
            Debug.LogError("PlayerRoster.GoTo: nobody is connected. Host or join a session first.");
            return;
        }

        PlayerRoster roster = Local;
        if (roster == null || !roster.IsSpawned)
        {
            Debug.LogError("PlayerRoster.GoTo: there is no live GameSession object. Host or join a session first.");
            return;
        }

        if (!InBuildSettings(sceneName))
        {
            Debug.LogError($"PlayerRoster.GoTo: nobody can load '{sceneName}' because it is not in File > Build Settings.");
            return;
        }

        if (manager.IsServer) roster.ServerMove(sceneName);
        else roster.RequestMoveServerRpc(sceneName);
    }

    /// <summary>End the minigame: everyone goes back to the overworld with their scores.</summary>
    public static void ReturnToOverworld() => GoTo(OverworldScene);

    static bool InBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            if (System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i)) == sceneName)
                return true;
        return false;
    }

    // The server is the one that loads; Netcode repeats the trip on every
    // connected client, so nobody has to check who is hosting.
    void ServerMove(string sceneName)
    {
        if (_tripUnderway)
        {
            Debug.LogWarning($"PlayerRoster.GoTo: a trip is already running, so '{sceneName}' was skipped.");
            return;
        }

        if (SceneManager.GetActiveScene().name == sceneName)
        {
            Debug.Log($"PlayerRoster.GoTo: everyone is already in '{sceneName}'.");
            return;
        }

        _tripUnderway = true;

        // A minigame that paused with Time.timeScale = 0 would stall the trip:
        // Netcode tracks it with ordinary frames.
        Time.timeScale = 1f;

        try
        {
            SceneEventProgressStatus status = NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                _tripUnderway = false;
                Debug.LogError($"PlayerRoster.GoTo: the trip to '{sceneName}' did not start ({status}). One trip at a time, and the scene has to be in Build Settings.");
            }
        }
        catch (Exception e)
        {
            _tripUnderway = false;
            Debug.LogError($"PlayerRoster.GoTo: the trip to '{sceneName}' failed: {e.Message}");
        }
    }

    [Rpc(SendTo.Server)]
    void RequestMoveServerRpc(string sceneName) => ServerMove(sceneName);

    // This computer finished loading the scene, so it parks its own player right
    // away instead of waiting for the slowest computer.
    void HandleLocalArrival(ulong clientId, string sceneName, LoadSceneMode mode)
    {
        if (clientId != NetworkManager!.LocalClientId) return;

        Time.timeScale = 1f;

        if (sceneName == OverworldScene) LandEveryoneInOverworld();
    }

    // A machine that joins a session already sitting in the overworld finishes
    // its first sync instead of a scene trip, so it lands here.
    void HandleLocalSync(ulong clientId)
    {
        if (clientId != NetworkManager!.LocalClientId) return;

        Time.timeScale = 1f;

        if (InOverworld) LandEveryoneInOverworld();
    }

    // The whole group's trip is over. This is what a minigame waits on to start
    // its round. A trip where somebody took too long also counts as over, and
    // that machine is not always named in the log, so check for a missing player.
    void HandleSceneTripFinished(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        _tripUnderway = false;

        if (clientsTimedOut.Count > 0)
            Debug.LogWarning($"PlayerRoster: {clientsTimedOut.Count} player(s) did not confirm they reached '{sceneName}'.");

        WarnAboutSceneNetworkManager();

        SceneArrived?.Invoke(this, new SceneArrivedArgs { sceneName = sceneName });
    }

    // A minigame scene may carry its own NetworkManager. It is left alone,
    // because that scene belongs to another student, and it controls nothing
    // while this session is running.
    void WarnAboutSceneNetworkManager()
    {
        foreach (NetworkManager other in FindObjectsByType<NetworkManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (other == NetworkManager) continue;

            Debug.LogWarning($"PlayerRoster: '{other.gameObject.name}' came with the scene that just loaded. It is a second NetworkManager and it controls nothing while this session runs.");
        }
    }

    // Each machine parks its own player on the same ring, worked out from the
    // shared PlayerId, so every machine agrees on where everybody lands.
    void LandEveryoneInOverworld()
    {
        NetworkObject mine = NetworkManager!.LocalClient?.PlayerObject;
        if (mine == null) return;

        PlayerRosterEntry? seat = FindByClientId(NetworkManager.LocalClientId);
        if (!seat.HasValue)
        {
            Debug.LogWarning("PlayerRoster: this player is not on the roster yet, so it stays where it is.");
            return;
        }

        float angle = seat.Value.PlayerId * 137.5f * Mathf.Deg2Rad;
        Vector3 landing = new Vector3(Mathf.Sin(angle) * _landingRadius, _landingHeight, Mathf.Cos(angle) * _landingRadius);
        NetworkTransform move = mine.GetComponent<NetworkTransform>();

        CharacterController controller = mine.GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;
        if (controller != null) controller.enabled = false;
        if (move != null) move.Teleport(landing, Quaternion.identity, mine.transform.localScale);
        else mine.transform.SetPositionAndRotation(landing, Quaternion.identity);
        if (controller != null) controller.enabled = wasEnabled;
    }

    void HandleRosterChanged(RosterRows previous, RosterRows next) => RaiseRosterChanged();

    void RaiseRosterChanged() =>
        PlayerRosterUpdated?.Invoke(this, new PlayerRosterUpdatedArgs { playerCount = _roster.Value.Rows.Count });
}
