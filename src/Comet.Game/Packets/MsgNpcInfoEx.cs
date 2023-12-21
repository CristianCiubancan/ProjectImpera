// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgNpcInfoEx.cs
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
using Comet.Game.States.NPCs;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgNpcInfoEx : MsgBase<Client>
    {
        public MsgNpcInfoEx()
        {
            Type = PacketType.MsgNpcInfoEx;
        }

        public MsgNpcInfoEx(DynamicNpc npc)
        {
            Type = PacketType.MsgNpcInfoEx;

            Identity = npc.Identity;
            MaxLife = npc.MaxLife;
            Life = npc.Life;
            PosX = npc.MapX;
            PosY = npc.MapY;
            Lookface = (ushort) npc.Mesh;
            NpcType = npc.Type;
            Sort = (ushort) npc.Sort;
            Name = npc.IsSynFlag() ? npc.Name : "";
        }

        public uint Identity { get; set; }
        public uint MaxLife { get; set; }
        public uint Life { get; set; }
        public ushort PosX { get; set; }
        public ushort PosY { get; set; }
        public ushort Lookface { get; set; }
        public ushort NpcType { get; set; }
        public ushort Sort { get; set; }
        public string Name { get; set; }

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
            MaxLife = reader.ReadUInt32();
            Life = reader.ReadUInt32();
            PosX = reader.ReadUInt16();
            PosY = reader.ReadUInt16();
            Lookface = reader.ReadUInt16();
            NpcType = (ushort) reader.ReadUInt32();
            List<string> names = reader.ReadStrings();
            if (names.Count > 0)
                Name = names[0];
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
            writer.Write((ushort)Type); // 2
            writer.Write(Identity); // 4
            writer.Write(MaxLife); // 8
            writer.Write(Life); // 12
            writer.Write(PosX); // 16
            writer.Write(PosY); // 18
            writer.Write(Lookface); // 20
            writer.Write(NpcType); // 22
            writer.Write(Sort); // 24
            if (!string.IsNullOrEmpty(Name))
                writer.Write(new List<string> { Name });
            else writer.Write(0);
            return writer.ToArray();
        }
    }
}