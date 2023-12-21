// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgSynpOffer.cs
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

using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.States.Syndicates;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgSynpOffer : MsgBase<Client>
    {
        public MsgSynpOffer()
        {
            
        }

        public MsgSynpOffer(SyndicateMember member)
        {
            Identity = 0;
            Silver = member.Silvers / 10000;
            ConquerPoints = member.ConquerPointsDonation * 20;
            GuideDonation = member.GuideDonation;
            PkDonation = member.PkDonation;
            ArsenalDonation = member.ArsenalDonation;
            RedRoseDonation = member.RedRoseDonation;
            WhiteRoseDonation = member.WhiteRoseDonation;
            OrchidDonation = member.OrchidDonation;
            TulipDonation = member.TulipDonation;
            SilverTotal = (uint) (member.SilversTotal / 10000);
            ConquerPointsTotal = member.ConquerPointsTotalDonation * 20;
            GuideTotal = member.GuideDonation;
            PkTotal = member.PkTotalDonation;
        }

        public uint Identity { get; set; }
        public int Silver { get; set; }
        public uint ConquerPoints { get; set; }
        public uint GuideDonation { get; set; }
        public int PkDonation { get; set; }
        public uint ArsenalDonation { get; set; }
        public uint RedRoseDonation { get; set; }
        public uint WhiteRoseDonation { get; set; }
        public uint OrchidDonation { get; set; }
        public uint TulipDonation { get; set; }
        public uint SilverTotal { get; set; }
        public uint ConquerPointsTotal { get; set; }
        public uint GuideTotal { get; set; }
        public int PkTotal { get; set; }

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            Silver = reader.ReadInt32();
            ConquerPoints = reader.ReadUInt32();
            GuideDonation = reader.ReadUInt32();
            ArsenalDonation = reader.ReadUInt32();
            RedRoseDonation = reader.ReadUInt32();
            WhiteRoseDonation = reader.ReadUInt32();
            OrchidDonation = reader.ReadUInt32();
            TulipDonation = reader.ReadUInt32();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgSynpOffer);
            writer.Write(Identity); // 4
            writer.Write(Silver); // 8
            writer.Write(ConquerPoints); // 12
            writer.Write(GuideDonation); // 16
            writer.Write(PkDonation); // 20
            writer.Write(ArsenalDonation); // 24
            writer.Write(RedRoseDonation); // 28
            writer.Write(WhiteRoseDonation); // 32
            writer.Write(OrchidDonation); // 36
            writer.Write(TulipDonation); // 40
            writer.Write(SilverTotal); // 44 Total Silver
            writer.Write(ConquerPointsTotal); // 48 Total Emoney
            writer.Write(GuideTotal); // 52 Guide
            writer.Write(PkTotal); // 56 PK
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            if (user == null)
                return;

            if (user.SyndicateIdentity > 0)
                await user.SendAsync(new MsgSynpOffer(user.SyndicateMember));
        }
    }
}