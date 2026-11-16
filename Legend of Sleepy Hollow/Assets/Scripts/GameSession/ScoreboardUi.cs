using System.Collections.Generic;
using TMPro;
using UnityEngine;

/*
 * ScoreboardUi fills two team columns from the replicated roster: one row per
 * player plus a per-team total. The overworld scoreboard and the minigame
 * scoreboard both inherit this; the fields are wired in the inspector.
 */

public abstract class ScoreboardUi : MonoBehaviour
{
    [Header("Team headers")]
    [SerializeField] TMP_Text _mortalTitle;
    [SerializeField] TMP_Text _ghostTitle;

    [Header("Team totals")]
    [SerializeField] TMP_Text _mortalTotal;
    [SerializeField] TMP_Text _ghostTotal;

    [Header("Columns that receive the spawned rows")]
    [SerializeField] GameObject _mortalColumn;
    [SerializeField] GameObject _ghostColumn;

    [SerializeField] ScoreboardRow _rowPrefab;

    readonly List<ScoreboardRow> _rows = new();
    bool _subscribed;

    protected void Subscribe()
    {
        if (_subscribed) return;
        PlayerRoster.PlayerRosterUpdated += HandleRosterUpdated;
        _subscribed = true;
    }

    protected void Unsubscribe()
    {
        if (!_subscribed) return;
        PlayerRoster.PlayerRosterUpdated -= HandleRosterUpdated;
        _subscribed = false;
    }

    void HandleRosterUpdated(object sender, PlayerRoster.PlayerRosterUpdatedArgs e) => Refresh();

    protected void Refresh()
    {
        PlayerRoster roster = PlayerRoster.Local;
        ClearRows();
        if (roster == null) return;

        int mortalCount = 0, ghostCount = 0;
        int mortalTotal = 0, ghostTotal = 0;

        foreach (PlayerRosterEntry entry in roster.Snapshot())
        {
            bool mortal = entry.Team == GameTeam.Mortal;
            ScoreboardRow row = Instantiate(_rowPrefab, (mortal ? _mortalColumn : _ghostColumn).transform);
            row.SetContent(entry.Username, entry.Points);
            _rows.Add(row);

            if (mortal)
            {
                mortalCount++;
                mortalTotal += entry.Points;
            }
            else
            {
                ghostCount++;
                ghostTotal += entry.Points;
            }
        }

        _mortalTitle.text = $"Mortals ({mortalCount})";
        _ghostTitle.text = $"Ghosts ({ghostCount})";
        _mortalTotal.text = $"Total: {mortalTotal}";
        _ghostTotal.text = $"Total: {ghostTotal}";

        OnRefreshed(roster);
    }

    protected virtual void OnRefreshed(PlayerRoster roster) { }

    void ClearRows()
    {
        foreach (ScoreboardRow row in _rows) Destroy(row.gameObject);
        _rows.Clear();
    }
}
