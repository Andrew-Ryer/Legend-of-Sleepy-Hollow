using System;
using Unity.Netcode;
using UnityEngine;

/*
 * PlayerRosterEntry is one row of the shared roster: stable player id, username,
 * team, and the total points earned across every minigame.
 *
 * The username uses the NGO 2.x pattern for NetworkList elements: a fixed byte
 * buffer so the struct stays unmanaged (no strings inside replicated structs).
 */

public unsafe struct PlayerRosterEntry : INetworkSerializable, IEquatable<PlayerRosterEntry>
{
    public const int MaxUsernameLength = 24;

    public ulong ClientId;
    public int PlayerId;
    public GameTeam Team;
    public int Points;
    public byte UsernameLength;

    private fixed byte _usernameBytes[MaxUsernameLength];

    public unsafe string Username
    {
        get
        {
            int length = Mathf.Clamp(UsernameLength, (byte)0, (byte)MaxUsernameLength);
            var characters = new char[length];
            fixed (byte* bytes = _usernameBytes)
            {
                for (int i = 0; i < length; i++) characters[i] = (char)bytes[i];
            }
            return new string(characters);
        }
        set
        {
            string trimmed = (value ?? string.Empty).Trim();
            int length = Math.Min(trimmed.Length, MaxUsernameLength);

            UsernameLength = (byte)length;
            fixed (byte* bytes = _usernameBytes)
            {
                for (int i = 0; i < length; i++)
                    bytes[i] = (byte)Math.Min(trimmed[i], (char)127);
            }
        }
    }

    public unsafe void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerId);
        serializer.SerializeValue(ref Team);
        serializer.SerializeValue(ref Points);

        int length = UsernameLength;
        serializer.SerializeValue(ref length);
        UsernameLength = (byte)Mathf.Clamp(length, 0, MaxUsernameLength);

        fixed (byte* bytes = _usernameBytes)
        {
            for (int i = 0; i < length && i < MaxUsernameLength; i++)
            {
                byte character = bytes[i];
                serializer.SerializeValue(ref character);
                bytes[i] = character;
            }
        }
    }

    public readonly bool Equals(PlayerRosterEntry other)
    {
        return ClientId == other.ClientId
            && PlayerId == other.PlayerId
            && Team == other.Team
            && Points == other.Points
            && UsernameLength == other.UsernameLength
            && Username == other.Username;
    }

    public override readonly bool Equals(object obj) => obj is PlayerRosterEntry other && Equals(other);

    public override readonly int GetHashCode() => HashCode.Combine(ClientId, PlayerId, (int)Team, Points, UsernameLength, Username);
}
