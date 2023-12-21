using Comet.Game.Database.Models;
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Network.Packets;
using Comet.Shared;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Comet.Game.Packets
{
    public sealed class MsgDetainItemInfo : MsgBase<Client>
    {
        public const int MAX_REDEEM_DAYS = 7;
        public const int MAX_REDEEM_SECONDS = 60 * 60 * 24 * MAX_REDEEM_DAYS;

        public MsgDetainItemInfo(DbDetainedItem dbDetainItem, Item item, Mode mode)
        {
            Identity = dbDetainItem.Identity;
            ItemIdentity = item?.Identity ?? 0;
            ItemType = item?.Type ?? 0;
            Amount = item?.Durability ?? 0;
            AmountLimit = item?.MaximumDurability ?? 0;
            Action = mode;
            SocketProgress = item?.SocketProgress ?? 0;
            SocketOne = item?.SocketOne ?? Item.SocketGem.NoSocket;
            SocketTwo = item?.SocketTwo ?? Item.SocketGem.NoSocket;
            Effect = item?.Effect ?? Item.ItemEffect.None;
            Addition = item?.Plus ?? 0;
            Blessing = (byte)(item?.Blessing ?? 0);
            Bound = item?.IsBound ?? false;
            Enchantment = item?.Enchantment ?? 0;
            Suspicious = item?.IsSuspicious() ?? false;
            Locked = item?.IsLocked() ?? false;
            Color = item?.Color ?? Item.ItemColor.Orange;
            if (Action == Mode.ClaimPage)
            {
                OwnerIdentity = dbDetainItem.HunterIdentity;
                OwnerName = dbDetainItem.HunterName;
                TargetIdentity = dbDetainItem.TargetIdentity;
                TargetName = dbDetainItem.TargetName;
            }
            else if (Action == Mode.DetainPage)
            {
                OwnerIdentity = dbDetainItem.TargetIdentity;
                OwnerName = dbDetainItem.TargetName;
                TargetIdentity = dbDetainItem.HunterIdentity;
                TargetName = dbDetainItem.HunterName;
            }
            DetainDate = int.Parse(UnixTimestamp.ToDateTime(dbDetainItem.HuntTime).ToString("yyyyMMdd"));
            Cost = dbDetainItem.RedeemPrice;
            if (item != null)
            {
                Expired = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss")) - long.Parse(UnixTimestamp.ToDateTime(dbDetainItem.HuntTime).ToString("yyyyMMddHHmmss")) > MAX_REDEEM_DAYS * 1000000;
                RemainingDays = Math.Max(0, int.Parse(DateTime.Now.ToString("yyyyMMdd")) - int.Parse(UnixTimestamp.ToDateTime(dbDetainItem.HuntTime).ToString("yyyyMMdd")));
            }
            else
            {
                Expired = true;
                RemainingDays = 0;
            }
        }

        public uint Identity { get; set; }
        public uint ItemIdentity { get; set; }
        public uint ItemType { get; set; }
        public ushort Amount { get; set; }
        public ushort AmountLimit { get; set; }
        public Mode Action { get; set; }
        public uint SocketProgress { get; set; }
        public Item.SocketGem SocketOne { get; set; }
        public Item.SocketGem SocketTwo { get; set; }
        public Item.ItemEffect Effect { get; set; }
        public byte Addition { get; set; }
        public byte Blessing { get; set; }
        public bool Bound { get; set; }
        public byte Enchantment { get; set; }
        public bool Suspicious { get; set; }
        public bool Locked { get; set; }
        public Item.ItemColor Color { get; set; }
        public uint OwnerIdentity { get; set; }
        public string OwnerName { get; set; }
        public uint TargetIdentity { get; set; }
        public string TargetName { get; set; }
        public int DetainDate { get; set; }
        public bool Expired { get; set; }
        public int Cost { get; set; }
        public int RemainingDays { get; set; }

        public override byte[] Encode()
        {
            PacketWriter writer = new ();
            writer.Write((ushort) PacketType.MsgDetainItemInfo);
            writer.Write(Identity); // 4
            writer.Write(ItemIdentity); // 8
            writer.Write(ItemType); // 12
            writer.Write(Amount); // 16
            writer.Write(AmountLimit); // 18
            writer.Write((int)Action); // 20
            writer.Write(SocketProgress); // 24
            writer.Write((byte)SocketOne); // 28
            writer.Write((byte)SocketTwo); // 29
            writer.Write(new byte[2]); // 30
            writer.Write(Addition); // 32
            writer.Write(Blessing); // 33
            writer.Write(Bound); // 34 
            writer.Write(Enchantment); // 35
            writer.Write(0); // 36
            writer.Write((ushort) (Suspicious ? 1 : 0)); // 40
            writer.Write((ushort) (Locked? 1 : 0)); // 42
            writer.Write((int) Color); // 44
            writer.Write(OwnerIdentity); // 48
            writer.Write(OwnerName, 16); // 52
            writer.Write(TargetIdentity); // 68
            writer.Write(TargetName, 16); // 72
            writer.Write(new byte[8]);
            writer.Write(Cost); // 100
            writer.Write(Expired ? 1 : 0); // 104
            writer.Write(DetainDate); // 108
            writer.Write(RemainingDays); // 112
            return writer.ToArray();
        }

        public override Task ProcessAsync(Client client)
        {
            return base.ProcessAsync(client);
        }

        public enum Mode
        {
            DetainPage,
            ClaimPage,
            ReadyToClaim
        }
    }
}
