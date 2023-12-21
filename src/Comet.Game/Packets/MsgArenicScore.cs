// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgArenicScore.cs
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
    public sealed class MsgArenicScore : MsgBase<Client>
    {
        public uint Identity1 { get; set; }
        public string Name1 { get; set; }
        public int Damage1 { get; set; }

        public uint Identity2 { get; set; }
        public string Name2 { get; set; }
        public int Damage2 { get; set; }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgArenicScore);
            writer.Write(Identity1);
            writer.Write(Name1, 16);
            writer.Write(Damage1);
            writer.Write(Identity2);
            writer.Write(Name2, 16);
            writer.Write(Damage2);
            return writer.ToArray();
        }
    }
}