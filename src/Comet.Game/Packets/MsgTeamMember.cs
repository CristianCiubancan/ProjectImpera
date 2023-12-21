// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgTeammate.cs
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
using Comet.Game.States;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgTeamMember : MsgBase<Client>
    {
        public const byte ADD_MEMBER_B = 0, DEL_MEMBER_B = 1;

        public struct TeamMember
        {
            public string Name { get; set; }
            public uint Identity { get; set; }
            public uint Lookface { get; set; }
            public ushort MaxLife { get; set; }
            public ushort Life { get; set; }
        }

        public MsgTeamMember()
        {
            Type = PacketType.MsgTeamMember;
            // Unknown1 = 0; // ?????
        }

        public byte Action { get; set; }
        public byte Count { get; set; }
        public byte Unknown0 { get; set; }
        public byte Unknown1 { get; set; }
        public List<TeamMember> Members = new List<TeamMember>();

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
            Action = reader.ReadByte();
            Count = reader.ReadByte();
            Unknown0 = reader.ReadByte();
            Unknown1 = reader.ReadByte();
            for (int i = 0; i < Count; i++)
            {
                Members.Add(new TeamMember
                {
                    Name = reader.ReadString(16),
                    Identity = reader.ReadUInt32(),
                    Lookface = reader.ReadUInt32(),
                    MaxLife = reader.ReadUInt16(),
                    Life = reader.ReadUInt16()
            });
            }
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
            writer.Write(Action);
            writer.Write((byte) Members.Count);
            writer.Write(Unknown0);
            writer.Write(Unknown1);
            foreach (var member in Members)
            {
                writer.Write(member.Name, 16);
                writer.Write(member.Identity);
                writer.Write(member.Lookface);
                writer.Write(member.MaxLife);
                writer.Write(member.Life);
            }
            return writer.ToArray();
        }
    }
}