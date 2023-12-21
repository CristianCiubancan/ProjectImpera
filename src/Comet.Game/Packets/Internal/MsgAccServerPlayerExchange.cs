using System;
using System.Collections.Generic;

namespace Comet.Network.Packets.Internal
{
    public abstract class MsgAccServerPlayerExchange<T> : MsgBase<T>
    {
        public string ServerName { get; set; }
        public int Count => Data.Count;
        public List<PlayerData> Data { get; } = new List<PlayerData>();

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new (bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            ServerName = reader.ReadString(16);
            int count = reader.ReadInt32();
           
            for (int i = 0; i < count; i++)
            {
                PlayerData data = new();
                data.Identity = reader.ReadUInt32();
                data.AccountIdentity = reader.ReadUInt32();
                data.Name = reader.ReadString(16);
                data.Level = reader.ReadByte();
                data.Metempsychosis = reader.ReadByte();
                data.Profession = reader.ReadUInt16();
                data.PreviousProfession = reader.ReadUInt16();
                data.FirstProfession = reader.ReadUInt16();
                data.Money = reader.ReadUInt32();
                data.ConquerPoints = reader.ReadUInt32();
                data.ConquerPointsMono = reader.ReadUInt32();
                data.Donation = reader.ReadUInt64();
                data.SyndicateIdentity = reader.ReadUInt32();
                data.SyndicatePosition = reader.ReadUInt16();
                data.FamilyIdentity = reader.ReadUInt32();
                data.FamilyPosition = reader.ReadUInt16();

                data.Force = reader.ReadUInt16();
                data.Speed = reader.ReadUInt16();
                data.Health = reader.ReadUInt16();
                data.Soul = reader.ReadUInt16();
                data.AdditionPoints = reader.ReadUInt16();

                data.LastLogin = reader.ReadInt32();
                data.LastLogout = reader.ReadInt32();
                data.TotalOnlineTime = reader.ReadInt32();

                data.AthletePoints = reader.ReadInt32();
                data.AthleteHistoryWins = reader.ReadInt32();
                data.AthleteHistoryLoses = reader.ReadInt32();
                data.HonorPoints = reader.ReadInt32();

                data.RedRoses = reader.ReadUInt32();
                data.WhiteRoses = reader.ReadUInt32();
                data.Orchids = reader.ReadUInt32();
                data.Tulips = reader.ReadUInt32();

                Data.Add(data);
            }
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgAccServerPlayerExchange);
            writer.Write(ServerName, 16);
            writer.Write(Count);
            foreach (var data in Data)
            {
                writer.Write(data.Identity);
                writer.Write(data.AccountIdentity);
                writer.Write(data.Name, 16);
                writer.Write(data.Level);
                writer.Write(data.Metempsychosis);
                writer.Write(data.Profession);
                writer.Write(data.PreviousProfession);
                writer.Write(data.FirstProfession);
                writer.Write(data.Money);
                writer.Write(data.ConquerPoints);
                writer.Write(data.ConquerPointsMono);
                writer.Write(data.Donation);
                writer.Write(data.SyndicateIdentity);
                writer.Write(data.SyndicatePosition);
                writer.Write(data.FamilyIdentity);
                writer.Write(data.FamilyPosition);

                writer.Write(data.Force);
                writer.Write(data.Speed);
                writer.Write(data.Health);
                writer.Write(data.Soul);
                writer.Write(data.AdditionPoints);

                writer.Write(data.LastLogin);
                writer.Write(data.LastLogout);
                writer.Write(data.TotalOnlineTime);

                writer.Write(data.AthletePoints);
                writer.Write(data.AthleteHistoryWins);
                writer.Write(data.AthleteHistoryLoses);
                writer.Write(data.HonorPoints);

                writer.Write(data.RedRoses);
                writer.Write(data.WhiteRoses);
                writer.Write(data.Orchids);
                writer.Write(data.Tulips);
            }
            return writer.ToArray();
        }

        public struct PlayerData
        {
            public uint Identity; // 0
            public uint AccountIdentity; // 4
            public string Name; // 8
            public byte Level; // 24
            public byte Metempsychosis; // 25
            public ushort Profession; // 26
            public ushort PreviousProfession; // 28
            public ushort FirstProfession; // 30
            public uint Money; // 32
            public uint ConquerPoints; // 36
            public uint ConquerPointsMono; // 40
            public ulong Donation; // 44
            public uint SyndicateIdentity; // 52
            public ushort SyndicatePosition; // 56
            public uint FamilyIdentity; // 58
            public ushort FamilyPosition; // 62

            public ushort Force; // 64
            public ushort Speed; // 66
            public ushort Health; // 68
            public ushort Soul; // 70
            public ushort AdditionPoints; // 72

            public int LastLogin; // 74
            public int LastLogout; // 78
            public int TotalOnlineTime; //82

            public int AthletePoints; // 86
            public int AthleteHistoryWins; // 90
            public int AthleteHistoryLoses; // 94
            public int HonorPoints; // 98

            public uint RedRoses; // 102
            public uint WhiteRoses; // 106
            public uint Orchids; // 110
            public uint Tulips; // 114
        }
    }
}