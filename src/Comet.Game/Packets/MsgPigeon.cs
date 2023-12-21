// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgPigeon.cs
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
using System.Linq;
using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgPigeon : MsgBase<Client>
    {
        public enum PigeonMode
        {
            None,
            Query,
            QueryUser,
            Send,
            SuperUrgent,
            Urgent
        }

        public MsgPigeon()
        {
            Type = PacketType.MsgPigeon;
        }

        public PigeonMode Mode { get; set; }
        public int Param { get; set; }
        public List<string> Strings = new List<string>();

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Mode = (PigeonMode) reader.ReadInt32();
            Param = reader.ReadInt32();
            Strings = reader.ReadStrings();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) Type);
            writer.Write((int) Mode);
            writer.Write(Param);
            writer.Write(Strings);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            switch (Mode)
            {
                case PigeonMode.Query:
                case PigeonMode.QueryUser:
                    await Kernel.PigeonManager.SendListAsync(client.Character, Mode);
                    break;
                case PigeonMode.Send:
                    await Kernel.PigeonManager.PushAsync(client.Character,Strings.FirstOrDefault());
                    await Kernel.PigeonManager.SendListAsync(client.Character, PigeonMode.Query);
                    break;
                case PigeonMode.SuperUrgent:
                case PigeonMode.Urgent:
                    await Kernel.PigeonManager.AdditionAsync(client.Character, this);
                    await Kernel.PigeonManager.SendListAsync(client.Character, PigeonMode.Query);
                    break;
            }
        }
    }
}