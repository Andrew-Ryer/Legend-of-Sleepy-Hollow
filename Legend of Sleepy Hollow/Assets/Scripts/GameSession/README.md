# GameSession — shared player roster, scores, and nameplates

One persistent object, `GameSession` (a `PlayerRoster` on a `NetworkObject`),
lives for the whole run: it is spawned by the server in the overworld, survives
every scene load (`DontDestroyOnLoad`), and is replicated to every client.
Minigame code never touches the network directly — it reads the roster and
reports scores through static calls.

Everything here lives in the `GameSession` assembly definition, but it is
`autoReferenced`, so your `Assembly-CSharp` minigame scripts can call it with
no setup.

## The one API you need

```csharp
// Read the roster (works on every client, read-only):
foreach (PlayerRosterEntry entry in PlayerRoster.Local.Snapshot())
{
    Debug.Log($"{entry.Username} (team {entry.Team}) has {entry.Points} pts");
}

// Add points for the local player during your minigame:
PlayerRoster.AddLocalPoints(10);

// Add points for a specific player id:
PlayerRoster.AddPoints(playerId, 10);

// End-of-minigame report (player score + team score at once).
// Pass -1 for playerId to mean "me". teamPoints is added to EVERY member of
// `team`, so pass each player's share (e.g. a 30-point team purse spread over
// the survivors), not the whole purse once.
PlayerRoster.SubmitLocalResult(playerId: -1, playerPoints: 12, team: GameTeam.Mortal, teamPoints: 30);
```

All four calls are safe to call from any client: they route through a server
RPC, and every peer's roster (and therefore every scoreboard) updates
automatically.

Useful lookups:

```csharp
PlayerRoster roster = PlayerRoster.Local;      // null only before the session spawns
int myId = roster.LocalPlayerId;
PlayerRosterEntry? me = roster.FindByClientId(NetworkManager.Singleton.LocalClientId);
PlayerRosterEntry? other = roster.FindByPlayerId(somePlayerId);
```

A `PlayerRosterEntry` carries: `PlayerId` (never reused, stable across
disconnects), `ClientId` (`ulong.MaxValue` = seat nobody is sitting in),
`Username`, `Team` (`GameTeam.Mortal` / `GameTeam.Ghost`), `Points`.

## Reacting to score changes

```csharp
PlayerRoster.PlayerRosterUpdated += HandleRosterUpdated; // in OnEnable
// signature: void HandleRosterUpdated(object sender, PlayerRoster.PlayerRosterUpdatedArgs e)
PlayerRoster.PlayerRosterUpdated -= HandleRosterUpdated; // in OnDisable
```

`ScoreboardUi` subclasses (`OverworldScoreboard`, `MinigameScoreboard`) do this
for you; you only need the event if you build your own HUD.

## Scoreboards

- **Overworld:** already built in `OverworldJ`. Hidden until **Tab**. Two team
  columns, per-player rows, team totals; the SetupPanel (username field,
  Be Mortal / Be Ghost buttons) drives names and teams.
- **Minigame scenes:** drag `Assets/Scripts/GameSession/Prefabs/MinigameScoreboardCanvas.prefab`
  into your scene and it self-refreshes when the session syncs — zero wiring.
  It shows both teams plus one line for the local player.
- Rows are spawned from `Assets/Scripts/GameSession/Prefabs/ScoreRow.prefab`;
  resize that one prefab if 30-point usernames ever overflow.

## Nameplates

`Player.prefab` already contains the floating nameplate (world-space canvas +
TextMeshPro, billboarded to the camera in `PlayerNameLabel`). Every player
model instantiated from it gets a plate automatically — do not add your own.

## Rules for minigame scenes

1. **Never create your own `NetworkManager` / start a host or client.** The
   overworld session is already running and persists into your scene. If your
   scene has its own `NetworkManager` GameObject, it is now a duplicate
   singleton: delete it, or the scene's `SessionManager`-style scripts will be
   wiring a dead component.
2. Don't re-parent or destroy the `GameSession` GameObject.
3. Report scores once, at the moment your minigame decides the outcome — the
   roster handles the rest, including the overworld scoreboard.
4. If your minigame spawns its own `NetworkObjects`, add the prefabs to the
   overworld `NetworkPrefabs` list (`Assets/Overworld/Prefabs/NetworkPrefabs.asset`),
   because that is the NetworkManager doing the replication.

## Files

| File | What it is |
|---|---|
| `GameTeam.cs` | `Mortal` / `Ghost` enum |
| `PlayerRosterEntry.cs` | roster row (fixed-size username buffer so it fits `NetworkList<T>`) |
| `PlayerRoster.cs` | the replicated roster + all static entry points |
| `GameSessionManager.cs` | server-side: spawns GameSession, claims/releases seats |
| `PlayerIdentity.cs` | player-side: sends username/team changes up |
| `PlayerNameLabel.cs` | overhead nameplate, copies the Resource Collector design |
| `ScoreboardUi/Row/Overworld/Minigame*.cs` | the two scoreboards |
| `PlayerRosterSetup.cs` | username + team pick panel |
