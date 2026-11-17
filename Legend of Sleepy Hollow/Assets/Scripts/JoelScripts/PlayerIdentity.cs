using Unity.Netcode;
using UnityEngine;

/*
 * PlayerIdentity rides on the player prefab. The owner tells the shared roster
 * its username once (and the UI can re-send it), and can ask to switch team.
 * The server-side roster rules live in PlayerRoster.
 */

public class PlayerIdentity : NetworkBehaviour
{
    public static PlayerIdentity Local { get; private set; }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner) return;

        Local = this;
        if (!string.IsNullOrEmpty(PlayerRosterSetup.LocalUsername))
            StartCoroutine(SendUsernameWhenRosterReady());
    }

    // The player can spawn before the GameSession object exists; wait for the
    // roster to hold our seat, then hand the username over.
    System.Collections.IEnumerator SendUsernameWhenRosterReady()
    {
        while (PlayerRoster.Local == null || PlayerRoster.Local.FindByClientId(OwnerClientId) == null)
            yield return null;
        SendUsernameServerRpc(PlayerRosterSetup.LocalUsername);
    }

    public override void OnNetworkDespawn()
    {
        if (Local == this) Local = null;
        base.OnNetworkDespawn();
    }

    [Rpc(SendTo.Server)]
    void SendUsernameServerRpc(string username) => PlayerRoster.ServerSetUsername(OwnerClientId, username);

    public static void LocalSetUsername(string username)
    {
        PlayerRosterSetup.LocalUsername = username;
        if (Local != null) Local.SendUsernameServerRpc(username);
    }

    public static void LocalSwitchTeam(GameTeam team)
    {
        if (Local == null) return;
        Local.SwitchTeamServerRpc(team);
    }

    // Runs on the host: resolve the caller's seat from the RPC sender so a
    // client can only ever change its own team.
    [Rpc(SendTo.Server)]
    void SwitchTeamServerRpc(GameTeam team)
    {
        PlayerRoster roster = PlayerRoster.Local;
        if (roster == null) return;
        PlayerRosterEntry? entry = roster.FindByClientId(OwnerClientId);
        if (entry.HasValue) PlayerRoster.ServerSetTeam(entry.Value.PlayerId, team);
    }
}
