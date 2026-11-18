using Unity.Netcode;

/* One row of the shared roster: stable player id, username, team, points. */

public struct PlayerRosterEntry : INetworkSerializable
{
    public ulong ClientId;
    public int PlayerId;
    public GameTeam Team;
    public int Points;
    public string Username;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerId);
        serializer.SerializeValue(ref Team);
        serializer.SerializeValue(ref Points);
        serializer.SerializeValue(ref Username);
    }
}
