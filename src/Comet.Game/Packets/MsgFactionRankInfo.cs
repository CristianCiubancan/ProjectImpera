// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgFactionRankInfo.cs
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
    public sealed class MsgFactionRankInfo : MsgBase<Client>
    {
        public const ushort MAX_COUNT = 10;

        public RankRequestType DonationType { get; set; }
        public ushort Count { get; set; }
        public ushort MaxCount { get; set; } = MAX_COUNT;
        public List<MemberListInfoStruct> Members = new List<MemberListInfoStruct>();

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            DonationType = (RankRequestType) reader.ReadUInt16();
            Count = reader.ReadUInt16();
            MaxCount = reader.ReadUInt16();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgFactionRankInfo);
            writer.Write((ushort) DonationType);
            writer.Write(Count = (ushort) Members.Count);
            writer.Write((int) MAX_COUNT);
            writer.Write(0);
            foreach (var member in Members)
            {
                //writer.Write(member.PlayerIdentity); // 0
                writer.Write((uint)member.Rank); // 4
                writer.Write(member.Position); // 8
                writer.Write(member.Silvers); // 12
                writer.Write(member.ConquerPoints); // 16
                writer.Write(member.PkDonation); // 20
                writer.Write(member.GuideDonation); // 24
                writer.Write(member.ArsenalDonation); // 28
                writer.Write(member.RedRose); // 32
                writer.Write(member.WhiteRose); // 36
                writer.Write(member.Orchid); // 40
                writer.Write(member.Tulip); // 44
                writer.Write(member.TotalDonation); // 48
                writer.Write(member.UsableDonation);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(member.PlayerName, 16); // 52
                writer.Write(0);
            }
            return writer.ToArray();
        }
            
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            Syndicate syn = user?.Syndicate;
            if (syn == null)
                return;

            List<SyndicateMember> members = syn.QueryRank(DonationType);
            for (int i = 0; i < MAX_COUNT && i < members.Count; i++)
            {
                var member = members[i];
                Members.Add(new MemberListInfoStruct
                {
                    PlayerIdentity = member.UserIdentity,
                    PlayerName = member.UserName,
                    Silvers = member.Silvers / 10000,
                    ConquerPoints = member.ConquerPointsDonation * 20,
                    GuideDonation = member.GuideDonation,
                    PkDonation = member.PkDonation,
                    ArsenalDonation = member.ArsenalDonation,
                    RedRose = member.RedRoseDonation,
                    WhiteRose = member.WhiteRoseDonation,
                    Orchid = member.OrchidDonation,
                    Tulip = member.TulipDonation,
                    TotalDonation = (uint) member.TotalDonation,
                    UsableDonation = member.UsableDonation,
                    Position = i,
                    Rank = member.Rank
                });
            }

            await client.SendAsync(this);
        }

        public struct MemberListInfoStruct
        {
            public uint PlayerIdentity { get; set; }
            public SyndicateMember.SyndicateRank Rank { get; set; }
            public int Position { get; set; }
            public int Silvers { get; set; }
            public uint ConquerPoints { get; set; }
            public int PkDonation { get; set; }
            public uint GuideDonation { get; set; }
            public uint ArsenalDonation { get; set; }
            public uint RedRose { get; set; }
            public uint WhiteRose { get; set; }
            public uint Orchid { get; set; }
            public uint Tulip { get; set; }
            public uint TotalDonation { get; set; }
            public int UsableDonation { get; set; }
            public string PlayerName { get; set; }
        }

        public enum RankRequestType
        {
            Silvers,
            ConquerPoints,
            Guide,
            PK,
            Arsenal,
            RedRose,
            Orchid,
            WhiteRose,
            Tulip,
            Usable,
            Total
        }
    }
}