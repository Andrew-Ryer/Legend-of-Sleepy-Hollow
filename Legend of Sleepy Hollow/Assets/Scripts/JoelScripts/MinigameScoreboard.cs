using TMPro;
using Unity.Netcode;
using UnityEngine;

/*
 * MinigameScoreboard is the always-on scoreboard for a minigame scene: two
 * team columns, per-player points, team totals, and the local player's line.
 * It refreshes itself from the replicated roster the moment the scene's
 * session finishes syncing - no wiring needed in minigame code.
 */

public class MinigameScoreboard : ScoreboardUi
{
    [SerializeField] TMP_Text _localPlayerText;

    void OnEnable()
    {
        Subscribe();

        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null) return;

        if (manager.LocalClientId != NetworkManager.ServerClientId || manager.IsServer) Refresh();
        else manager.OnConnectionEvent += HandleConnectionEvent;
    }

    void OnDisable()
    {
        Unsubscribe();
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnConnectionEvent -= HandleConnectionEvent;
    }

    void HandleConnectionEvent(NetworkManager networkManager, ConnectionEventData eventData)
    {
        // Our own connection event carries the local client id.
        if (eventData.EventType != ConnectionEvent.ClientConnected || eventData.ClientId != networkManager.LocalClientId) return;

        networkManager.OnConnectionEvent -= HandleConnectionEvent;
        Refresh();
    }

    protected override void OnRefreshed(PlayerRoster roster)
    {
        PlayerRosterEntry? entry = roster.FindByClientId(NetworkManager.Singleton.LocalClientId);
        _localPlayerText.text = entry.HasValue
            ? $"{entry.Value.Username}: {entry.Value.Points} pts ({entry.Value.Team})"
            : "";
    }
}
