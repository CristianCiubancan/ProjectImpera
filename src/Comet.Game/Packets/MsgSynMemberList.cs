// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgSynMemberList.cs
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
using Comet.Game.States.Syndicates;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgSynMemberList : MsgBase<Client>
    {
        public uint SubType { get; set; }
        public int Index { get; set; }
        public int Amount { get; set; }
        public List<MemberStruct> Members { get; set; } = new List<MemberStruct>();

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            SubType = reader.ReadUInt32();
            Index = reader.ReadInt32();
            Amount = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgSynMemberList);
            writer.Write(SubType);
            writer.Write(Index);
            writer.Write(Amount = Members.Count);
            foreach (var member in Members)
            {
                writer.Write(member.Name, 16);
                writer.Write(0);
                writer.Write((int)((int)member.Nobility * 10 + member.LookFace % 10000 / 1000));
                writer.Write(member.Level);
                writer.Write((uint) member.Rank);
                writer.Write(member.PositionExpire);
                writer.Write(member.TotalDonation);
                writer.Write(member.IsOnline ? 1 : 0);
                writer.Write(0); // test
            }
            return writer.ToArray();
        }

        public override Task ProcessAsync(Client client)
        {
            if (client.Character?.Syndicate == null)
                return Task.CompletedTask;

            return client.Character.Syndicate.SendMembersAsync(Index, client.Character);
        }

        public struct MemberStruct
        {
            public uint Identity { get; set; }
            public uint LookFace { get; set; }
            public string Name { get; set; }
            public int Level { get; set; }
            public NobilityRank Nobility { get; set; }
            public SyndicateMember.SyndicateRank Rank { get; set; }
            public uint PositionExpire { get; set; }
            public int TotalDonation { get; set; }
            public bool IsOnline { get; set; }
        }
    }
}