// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgSynMemberInfo.cs
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
    public sealed class MsgSynMemberInfo : MsgBase<Client>
    {
        public MsgSynMemberInfo()
        {
            Type = PacketType.MsgSyndicate;
        }

        public int Donation { get; set; }
        public SyndicateMember.SyndicateRank Position { get; set; }
        public string Name { get; set; }

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Donation = reader.ReadInt16();
            Position = (SyndicateMember.SyndicateRank) reader.ReadByte();
            Name = reader.ReadString(16);
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) Type);
            writer.Write(Donation);
            writer.Write((byte) Position);
            writer.Write(Name, 16);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            if (user.SyndicateIdentity == 0)
                return;

            var target = user.Syndicate.QueryMember(Name);
            if (target == null)
            {
                await user.SendAsync(this);
                return;
            }

            Donation = target.Silvers;
            Position = target.Rank;
            await user.SendAsync(this);
        }
    }
}