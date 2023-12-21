// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgConnectEx.cs
// Description:
// 
// Creator: FELIPEVIEIRAVENDRAMI [FELIPE VIEIRA VENDRAMINI]
// 
// Developed by:
// Felipe Vieira Vendramini <felipevendramini@live.com>
// 
// Programming today is a race between software engineers striving to build bigger and better
// idiot-proof programs, and the Universe trying to produce bigger and better idiots.
// So far, the Universe is winning.
// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#region References

using Comet.Game.States;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    /// <remarks>Packet Type 1055</remarks>
    /// <summary>
    ///     Message containing an authentication response from the server to the client. Can
    ///     either accept a client with realm connection details and an access token, or
    ///     reject a client with a reason on why the login attempt was rejected.
    /// </summary>
    public sealed class MsgConnectEx : MsgBase<Client>
    {
        /// <summary>
        ///     Instantiates a new instance of <see cref="MsgConnectEx" /> for rejecting a
        ///     client connection using a rejection code. The rejection code spawns an error
        ///     dialog in the client with a respective error message.
        /// </summary>
        /// <param name="rejectionCode">Rejection code / error message ID</param>
        public MsgConnectEx(RejectionCode rejectionCode)
        {
            Code = rejectionCode;
        }

        /// <summary>
        ///     Instantiates a new instance of <see cref="MsgConnectEx" /> for accepting a
        ///     client connection. Provides the client with redirect information for the game
        ///     server of their choice, accompanied by an access token for that server.
        /// </summary>
        /// <param name="ipAddress">IP address of the game server</param>
        /// <param name="port">Port the game server is listening on</param>
        /// <param name="token">Access token for the game server</param>
        public MsgConnectEx(string ipAddress, uint port, ulong token)
        {
            Token = token;
            IPAddress = ipAddress;
            Port = port;
        }

        // Packet Properties
        public ulong Token { get; set; }
        public RejectionCode Code { get; set; }
        public string IPAddress { get; set; }
        public uint Port { get; set; }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgConnectEx);
            if (Code != RejectionCode.Clear)
            {
                // Failed login
                writer.Write((uint) 0);
                writer.Write((uint) Code);
            }
            else
            {
                // Successful login
                writer.Write(Token);
                writer.Write(IPAddress, 16);
                writer.Write(Port);
                writer.Write((ushort) 0);
            }

            return writer.ToArray();
        }

        /// <summary>
        ///     Rejection codes are sent to the client in offset 8 of this packet when the
        ///     client has failed authentication with the account server. These codes define
        ///     which error message will be displayed in the client.
        /// </summary>
        public enum RejectionCode : uint
        {
            Clear = 0,
            InvalidPassword = 1,
            ServerDown = 10,
            PleaseTryAgainLater = 11,
            AccountBanned = 12,
            ServerFull = 20,
            ServerBusy = 21,
            AccountLocked = 22,
            AccountNotActivated = 30,
            AccountActivationFailed = 31,
            ServerTimedOut = 42,
            NonCooperatorAccount = 50,
            AccountMaxLoginAttempts = 51,
            InvalidAccount = 57,
            ValidationTimeout = 58,
            ServersNotConfigured = 59,
            DatabaseError = 64,
            ServerLocked = 70,
            ServerOldProtocol = 73
        }
    }
}