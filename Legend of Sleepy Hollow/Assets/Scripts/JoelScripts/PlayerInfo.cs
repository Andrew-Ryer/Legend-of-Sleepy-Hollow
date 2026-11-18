// Plain player data. Not a network type: use it with any multiplayer system.
public struct PlayerInfo
{
    public int PlayerId;
    public string Username;
    public GameTeam Team;
    public int Points;

    public PlayerInfo(int playerId, string username, GameTeam team, int points)
    {
        PlayerId = playerId;
        Username = username;
        Team = team;
        Points = points;
    }
}
