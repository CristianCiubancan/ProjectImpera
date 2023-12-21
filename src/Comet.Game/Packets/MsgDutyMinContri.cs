// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgDutyMinContri.cs
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
    public sealed class MsgDutyMinContri : MsgBase<Client>
    {
        public ushort Action { get; set; }
        public ushort Count { get; set; }

        public List<MinContriStruct> Members = new List<MinContriStruct>();

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgDutyMinContri);
            writer.Write(Action);
            writer.Write(Count = (ushort) Members.Count);
            foreach (var member in Members)
            {
                writer.Write((int) member.Position);
                writer.Write(member.Donation);
            }
            return writer.ToArray();
        }

        public struct MinContriStruct
        {
            public SyndicateMember.SyndicateRank Position;
            public uint Donation;
        }
    }
}