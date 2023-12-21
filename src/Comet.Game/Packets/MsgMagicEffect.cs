// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgMagicEffect.cs
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
using Comet.Game.States;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgMagicEffect : MsgBase<Client>
    {
        public MsgMagicEffect()
        {
            Type = PacketType.MsgMagicEffect;
        }

        public uint AttackerIdentity { get; set; }
        public ushort MapX { get; set; }
        public ushort MapY { get; set; }
        public ushort MagicIdentity { get; set; }
        public ushort MagicLevel { get; set; }
        public ushort Count { get; set; }
        private List<MagicTarget> Targets = new List<MagicTarget>();

        public void Append(uint idTarget, int damage, bool showValue)
        {
            Count++;
            Targets.Add(new MagicTarget
            {
                Identity = idTarget, 
                Damage = damage,
                Show = showValue ? 1 : 0
            });
        }

        public void ClearTargets()
        {
            Count = 0;
            Targets.Clear();
        }

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
            AttackerIdentity = reader.ReadUInt32();
            MapX = reader.ReadUInt16();
            MapY = reader.ReadUInt16();
            MagicIdentity = reader.ReadUInt16();
            MagicLevel = reader.ReadUInt16();
            Count = reader.ReadUInt16();
            for (int i = 0; i < Count; i++)
            {
                Targets.Add(new MagicTarget
                {
                    Identity = reader.ReadUInt32(),
                    Damage = reader.ReadInt32(),
                    Show = reader.ReadInt32()
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
            writer.Write(AttackerIdentity);
            writer.Write(MapX);
            writer.Write(MapY);
            writer.Write(MagicIdentity);
            writer.Write(MagicLevel);
            writer.Write((uint) Count);
            foreach (var target in Targets)
            {
                writer.Write(target.Identity);
                writer.Write(target.Damage);
                writer.Write(target.Show);
            }
            return writer.ToArray();
        }
        
        private struct MagicTarget
        {
            public uint Identity;
            public int Damage;
            public int Show;
        }
    }
}