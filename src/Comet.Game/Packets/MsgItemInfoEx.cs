// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgItemInfoEx.cs
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

using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgItemInfoEx : MsgBase<Client>
    {
        public enum ViewMode : ushort
        {
            None, 
            Silvers,
            Unknown,
            Emoney,
            ViewEquipment
        }

        public MsgItemInfoEx(BoothItem item)
        {
            Type = PacketType.MsgItemInfoEx;

            Identity = item.Identity;
            TargetIdentity = item.Item.PlayerIdentity;
            ItemType = item.Item.Type;
            Amount = item.Item.Durability;
            AmountLimit = item.Item.MaximumDurability;
            Position = Item.ItemPosition.Inventory;
            SocketOne = item.Item.SocketOne;
            SocketTwo = item.Item.SocketTwo;
            Addition = item.Item.Plus;
            Blessing = (byte) item.Item.Blessing;
            Enchantment = item.Item.Enchantment;
            Color = item.Item.Color;
            Mode = item.IsSilver ? ViewMode.Silvers : ViewMode.Emoney;
            Price = item.Value;
            SocketProgress = item.Item.SocketProgress;
            CompositionProgress = item.Item.CompositionProgress;
        }

        public uint Identity;
        public uint TargetIdentity;
        public uint Price;
        public uint ItemType;
        public ushort Amount;
        public ushort AmountLimit;
        public ViewMode Mode;
        public Item.ItemPosition Position;
        public uint SocketProgress;
        public Item.SocketGem SocketOne;
        public Item.SocketGem SocketTwo;
        public byte Addition;
        public byte Blessing;
        public byte Enchantment;
        public Item.ItemColor Color;
        public uint CompositionProgress;

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) Type);
            writer.Write(Identity);
            writer.Write(TargetIdentity);
            writer.Write(Price);
            writer.Write(ItemType);
            writer.Write(Amount);
            writer.Write(AmountLimit);
            writer.Write((ushort) Mode);
            writer.Write((ushort) Position);
            writer.Write(SocketProgress);
            writer.Write((byte) SocketOne);
            writer.Write((byte) SocketTwo);
            writer.Write((ushort) 0);
            writer.Write(Addition);
            writer.Write(Blessing);
            writer.Write(Enchantment);
            writer.Write((byte) 0);
            writer.Write(0UL);
            writer.Write((ushort) Color);
            writer.Write(CompositionProgress);
            return writer.ToArray();
        }
    }
}