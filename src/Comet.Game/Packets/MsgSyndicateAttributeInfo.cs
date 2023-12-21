// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgSyndicateAttributeInfo.cs
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

using System.IO;
using Comet.Game.States;
using Comet.Game.States.Syndicates;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgSyndicateAttributeInfo : MsgBase<Client>
    {
        public MsgSyndicateAttributeInfo()
        {
            Type = PacketType.MsgSyndicateAttributeInfo;
            LeaderName = Language.StrNone;
        }

        public uint Identity { get; set; }
        public int PlayerDonation { get; set; }
        public long Funds { get; set; }
        public uint ConquerPointsFunds { get; set; }
        public int MemberAmount { get; set; }
        public SyndicateMember.SyndicateRank Rank { get; set; }
        public string LeaderName { get; set; }
        public int ConditionLevel { get; set; }
        public int ConditionMetempsychosis { get; set; }
        public int ConditionProfession { get; set; }
        public byte Level { get; set; }
        public uint PositionExpiration { get; set; }
        public uint EnrollmentDate { get; set; }

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
            writer.Write(Identity); // 4
            writer.Write(PlayerDonation); // 8
            writer.Write(Funds); // 12
            writer.Write(ConquerPointsFunds); // 20
            writer.Write(MemberAmount); // 24
            writer.Write((uint) Rank); // 28
            writer.Write(LeaderName, 16); // 32
            writer.Write(ConditionLevel); // 48
            writer.Write(ConditionMetempsychosis); // 52
            writer.Write(ConditionProfession); // 56
            writer.Write(Level); // 60
            writer.BaseStream.Seek(2, SeekOrigin.Current); // 61
            writer.Write(PositionExpiration); // 63
            writer.Write(0); // 67
            writer.Write(EnrollmentDate);
            return writer.ToArray();
        }
    }
}