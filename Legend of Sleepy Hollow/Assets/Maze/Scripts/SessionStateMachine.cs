using Unity.Netcode;
using UnityEngine;

public enum SessionPhase
{
    Lobby,
    Countdown,
    Playing,
    GameOver,
}

/*
 * TestSessionStateMachine is Demo 08's match phase. Clients may request
 * a change; only the server checks ready players, runs the countdown,
 * and writes the phase. Countdown can return to Lobby, and GameOver can
 * return to Lobby.
 */

public class SessionStateMachine : NetworkBehaviour
{
    [SerializeField, Min(1)]
    int requiredReadyPlayers = 2;

    [SerializeField, Min(0.25f)]
    float countdownSeconds = 3f;

    public SessionPhase CurrentPhase => _phase.Value;
    public int RequiredReadyPlayers => requiredReadyPlayers;
    public float CountdownRemaining => _countdownRemaining;
    public int ReadyPlayerCount => CountReadyPlayers();
    public bool CanStart =>
        _phase.Value == SessionPhase.Lobby
        && HasHostAndClient
        && CountReadyPlayers() >= requiredReadyPlayers;

    readonly NetworkVariable<SessionPhase> _phase = new(
        SessionPhase.Lobby,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    float _countdownRemaining;
    string _lastBlockedReason = "Waiting for ready players.";

    public override void OnNetworkSpawn()
    {
        if (_phase.Value == SessionPhase.Lobby)
            Debug.Log(
                $"[SessionState] Spawned | phase={_phase.Value} | isServer={IsServer} | isClient={IsClient}"
            );

        _phase.OnValueChanged += HandlePhaseChanged;

        if (!IsServer || NetworkManager == null)
            return;

        _phase.Value = SessionPhase.Lobby;
        _countdownRemaining = 0f;
        _lastBlockedReason = "Waiting for ready players.";
        NetworkManager.OnConnectionEvent += HandleConnectionEvent;
    }

    public override void OnNetworkDespawn()
    {
        _phase.OnValueChanged -= HandlePhaseChanged;

        if (IsServer && NetworkManager != null)
            NetworkManager.OnConnectionEvent -= HandleConnectionEvent;
    }

    void Update()
    {
        if (!IsServer)
            return;

        if (_phase.Value == SessionPhase.Lobby && CanStart)
        {
            TryStartCountdown("host and client connected");
            return;
        }

        if (_phase.Value == SessionPhase.Countdown)
        {
            if (CountReadyPlayers() < requiredReadyPlayers)
            {
                ChangePhase(
                    SessionPhase.Lobby,
                    "countdown cancelled because not enough ready players remain"
                );
                return;
            }

            _countdownRemaining -= Time.deltaTime;
            if (_countdownRemaining <= 0f)
                ChangePhase(SessionPhase.Playing, "countdown finished");
        }
    }

    public void RequestStart()
    {
        if (IsServer)
        {
            TryStartCountdown("local server control");
            return;
        }

        RequestStartRpc();
    }

    public void RequestGameOver()
    {
        if (IsServer)
        {
            TryEndGame("local server control");
            return;
        }

        RequestGameOverRpc();
    }

    public void RequestReturnToLobby()
    {
        if (IsServer)
        {
            TryReturnToLobby("local server reset");
            return;
        }

        RequestReturnToLobbyRpc();
    }

    [Rpc(SendTo.Server)]
    void RequestStartRpc(RpcParams rpcParams = default)
    {
        TryStartCountdown($"client request from {rpcParams.Receive.SenderClientId}");
    }

    [Rpc(SendTo.Server)]
    void RequestGameOverRpc(RpcParams rpcParams = default)
    {
        TryEndGame($"client request from {rpcParams.Receive.SenderClientId}");
    }

    [Rpc(SendTo.Server)]
    void RequestReturnToLobbyRpc(RpcParams rpcParams = default)
    {
        TryReturnToLobby($"client reset request from {rpcParams.Receive.SenderClientId}");
    }

    void TryStartCountdown(string reason)
    {
        if (_phase.Value != SessionPhase.Lobby)
        {
            Reject($"start rejected from {reason}; phase is {_phase.Value}");
            return;
        }

        if (!HasHostAndClient)
        {
            Reject($"start rejected from {reason}; waiting for a host and client");
            return;
        }

        int readyPlayers = CountReadyPlayers();
        if (readyPlayers < requiredReadyPlayers)
        {
            Reject(
                $"start rejected from {reason}; readyPlayers={readyPlayers}, "
                    + $"requiredReadyPlayers={requiredReadyPlayers}"
            );
            return;
        }

        _countdownRemaining = countdownSeconds;
        ChangePhase(SessionPhase.Countdown, reason);
    }

    bool HasHostAndClient =>
        NetworkManager != null
        && NetworkManager.IsHost
        && NetworkManager.ConnectedClientsIds.Count >= 2;

    void TryEndGame(string reason)
    {
        if (_phase.Value != SessionPhase.Playing)
        {
            Reject($"game over rejected from {reason}; phase is {_phase.Value}");
            return;
        }

        ChangePhase(SessionPhase.GameOver, reason);
    }

    void TryReturnToLobby(string reason)
    {
        if (_phase.Value != SessionPhase.GameOver)
        {
            Reject($"lobby reset rejected from {reason}; phase is {_phase.Value}");
            return;
        }

        ChangePhase(SessionPhase.Lobby, reason);
    }

    void HandleConnectionEvent(NetworkManager networkManager, ConnectionEventData eventData)
    {
        if (!IsServer)
            return;

        if (eventData.EventType == ConnectionEvent.ClientDisconnected)
            RecheckSessionCanContinue("client disconnected");
    }

    void RecheckSessionCanContinue(string reason)
    {
        if (_phase.Value == SessionPhase.Lobby || _phase.Value == SessionPhase.GameOver)
            return;

        if (CountReadyPlayers() >= requiredReadyPlayers)
            return;

        SessionPhase fallback =
            _phase.Value == SessionPhase.Playing ? SessionPhase.GameOver : SessionPhase.Lobby;
        ChangePhase(fallback, reason);
    }

    void ChangePhase(SessionPhase nextPhase, string reason)
    {
        if (!IsServer)
            return;
        if (_phase.Value == nextPhase)
            return;

        SessionPhase previousPhase = _phase.Value;
        if (!IsValidTransition(previousPhase, nextPhase))
        {
            Reject($"invalid transition {previousPhase} -> {nextPhase} from {reason}");
            return;
        }

        _phase.Value = nextPhase;
        _lastBlockedReason = string.Empty;
        if (nextPhase == SessionPhase.Lobby)
            _lastBlockedReason = "Waiting for ready players.";

        Debug.Log($"[SessionState] {previousPhase} -> {nextPhase} | reason={reason}");
    }

    void Reject(string reason)
    {
        _lastBlockedReason = reason;
        Debug.LogWarning($"[SessionState] {reason}");
    }

    void HandlePhaseChanged(SessionPhase previousPhase, SessionPhase currentPhase)
    {
        Debug.Log($"[SessionState] Observed phase change {previousPhase} -> {currentPhase}");
    }

    int CountReadyPlayers()
    {
        int count = 0;
        foreach (
            var player in FindObjectsByType<NetworkObject>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            )
        )
        {
            if (!player.IsPlayerObject || !player.IsSpawned)
                continue;
            count++;
        }

        return count;
    }

    static bool IsValidTransition(SessionPhase current, SessionPhase next)
    {
        return current switch
        {
            SessionPhase.Lobby => next == SessionPhase.Countdown,
            SessionPhase.Countdown => next == SessionPhase.Playing || next == SessionPhase.Lobby,
            SessionPhase.Playing => next == SessionPhase.GameOver,
            SessionPhase.GameOver => next == SessionPhase.Lobby,
            _ => false,
        };
    }
}
