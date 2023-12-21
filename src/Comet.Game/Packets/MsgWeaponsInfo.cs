// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgWeaponsInfo.cs
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
using Comet.Game.States.Items;
using Comet.Game.States.Syndicates;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgWeaponsInfo : MsgBase<Client>
    {
        public uint Action { get; set; }
        public int Data1 { get; set; }
        public int Data2 { get; set; }
        public Syndicate.TotemPoleType TotemType { get; set; }
        public int TotalInscribed { get; set; }
        public int SharedBattlePower { get; set; }
        public int Enhancement { get; set; }
        public uint EnhancementExpiration { get; set; }
        public int Donation { get; set; }
        public int Count { get; set; }

        public List<TotemPoleStruct> Items = new List<TotemPoleStruct>();

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Action = reader.ReadUInt32();
            Data1 = reader.ReadInt32();
            Data2 = reader.ReadInt32();
            TotemType = (Syndicate.TotemPoleType) reader.ReadUInt32();
            TotalInscribed = reader.ReadInt32();
            SharedBattlePower = reader.ReadInt32();
            Enhancement = reader.ReadInt32();
            EnhancementExpiration = reader.ReadUInt32();
            Donation = reader.ReadInt32();
            Count = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgWeaponsInfo);
            writer.Write(Action); // 4
            writer.Write(Data1); // 8
            writer.Write(Data2); // 12
            writer.Write((uint) TotemType); // 16
            writer.Write(TotalInscribed); // 20 
            writer.Write(SharedBattlePower); // 24
            writer.Write(Enhancement); // 28
            writer.Write(EnhancementExpiration); // 32
            writer.Write(Donation); // 36
            writer.Write(Count = Items.Count); // 40
            foreach (var item in Items)
            {
                writer.Write(item.ItemIdentity); // 0
                writer.Write(item.Position); // 4
                writer.Write(item.PlayerName, 16); // 8
                writer.Write(item.Type); // 24
                writer.Write((byte) item.Quality); // 28
                writer.Write(item.Addition); // 29
                writer.Write((byte) item.SocketOne); // 30
                writer.Write((byte) item.SocketTwo); // 31
                writer.Write(item.BattlePower); // 32
                writer.Write(item.Donation); // 36
            }
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            if (user?.Syndicate == null)
                return;

            Syndicate syn = user.Syndicate;

            if (Action == 0)
            {
                if (TotemType == Syndicate.TotemPoleType.None)
                    return;

                await syn.SendTotemsAsync(user, TotemType, Data1);
            }
        }

        public struct TotemPoleStruct
        {
            public uint ItemIdentity { get; set; }
            public int Position { get; set; }
            public string PlayerName { get; set; }
            public uint Type { get; set; }
            public int Quality { get; set; }
            public byte Addition { get; set; }
            public Item.SocketGem SocketOne { get; set; }
            public Item.SocketGem SocketTwo { get; set; }
            public int BattlePower { get; set; }
            public int Donation { get; set; }
        }
    }
}