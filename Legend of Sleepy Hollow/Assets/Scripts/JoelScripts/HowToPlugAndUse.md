# GameSession: shared player list and scores (Joel's layer)

The prefab `Assets/Overworld/Prefabs/GameSession.prefab` holds the shared
player list: name, team, points. It syncs to every player automatically.

## Setup in a minigame scene

Nothing to set up. The overworld host spawns the GameSession object, and Unity
keeps it alive across scene loads. It is already there when your scene starts.

Do not add a NetworkManager to your minigame scene. If your scene already has
one, delete it.

## Read the players

```csharp
foreach (PlayerRosterEntry e in PlayerRoster.Local.Snapshot())
    Debug.Log($"{e.Username} team {e.Team} pts {e.Points}");
```

`PlayerRoster.Local` is null before any host starts. Check for null.

## Give out points

```csharp
PlayerRoster.AddLocalPoints(10);                             // my score gets +10
PlayerRoster.SubmitLocalResult(-1, 12, GameTeam.Mortal, 5);  // I get 12, each player on my team gets +5
```

You can call these from any client. The server applies them.

## Refresh your UI when the list changes

```csharp
void OnEnable()  { PlayerRoster.PlayerRosterUpdated += MyRefresh; }
void OnDisable() { PlayerRoster.PlayerRosterUpdated -= MyRefresh; }
```

## Scoreboards

- Minigame: drag `Assets/Overworld/Prefabs/MinigameScoreboardCanvas.prefab`
  into your scene. It shows and updates itself. No code needed.
- Overworld: a chip at the top-left shows both team totals and your own
  `name: points`. Hold Tab for the full board. Release Tab to close it.
  Players who have not picked a seat are never shown.
- Every `Player.prefab` already has a nameplate. Do not add your own.

## Files (all in `Assets/Scripts/JoelScripts/`)

| File | What it does |
|---|---|
| `PlayerRoster.cs` | the player list, plus the static calls above |
| `PlayerRosterEntry.cs` | one row: id, name, team, points |
| `GameSessionManager.cs` | spawns the prefab when the host starts |
| `PlayerIdentity.cs` / `PlayerNameLabel.cs` | username sync + nameplate |
| `ScoreboardUi/Row/Overworld/Minigame*.cs` | the scoreboards |
| `PlayerRosterSetup.cs` | overworld name/team buttons |
