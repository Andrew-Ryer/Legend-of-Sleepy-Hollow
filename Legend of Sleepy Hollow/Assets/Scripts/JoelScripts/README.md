# GameSession: shared player list + scores (Joel's layer)

One prefab does the whole job: `Assets/Overworld/Prefabs/GameSession.prefab`
(GameObject with a NetworkObject + a `PlayerRoster` script holding a list of
players: name, team, points).

## How the plug-and-play part works

1. In the overworld, the `Joel GameSession Bootstrap` object waits on the
   NetworkManager. When the host starts, it spawns the GameSession prefab.
2. Unity moves that spawned object to `DontDestroyOnLoad`, so it survives every
   scene load. Its roster syncs to every player automatically.
3. Your minigame scene needs **no NetworkManager of its own** and **no wiring**:
   the object is already there when your scene starts.

If your minigame scene has its own NetworkManager, delete it: the overworld
session is the only one.

## Using it from your minigame code

```csharp
// Read all players (updated live):
foreach (PlayerRosterEntry e in PlayerRoster.Local.Snapshot())
    Debug.Log($"{e.Username} team {e.Team} pts {e.Points}");

// Report scores, server-side safe from any client:
PlayerRoster.AddLocalPoints(10);                    // for me
PlayerRoster.SubmitLocalResult(-1, 12, GameTeam.Mortal, 5); // me + my team's players each get +5
```

`PlayerRoster.Local` is null only if no session was started. To refresh your
own UI the moment anything changes:

```csharp
PlayerRoster.PlayerRosterUpdated += MyRefresh;   // in OnEnable
PlayerRoster.PlayerRosterUpdated -= MyRefresh;   // in OnDisable
```

## Scoreboard prefabs

- Drag `Assets/Overworld/Prefabs/MinigameScoreboardCanvas.prefab` into your
  minigame scene: it appears and updates itself, zero code.
- Overworld scoreboard: small square top-left; **hold Tab** to see all scores,
  release to close.
- Nameplates float over every `Player.prefab` already; don't add your own.

## Files (all in `Assets/Scripts/JoelScripts/`)

| File | Role |
|---|---|
| `PlayerRoster.cs` | replicated player list + the static calls above |
| `PlayerRosterEntry.cs` | one row (id, name, team, points) |
| `GameSessionManager.cs` | spawns the prefab when the host starts |
| `PlayerIdentity.cs` / `PlayerNameLabel.cs` | username sync + nameplate |
| `ScoreboardUi/Row/Overworld/Minigame*.cs` | the scoreboards |
| `PlayerRosterSetup.cs` | overworld name/team buttons |
