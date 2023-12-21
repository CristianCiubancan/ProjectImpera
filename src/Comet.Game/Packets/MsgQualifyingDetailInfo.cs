// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgQualifyingDetailInfo.cs
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

using System;
using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgQualifyingDetailInfo : MsgBase<Client>
    {
        public int Ranking { get; set; }
        public int Unknown { get; set; }
        public ArenaStatus Status { get; set; }
        public uint Activity { get; set; }
        public byte TriumphToday20 { get; set; }
        public byte TriumphToday9 { get; set; }
        public ushort Fill { get; set; }
        public uint TotalWins { get; set; }
        public uint TotalLoses { get; set; }
        public uint TodayWins { get; set; }
        public uint TodayLoses { get; set; }
        public uint CurrentHonor { get; set; }
        public uint HistoryHonor { get; set; }
        public uint Points { get; set; }

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Ranking = reader.ReadInt32();
            Unknown = reader.ReadInt32();
            Status = (ArenaStatus) reader.ReadInt32();
            Activity = reader.ReadUInt32();
            TriumphToday20 = reader.ReadByte();
            TriumphToday9 = reader.ReadByte();
            Fill = reader.ReadUInt16();
            TotalWins = reader.ReadUInt32();
            TotalLoses = reader.ReadUInt32();
            TodayWins = reader.ReadUInt32();
            TodayLoses = reader.ReadUInt32();
            HistoryHonor = reader.ReadUInt32();
            CurrentHonor = reader.ReadUInt32();
            Points = reader.ReadUInt32();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgQualifyingDetailInfo);
            writer.Write(Ranking); // 4
            writer.Write(0); // 8
            writer.Write((int) Status); // 12
            writer.Write(Activity); // Activity
            writer.Write(TriumphToday20); // 20 Triumph Today 20
            writer.Write(TriumphToday9); // 21 Triumph Today 9
            writer.Write(Fill); // 22
            writer.Write(TotalWins); // 24
            writer.Write(TotalLoses); // 28
            writer.Write(TodayWins); // Season Wins
            writer.Write(TodayLoses); // Season Loss
            writer.Write(HistoryHonor); // History Honor
            writer.Write(CurrentHonor); // 44
            writer.Write(Points); // 48
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            if (user == null)
                return;

            Ranking = user.QualifierRank;
            Status = user.QualifierStatus;
            TodayWins = user.QualifierDayWins;
            TodayLoses = user.QualifierDayLoses;
            TotalWins = user.QualifierHistoryWins;
            TotalLoses = user.QualifierHistoryLoses;
            HistoryHonor = user.HistoryHonorPoints;
            CurrentHonor = user.HonorPoints;
            Points = user.QualifierPoints;
            TriumphToday20 = (byte) Math.Min(20, user.QualifierDayGames);
            TriumphToday9 = (byte)Math.Min(9, user.QualifierDayWins);
            await client.SendAsync(this);
        }

        public enum ArenaStatus
        {
            NotSignedUp,
            WaitingForOpponent,
            WaitingInactive
        }
    }
}