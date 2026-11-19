using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/*
 * OverworldScoreboard is the chip in the top-left corner. Collapsed it shows
 * each team's total plus your name and points; hold Tab to open the full
 * two-column scoreboard, release to close it.
 */

public class OverworldScoreboard : ScoreboardUi
{
    [SerializeField] GameObject _expandedContent;
    [SerializeField] GameObject _chipContent;
    [SerializeField] Image _panelImage;

    [SerializeField] TMP_Text _chipMortal;
    [SerializeField] TMP_Text _chipGhost;
    [SerializeField] TMP_Text _chipMe;

    void Awake()
    {
        _chipContent.SetActive(true);
        _expandedContent.SetActive(false);
    }

    protected void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    protected void OnDisable()
    {
        Unsubscribe();
    }

    void Update()
    {
        SetExpanded(Keyboard.current != null && Keyboard.current.tabKey.isPressed);
    }

    void SetExpanded(bool open)
    {
        if (_expandedContent.activeSelf == open) return;

        _expandedContent.SetActive(open);
        _chipContent.SetActive(!open);
        _panelImage.enabled = !open;
        Refresh();
    }

    protected override void OnRefreshed(PlayerRoster roster)
    {
        int mortalTotal = 0, ghostTotal = 0;
        foreach (PlayerRosterEntry entry in roster.Snapshot())
        {
            if (entry.ClientId == PlayerRoster.UnclaimedClientId) continue;
            if (entry.Team == GameTeam.Mortal) mortalTotal += entry.Points;
            else ghostTotal += entry.Points;
        }

        _chipMortal.text = $"Mortals: {mortalTotal}";
        _chipGhost.text = $"Ghosts: {ghostTotal}";

        PlayerRosterEntry? me = roster.FindByClientId(NetworkManager.Singleton.LocalClientId);
        _chipMe.text = me.HasValue ? $"{me.Value.Username}: {me.Value.Points}" : "You: 0";
    }
}
