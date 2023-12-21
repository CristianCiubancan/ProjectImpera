// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgConnect.cs
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

using System;
using System.Threading.Tasks;
using Comet.Game.Database.Repositories;
using Comet.Game.States;
using Comet.Network.Packets;
using Comet.Shared;
using Comet.Shared.Models;
using Org.BouncyCastle.Utilities.Encoders;

#endregion

namespace Comet.Game.Packets
{
    #region References

    using static MsgTalk;

    #endregion

    /// <remarks>Packet Type 1052</remarks>
    /// <summary>
    ///     Message containing a connection request to the game server. Contains the player's
    ///     access token from the Account server, and the patch and language versions of the
    ///     game client.
    /// </summary>
    public sealed class MsgConnect : MsgBase<Client>
    {
        // Static properties from server initialization
        public static bool StrictAuthentication { get; set; }

        // Packet Properties
        public ulong Token { get; set; }
        public ushort Patch { get; set; }
        public string Language { get; set; }
        public string MacAddress { get; set; }
        public int Version { get; set; }

        /// <summary>
        ///     Decodes a byte packet into the packet structure defined by this message class.
        ///     Should be invoked to structure data from the client for processing. Decoding
        ///     follows TQ Digital's byte ordering rules for an all-binary protocol.
        /// </summary>
        /// <param name="bytes">Bytes from the packet processor or client socket</param>
        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            this.Length = reader.ReadUInt16();
            this.Type = (PacketType)reader.ReadUInt16();
            this.Token = reader.ReadUInt64();
            this.Patch = reader.ReadUInt16();
            this.Language = reader.ReadString(2);
            this.MacAddress = Hex.ToHexString(reader.ReadBytes(8));
            this.Version = Convert.ToInt32(reader.ReadInt32().ToString(), 2);
        }

        /// <summary>
        ///     Process can be invoked by a packet after decode has been called to structure
        ///     packet fields and properties. For the server implementations, this is called
        ///     in the packet handler after the message has been dequeued from the server's
        ///     <see cref="PacketProcessor{TClient}" />.
        /// </summary>
        /// <param name="client">Client requesting packet processing</param>
        public override async Task ProcessAsync(Client client)
        {
            // Validate access token
            var auth = Kernel.Logins.Get(Token.ToString()) as TransferAuthArgs;
            await Log.WriteLogAsync(LogLevel.Debug, $"[{client.IPAddress}] has connected to the game server.");
            if (auth == null || StrictAuthentication && auth.IPAddress == client.IPAddress)
            {
                if (auth != null)
                    Kernel.Logins.Remove(Token.ToString());
                
                await client.SendAsync(LoginInvalid);
                await Log.WriteLogAsync(LogLevel.Warning, $"Invalid Login Token: {Token} from {client.IPAddress}");
                client.Socket.Disconnect(false);
                return;
            }

            Kernel.Logins.Remove(Token.ToString());
            
            // Generate new keys and check for an existing character
            var character = await CharactersRepository.FindAsync(auth.AccountID);
            client.AccountIdentity = auth.AccountID;
            client.AuthorityLevel = auth.AuthorityID;
            client.MacAddress = MacAddress;

//             // temp code for pre-release
// #if DEBUG
//             if (client.AuthorityLevel < 2)
//             {
//                 await client.SendAsync(new MsgConnectEx(MsgConnectEx.RejectionCode.NonCooperatorAccount));
//                 await Log.WriteLogAsync(LogLevel.Warning, $"{client.Identity} non cooperator account.");
//                 client.Socket.Disconnect(false);
//                 return;
//             }
// #endif

            if (character == null)
            {
                // Create a new character
                client.Creation = new Creation {AccountID = auth.AccountID, Token = (uint) Token};
                Kernel.Registration.Add(client.Creation.Token);
                await client.SendAsync(LoginNewRole);
            }
            else
            {
                // Character already exists
                client.Character = new Character(character, client);
                Console.WriteLine($"[{client.Character.Name}] has logged in.");
                if (await Kernel.RoleManager.LoginUserAsync(client))
                {
                    client.Character.MateName = (await CharactersRepository.FindByIdentityAsync(client.Character.MateIdentity))?.Name ?? Game.Language.StrNone;
                    await client.SendAsync(LoginOk);
                    await client.SendAsync(new MsgUserInfo(client.Character));
                    await client.SendAsync(new MsgData(DateTime.Now));

#if DEBUG
                    await client.Character.SendAsync($"Server is running in DEBUG mode. Version: [{Kernel.SERVER_VERSION}]{Kernel.Version}", TalkChannel.Talk);
#endif
                }
            }
        }
    }
}