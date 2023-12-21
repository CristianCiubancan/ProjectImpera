// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgPlayer.cs
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

#define NO_COLOR

#region References

using System.Collections.Generic;
using System.IO;
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgPlayer : MsgBase<Client>
    {
        public MsgPlayer(Character user)
        {
            Type = PacketType.MsgPlayer;

            Identity = user.Identity;
            Mesh = user.Mesh;

            MapX = user.MapX;
            MapY = user.MapY;

            Status = (uint) user.StatusFlag;

            Hairstyle = user.Hairstyle;
            Direction = (byte) user.Direction;
            Pose = (byte) user.Action;
            Metempsychosis = user.Metempsychosis;
            Level = user.Level;

            SyndicateIdentity = user.SyndicateIdentity;
            SyndicatePosition = (ushort) user.SyndicateRank;

            NobilityRank = (uint) user.NobilityRank;

            Helmet = user.Headgear?.Type ?? 0;
            HelmetColor = (ushort) (user.Headgear?.Color ?? Item.ItemColor.None);
            RightHand = user.RightHand?.Type ?? 0;
            LeftHand = user.LeftHand?.Type ?? 0;
            LeftHandColor = (ushort) (user.LeftHand?.Color ?? Item.ItemColor.None);
            Armor = user.Armor?.Type ?? 0;
            ArmorColor = (ushort) (user.Armor?.Color ?? Item.ItemColor.None);
            Garment = user.Garment?.Type ?? 0;

            FlowerRanking = user.FlowerCharm;
            QuizPoints = user.QuizPoints;
            UserTitle = user.UserTitle;

            Away = user.IsAway;
            
            if (user.Syndicate != null)
                SharedBattlePower = (uint) user.Syndicate.GetSharedBattlePower(user.SyndicateRank);

            FamilyIdentity = user.FamilyIdentity;
            FamilyRank = (uint) user.FamilyPosition;
            FamilyBattlePower = user.FamilyBattlePower;

            Name = user.Name;
            FamilyName = user.FamilyName;
        }

        public MsgPlayer(Monster monster)
        {
            Type = PacketType.MsgPlayer;

            Identity = monster.Identity;

            Mesh = monster.Mesh;

            MapX = monster.MapX;
            MapY = monster.MapY;

            Status = (uint) monster.StatusFlag;

            Direction = (byte) monster.Direction;
            Pose = (byte) monster.Action;

            MonsterLevel = monster.Level;
            if (monster.Life > ushort.MaxValue)
            {
                MonsterLife = (ushort) (ushort.MaxValue / monster.Life * 100);
            }
            else
            {
                MonsterLife = (ushort) monster.Life;
            }

            Name = monster.Name;
            FamilyName = "";
        }

        public uint Identity { get; set; }
        public uint Mesh { get; set; }

        #region Union

        #region Struct

        public uint SyndicateIdentity { get; set; }
        public uint SyndicatePosition { get; set; }

        #endregion

        public uint OwnerIdentity { get; set; }

        #endregion

        #region Union

        public ulong Status { get; set; }

        #region Struct

        public ushort StatuaryLife { get; set; }
        public ushort StatuaryFrame { get; set; }

        #endregion

        #endregion
       
        public uint Garment { get; set; }
        public uint Helmet { get; set; }
        public uint Armor { get; set; }
        public uint RightHand { get; set; }
        public uint LeftHand { get; set; }
        public uint Mount { get; set; }

        public uint Padding0 { get; set; }

        public ushort MonsterLife { get; set; }
        public ushort MonsterLevel { get; set; }

        public ushort MapX { get; set; }
        public ushort MapY { get; set; }
        public ushort Hairstyle { get; set; }
        public byte Direction { get; set; }
        public byte Pose { get; set; }
        public ushort Metempsychosis { get; set; }
        public ushort Level { get; set; }
        public bool WindowSpawn { get; set; }
        public bool Away { get; set; }
        public uint SharedBattlePower { get; set; }
        public uint FlowerRanking { get; set; }

        public uint NobilityRank { get; set; }

        public ushort Padding2 { get; set; }

        public ushort HelmetColor { get; set; }
        public ushort ArmorColor { get; set; }
        public ushort LeftHandColor { get; set; }
        public uint QuizPoints { get; set; }

        public byte MountAddition { get; set; }
        public uint MountColor { get; set; }
        public ushort EnlightenPoints { get; set; }

        public uint FamilyIdentity { get; set; }
        public uint FamilyRank { get; set; }
        public int FamilyBattlePower { get; set; }

        public uint UserTitle { get; set; }

        public string Name { get; set; }
        public string FamilyName { get; set; }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) Type);
            writer.Write(Mesh); // 4
            writer.Write(Identity); // 8

            if (OwnerIdentity > 0)
            {
                writer.Write(OwnerIdentity); // 12
                writer.Write(0); // 16
            }
            else
            {
                writer.Write(SyndicateIdentity); // 12
                writer.Write(SyndicatePosition); // 16
            }

            writer.Write((ushort)0);

            if (StatuaryLife > 0)
            {
                writer.Write(StatuaryLife); // 24
                writer.Write(StatuaryFrame); // 26
                writer.Write(0u); // 28
            }
            else
            {
                writer.Write(Status); // 24
            }

            writer.Write(Helmet); // 30
            writer.Write(Garment); // 34
            writer.Write(Armor); // 38
            writer.Write(RightHand); // 42
            writer.Write(LeftHand); // 46
            writer.Write(Mount); // 50
            writer.Write(Padding0); // 54
            writer.Write(MonsterLife); // 58
            writer.Write(MonsterLevel); // 60
            writer.Write(Hairstyle); // 62
            writer.Write(MapX); // 64
            writer.Write(MapY); // 66
            writer.Write(Direction); // 68
            writer.Write(Pose); // 69
            writer.BaseStream.Seek(4, SeekOrigin.Current);
            writer.Write((byte) Metempsychosis); // 73
            writer.Write(Level); // 74
            writer.Write(WindowSpawn); // 76
            writer.Write(Away); // 77
            writer.Write(SharedBattlePower); // 78
            writer.BaseStream.Seek(8, SeekOrigin.Current); // 82
            writer.Write(FlowerRanking); // 91
            writer.Write(NobilityRank); // 95
            writer.Write(ArmorColor); // 99
            writer.Write(LeftHandColor); // 101
            writer.Write(HelmetColor); // 103
            writer.Write(QuizPoints); // 105
            writer.Write(MountAddition); // 107
            writer.Write(0); // 108
            writer.Write(MountColor); // 112
            writer.Write((byte) 0); // 116
            writer.Write(EnlightenPoints); // 117
            writer.BaseStream.Seek(10, SeekOrigin.Current); // 119
            writer.Write(FamilyIdentity); // 129
            writer.Write(FamilyRank); // 133
            writer.Write(FamilyBattlePower); // 137
            writer.Write(UserTitle); // 141
            writer.BaseStream.Seek(8, SeekOrigin.Current); // 145
            writer.Write(new List<string> // 95
            {
                Name,
                FamilyName
            });

            return writer.ToArray();
        }
    }
}