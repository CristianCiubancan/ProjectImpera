// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgItemInfo.cs
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
using Comet.Game.States.Items;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgItemInfo : MsgBase<Client>
    {
        public MsgItemInfo(Item item, ItemMode mode = ItemMode.Default)
        {
            Type = PacketType.MsgItemInfo;

            if (mode == ItemMode.View)
            {
                Identity = item.PlayerIdentity;
            }
            else
            {
                Identity = item.Identity;
            }
                
            Itemtype = item.Type;
            Amount = item.Durability;
            AmountLimit = item.MaximumDurability;
            Mode = mode;
            Position = item.Position;
            SocketProgress = item.SocketProgress;
            SocketOne = item.SocketOne;
            SocketTwo = item.SocketTwo;
            Effect = item.Effect;

            if (item.GetItemSubType() == 730)
            {
                Plus = (byte) (item.Type % 100);
            }
            else
            {
                Plus = item.Plus;
            }

            Bless = (byte) item.Blessing;
            Enchantment = item.Enchantment;
            Color = item.Color;
            IsLocked = item.IsLocked() || item.IsUnlocking();
            IsBound = item.IsBound;
            CompositionProgress = item.CompositionProgress;
            Inscribed = item.SyndicateIdentity != 0;
            AntiMonster = item.AntiMonster;
        }

        public uint Identity { get; set; }
        public uint Itemtype { get; set; }
        public ushort Amount { get; set; }
        public ushort AmountLimit { get; set; }
        public ItemMode Mode { get; set; }
        public Item.ItemPosition Position { get; set; }
        public uint SocketProgress { get; set; }
        public Item.SocketGem SocketOne { get; set; }
        public Item.SocketGem SocketTwo { get; set; }
        public Item.ItemEffect Effect { get; set; }
        public byte Plus { get; set; }
        public byte Bless { get; set; }
        public byte Enchantment { get; set; }
        public int AntiMonster { get; set; }
        public bool IsSuspicious { get; set; }
        public Item.ItemColor Color { get; set; }
        public bool IsLocked { get; set; }
        public bool IsBound { get; set; }
        public uint CompositionProgress { get; set; }
        public bool Inscribed { get; set; }

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
            writer.Write(Identity); // 4
            writer.Write(Itemtype); // 8
            writer.Write(Amount); // 12
            writer.Write(AmountLimit); // 14
            writer.Write((ushort) Mode); // 16
            writer.Write((ushort) Position); // 18
            writer.Write(SocketProgress); // 20
            writer.Write((byte) SocketOne); // 24
            writer.Write((byte) SocketTwo); // 25
            writer.Write((byte) Effect); // 26
            writer.Write((byte) 0); // 27
            writer.Write(Plus); // 28
            writer.Write(Bless); // 29
            writer.Write(IsBound); // 30
            writer.Write(Enchantment); // 31
            writer.Write(AntiMonster); // 32
            writer.Write(IsSuspicious); // 36
            writer.Write((byte) 0); // 37
            writer.Write(IsLocked); // 38
            writer.Write((byte) 0); // 39
            writer.Write((int) Color); // 40
            writer.Write(CompositionProgress); // 44
            writer.BaseStream.Seek(2, SeekOrigin.Current);
            writer.Write(Inscribed ? 1 : 0);
            return writer.ToArray();
        }

        public enum ItemMode : ushort
        {
            Default = 0x01,
            Trade = 0x02,
            Update = 0x03,
            View = 0x04
        }
    }
}