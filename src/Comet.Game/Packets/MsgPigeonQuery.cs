// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgPigeonQuery.cs
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
    public sealed class MsgPigeonQuery : MsgBase<Client>
    {
        public MsgPigeonQuery()
        {
            Type = PacketType.MsgPigeonQuery;
        }

        public uint Mode { get; set; }
        public ushort Total { get; set; }
        public ushort Count { get; set; }
        public List<PigeonMessage> Messages = new List<PigeonMessage>();

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Mode = reader.ReadUInt32();
            Total = reader.ReadUInt16();
            Count = reader.ReadUInt16();

            for (int i = 0; i < Count; i++)
            {
                PigeonMessage message = new PigeonMessage();
                message.Identity = reader.ReadUInt32();
                message.Position = reader.ReadUInt32();
                message.UserIdentity = reader.ReadUInt32();
                message.UserName = reader.ReadString(16);
                message.Addition = reader.ReadUInt32();
                message.Message = reader.ReadString(80);
                Messages.Add(message);
            }
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) Type);
            writer.Write(Mode);
            writer.Write(Total);
            writer.Write(Count = (ushort) Messages.Count);

            foreach (var message in Messages)
            {
                writer.Write(message.Identity);
                writer.Write(message.Position);
                writer.Write(message.UserIdentity);
                writer.Write(message.UserName, 16);
                writer.Write(message.Addition);
                writer.Write(message.Message, 80);
            }

            return writer.ToArray();
        }

        public struct PigeonMessage
        {
            public uint Identity { get; set; }
            public uint Position { get; set; }
            public uint UserIdentity { get; set; }
            public string UserName { get; set; }
            public uint Addition { get; set; }
            public string Message { get; set; }
        }
    }
}