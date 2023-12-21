// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgGuideInfo.cs
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
    public sealed class MsgGuideInfo : MsgBase<Client>
    {
        public enum RequestMode
        {
            None,
            Mentor,
            Apprentice
        }

        public MsgGuideInfo()
        {
            Type = PacketType.MsgGuideInfo;
        }

        public RequestMode Mode;
        public uint SenderIdentity;
        public uint Identity;
        public uint Mesh;
        public uint SharedBattlePower;
        public uint Unknown24;
        public uint EnroleDate;
        public byte Level;
        public byte Profession;
        public ushort PkPoints;
        public ushort Syndicate;
        public byte Unknown38;
        public SyndicateMember.SyndicateRank SyndicatePosition;
        public ulong Unknown40;
        public bool IsOnline;
        public byte[] Fill41 = new byte[3];
        public uint Unknown52;
        public ulong Experience;
        public ushort Blessing;
        public ushort Composition;
        public List<string> Names = new List<string>();

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Mode = (RequestMode) reader.ReadUInt32();
            SenderIdentity = reader.ReadUInt32();
            Identity = reader.ReadUInt32();
            Mesh = reader.ReadUInt32();
            SharedBattlePower = reader.ReadUInt32();
            Unknown24 = reader.ReadUInt32();
            EnroleDate = reader.ReadUInt32();
            Level = reader.ReadByte();
            Profession = reader.ReadByte();
            PkPoints = reader.ReadUInt16();
            Syndicate = reader.ReadUInt16();
            Unknown38 = reader.ReadByte();
            SyndicatePosition = (SyndicateMember.SyndicateRank) reader.ReadByte();
            Unknown40 = reader.ReadUInt64();
            IsOnline = reader.ReadBoolean();
            Fill41 = reader.ReadBytes(Fill41.Length);
            Unknown52 = reader.ReadUInt32();
            Experience = reader.ReadUInt64();
            Blessing = reader.ReadUInt16();
            Composition = reader.ReadUInt16();
            Names = reader.ReadStrings();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) Type);
            writer.Write((int) Mode);
            writer.Write(SenderIdentity);
            writer.Write(Identity);
            writer.Write(Mesh);
            writer.Write(SharedBattlePower);
            writer.Write(Unknown24);
            writer.Write(EnroleDate);
            writer.Write(Level);
            writer.Write(Profession);
            writer.Write(PkPoints);
            writer.Write(Syndicate);
            writer.Write(Unknown38);
            writer.Write((byte) SyndicatePosition);
            writer.Write(Unknown40);
            writer.Write(0);
            writer.Write(0);
            writer.Write(IsOnline);
            writer.Write(Fill41);
            writer.Write(Unknown52);
            writer.Write(Experience);
            writer.Write(Blessing);
            writer.Write(Composition);
            writer.Write(Names);
            return writer.ToArray();
        }
    }
}