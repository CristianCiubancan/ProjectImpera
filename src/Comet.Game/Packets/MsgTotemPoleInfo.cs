// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgTotemPoleInfo.cs
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
using Comet.Game.States.Syndicates;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgTotemPoleInfo : MsgBase<Client>
    {
        public int TotemBattlePower { get; set; }
        public int SharedBattlePower { get; set; }
        public int TotemDonation { get; set; }
        public int TotemPoleAmount { get; set; }
        public List<TotemPoleStruct> Items { get; set; } = new List<TotemPoleStruct>();

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgTotemPoleInfo);
            writer.Write(0);
            writer.Write(TotemBattlePower);
            writer.Write(TotemDonation);
            writer.Write(SharedBattlePower);
            writer.Write(TotemPoleAmount = Items.Count);
            foreach (var pole in Items)
            {
                writer.Write((int) pole.Type);
                writer.Write(pole.BattlePower);
                writer.Write(pole.Enhancement);
                writer.Write(pole.Donation);
                writer.Write(pole.Open ? 1 : 0);
            }
            return writer.ToArray();
        }

        public struct TotemPoleStruct
        {
            public Syndicate.TotemPoleType Type;
            public int BattlePower;
            public int Enhancement;
            public long Donation;
            public bool Open;
        }
    }
}