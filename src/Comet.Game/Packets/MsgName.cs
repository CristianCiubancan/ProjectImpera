// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgName.cs
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

using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Game.Database.Models;
using Comet.Game.Database.Repositories;
using Comet.Game.States;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Syndicates;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgName : MsgBase<Client>
    {
        public MsgName()
        {
            Type = PacketType.MsgName;
        }

        public uint Identity { get; set; }
        public ushort PositionX { get; set; }
        public ushort PositionY { get; set; }
        public StringAction Action { get; set; }
        public List<string> Strings = new List<string>();

        /// <summary>
        ///     Decodes a byte packet into the packet structure defined by this message class.
        ///     Should be invoked to structure data from the client for processing. Decoding
        ///     follows TQ Digital's byte ordering rules for an all-binary protocol.
        /// </summary>
        /// <param name="bytes">Bytes from the packet processor or client socket</param>
        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            //PositionX = reader.ReadUInt16();
            //PositionY = reader.ReadUInt16();
            Action = (StringAction) reader.ReadByte();
            Strings = reader.ReadStrings();
        }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort)Type);
            writer.Write(Identity);
            //writer.Write(PositionX);
            //writer.Write(PositionY);
            writer.Write((byte) Action);
            writer.Write(Strings);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Role target = null;
            Character targetUser = null;
            switch (Action)
            {
                case StringAction.QueryMate:
                    targetUser = Kernel.RoleManager.GetUser(Identity);
                    if (targetUser == null)
                        return;

                    Strings[0] = targetUser.MateName;
                    await client.Character.SendAsync(this);
                    break;

                case StringAction.Guild:
                    Syndicate syndicate = Kernel.SyndicateManager.GetSyndicate((int) Identity);
                    if (syndicate == null)
                        return;

                    Strings.Add(syndicate.Name);
                    await client.Character.SendAsync(this);
                    break;

                case StringAction.MemberList:
                    if (client.Character.Syndicate == null)
                        return;

                    await client.Character.Syndicate.SendMembersAsync((int) Identity, client.Character);
                    break;

                case StringAction.WhisperWindowInfo:
                    if (Strings.Count == 0)
                    {
                        await client.SendAsync(this);
                        return;
                    }

                    targetUser = Kernel.RoleManager.GetUser(Strings[0]);
                    if (targetUser == null)
                    {
                        await client.SendAsync(this);
                        return;
                    }

                    Strings.Add(
                        $"{targetUser.Identity} {targetUser.Level} {targetUser.BattlePower} #{targetUser.SyndicateName} #None {targetUser.MateName} {(int) targetUser.NobilityRank} {targetUser.Gender}");
                    await client.SendAsync(this);
                    break;
            }
        }
    }

    public enum StringAction : byte
    {
        None = 0,
        Fireworks,
        CreateGuild,
        Guild,
        ChangeTitle,
        DeleteRole = 5,
        Mate,
        QueryNpc,
        Wanted,
        MapEffect,
        RoleEffect = 10,
        MemberList,
        KickoutGuildMember,
        QueryWanted,
        QueryPoliceWanted,
        PoliceWanted = 15,
        QueryMate,
        AddDicePlayer,
        DeleteDicePlayer,
        DiceBonus,
        PlayerWave = 20,
        SetAlly,
        SetEnemy,
        WhisperWindowInfo = 26
    }
}