using UnityEngine;
using UnityEngine.InputSystem;

/*
 * OverworldScoreboard is the Tab-key scoreboard in the overworld: two teams,
 * every player's points, and both team totals. Start hidden; Tab toggles.
 */

public class OverworldScoreboard : ScoreboardUi
{
    void Awake()
    {
        SetVisible(false);
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
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            SetVisible(!gameObject.activeSelf);
    }

    void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
        if (visible) Refresh();
    }
}
