using System;
using System.Collections.Generic;
using UnityEngine;

// Bridge for scenes that run their own multiplayer and do NOT want a
// NetworkManager. Drop PlayerInfoBridge.prefab into the scene and use:
//   PlayerInfoBridge.Local.Players        - the current player list
//   PlayerInfoBridge.Local.PlayersChanged - fires whenever it changes
// If a GameSession roster exists the bridge mirrors it automatically. If
// there is no GameSession, call Publish() from your own netcode after your
// server sends player data, and everything reading the bridge sees it.
public class PlayerInfoBridge : MonoBehaviour
{
    public static PlayerInfoBridge Local { get; private set; }

    public event Action PlayersChanged;

    readonly List<PlayerInfo> _players = new List<PlayerInfo>();
    public IReadOnlyList<PlayerInfo> Players => _players;

    bool _hooked;

    void Awake()
    {
        if (Local != null && Local != this)
        {
            Destroy(gameObject);
            return;
        }
        Local = this;
    }

    void OnEnable() => _hooked = false;

    void OnDisable()
    {
        Unhook();
        if (Local == this) Local = null;
    }

    void LateUpdate()
    {
        if (_hooked || PlayerRoster.Local == null) return;
        PlayerRoster.PlayerRosterUpdated += OnRosterUpdated;
        _hooked = true;
        CopyRoster();
    }

    void Unhook()
    {
        if (!_hooked) return;
        PlayerRoster.PlayerRosterUpdated -= OnRosterUpdated;
        _hooked = false;
    }

    void OnRosterUpdated(object sender, PlayerRoster.PlayerRosterUpdatedArgs e) => CopyRoster();

    void CopyRoster()
    {
        PlayerRoster roster = PlayerRoster.Local;
        if (roster == null) return;
        _players.Clear();
        foreach (PlayerRosterEntry e in roster.Snapshot())
            _players.Add(new PlayerInfo(e.PlayerId, e.Username, e.Team, e.Points));
        PlayersChanged?.Invoke();
    }

    // For your own multiplayer: push your player list whenever your server
    // sends new data. The bridge stores it and fires PlayersChanged.
    public void Publish(IReadOnlyList<PlayerInfo> players)
    {
        _players.Clear();
        foreach (PlayerInfo p in players) _players.Add(p);
        PlayersChanged?.Invoke();
    }
}
