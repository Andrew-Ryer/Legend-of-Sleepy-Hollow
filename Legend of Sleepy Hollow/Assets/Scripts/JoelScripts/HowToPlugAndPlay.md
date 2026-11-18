
## 1. Start a session

1. Open `Assets/Scenes/OverworldJ.unity` and press Play.
2. On one game instance, click **Host Session**.
3. On the other instances, click **Join Client**.

On one computer the default address `127.0.0.1` works. On another computer on the same
network, set `UnityTransport` **Address** to the host computer's local IP before joining,
and keep the same **Port**, currently `7777`. This is a direct connection, not an
internet lobby or a join-code service.

The overworld already creates the shared player list. Keep that connection
running in your minigame; do not start a second NetworkManager. Section 5 moves
everyone between scenes without breaking that connection.

### Already have your own Unity Netcode connection?

Before starting it:

1. Add `GameSessionManager` to your existing NetworkManager object, if absent.
2. Assign that NetworkManager to its **Network Manager** field.
3. Assign `Assets/Overworld/Prefabs/GameSession.prefab` to **Game Session Prefab**.
4. Register that prefab in your manager's **Network Prefabs** list on all builds.

Use your existing host/join buttons. `GameSessionManager` listens to those
connections and creates the roster. Then use the calls below.
This setup is for Unity Netcode, not a different networking library.

## 2. Read the players

Inside one of your methods:

```csharp
if (PlayerRoster.Local == null) return;

foreach (var player in PlayerRoster.Local.Snapshot())
{
    if (player.ClientId == PlayerRoster.UnclaimedClientId) continue;
    Debug.Log($"{player.PlayerId}: {player.Username}, {player.Team}, {player.Points}");
}
```

This gives you the connected players. It does not create their characters.
You still handle your own spawning, movement, and game rules.

## 3. Match a character to its data

Inside that character's `NetworkBehaviour`:

```csharp
var player = PlayerRoster.Local?.FindByClientId(OwnerClientId);

if (player.HasValue)
    Debug.Log(player.Value.PlayerId);
```

Match by the character's owner, not its name. If you spawn your own networked
characters, the server gives each one ownership matching that player's `ClientId`.

## 4. Give points

Give this machine's local player 10 points:

```csharp
PlayerRoster.AddLocalPoints(10);
```

Or have the host give a specific player 10 points:

```csharp
PlayerRoster.AddPoints(playerId, 10);
```

`playerId` is the `PlayerId` from their roster data.
Report each award once, not once on every computer.

A computer that has just joined can score a second or two later than the rest.
If the Console says `not on roster yet`, wait a moment and try that award again.

The same running session already shares these scores with the overworld.
There is no extra send-back call. Changing your copy of the data does not save it.

## 5. Going to the minigame and back

Two calls. They already sit on `PlayerRoster`, so there is nothing to drag into
your scene and nothing to wire in the Inspector.

Start your minigame:

```csharp
PlayerRoster.GoTo("Maze");
```

Send everyone back when the round is over:

```csharp
PlayerRoster.ReturnToOverworld();
```

Either call works on any computer, host or client. Start a session in the
overworld first, and wait until the text says Connected. The session's own server
moves everybody, so you do not check who is the host. Call it once, from a
button, an entrance trigger, or the end of your round.

What stays alive during the trip:

- The connection, and the shared player objects.
- The roster. Names, teams, and points are still there, and `PlayerRoster.Local`
  works in the new scene.
- Everyone lands in a small ring in the overworld, so nobody stays stuck where the
  minigame ended.
- Time runs normally again on arrival, even if your round ended paused. Do not
  pause again while the trip is loading.

Inside your minigame the overworld player stands still. It stops reading the
overworld controls, so your own movement code has to move it.

Optional: start your round when the trip is over. Put the first line in `Start`,
and the last line in `OnDestroy`. The message fires on every computer still
connected to the session. The trip can finish before everyone arrives, so look
for a missing player before you start.

```csharp
PlayerRoster.SceneArrived += HandleArrived;

void HandleArrived(object sender, PlayerRoster.SceneArrivedArgs e)
{
    if (e.sceneName == "Maze") StartRound();
}
```

Stop listening when your object goes away:

```csharp
PlayerRoster.SceneArrived -= HandleArrived;
```

## Using different networking? Use PlayerInfoBridge

Think of the bridge as a local list your game can read. Your existing
networking still connects the computers and sends the messages.

1. Drag `Assets/Overworld/Prefabs/PlayerInfoBridge.prefab` into your scene.
2. Keep your existing connection code. The bridge starts no connection.
3. When your network code receives player data, turn it into `PlayerInfo` values.
4. Give that list to the bridge on Unity's main thread:

```csharp
PlayerInfoBridge.Local.Publish(receivedPlayers);
```

`receivedPlayers` is your received `PlayerInfo[]`. Each entry has `PlayerId`,
`Username`, `Team`, and `Points`. Keep the same `PlayerId` on your own character
so you know which entry belongs to it.

Once the bridge exists and has data, your other scripts read it:

```csharp
foreach (var player in PlayerInfoBridge.Local.Players)
    Debug.Log($"{player.Username}: {player.Points}");
```

Publish again whenever your networking receives a changed list. Do this on
each computer that needs the data; publishing does not send a network message.

If the shared roster already exists, the bridge copies it automatically.
That copy includes empty seats and has no `ClientId`; it is not a spawn list.

The bridge does not send scores back to the overworld. That handoff is not
built yet. For working shared score updates, keep the shared session and use
`PlayerRoster.AddPoints` instead.
