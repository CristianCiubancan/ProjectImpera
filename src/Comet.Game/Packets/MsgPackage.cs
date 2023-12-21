// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgPackage.cs
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
using Comet.Game.States.NPCs;
using Comet.Game.World.Maps;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgPackage : MsgBase<Client>
    {
        public enum WarehouseMode : byte
        {
            Query = 0,
            CheckIn,
            CheckOut
        }

        public enum StorageType : byte
        {
            None = 0,
            Storage = 10,
            Trunk = 20,
            Chest = 30,
        };

        public struct WarehouseItem
        {
            public uint Identity;
            public uint Type;
            public byte Ident;
            public Item.SocketGem SocketOne;
            public Item.SocketGem SocketTwo;
            public Item.ItemEffect Magic1;
            public byte Magic2;
            public byte Magic3;
            public ushort Blessing;
            public bool Bound;
            public ushort Enchantment;
            public ushort AntiMonster;
            public bool Suspicious;
            public bool Locked;
            public Item.ItemColor Color;
            public uint SocketProgress;
            public uint CompositionProgress;
            public int Inscribed;
        }

        public MsgPackage()
        {
            Type = PacketType.MsgPackage;
        }

        public uint Identity { get; set; }
        public WarehouseMode Action { get; set; }
        public StorageType Mode { get; set; }
        public ushort Unknown { get; set; }
        public uint Param { get; set; }
        public List<WarehouseItem> Items = new List<WarehouseItem>();

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            Action = (WarehouseMode) reader.ReadByte();
            Mode = (StorageType) reader.ReadByte();
            Unknown = reader.ReadUInt16();
            Param = reader.ReadUInt32();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort)Type);
            writer.Write(Identity);
            writer.Write((byte) Action);
            writer.Write((byte) Mode);
            writer.Write((ushort) 0);
            if (Items.Count > 0)
            {
                writer.Write(Items.Count);
                foreach (var item in Items)
                {
                    writer.Write(item.Identity); // 0
                    writer.Write(item.Type); // 4
                    writer.Write(item.Ident); // 8
                    writer.Write((byte) item.SocketOne); // 9
                    writer.Write((byte) item.SocketTwo); // 10
                    writer.Write((byte) item.Magic1); // 11
                    writer.Write(item.Magic2); // 12
                    writer.Write(item.Magic3); // 13
                    writer.Write((byte) item.Blessing); // 14
                    writer.Write(item.Bound); // 15
                    writer.Write(item.Enchantment); // 16
                    writer.Write(item.AntiMonster); // 18
                    writer.Write(item.Suspicious); // 20
                    writer.Write((byte)0); // 21
                    writer.Write(item.Locked); // 22
                    writer.Write((byte)item.Color); // 23
                    writer.Write(item.SocketProgress); // 24
                    writer.Write(item.CompositionProgress); // 28
                    writer.Write(item.Inscribed);
                }
            }
            else
            {
                writer.Write(Param);
            }
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            BaseNpc npc = null;
            Item storageItem = null;
            if (Mode == StorageType.Storage || Mode == StorageType.Trunk)
            {
                npc = Kernel.RoleManager.GetRole(Identity) as BaseNpc;
                
                if (npc == null)
                {
                    if (user.IsPm())
                        await user.SendAsync($"Could not find Storage NPC, {Identity}");
                    return;
                }

                BaseNpc interacting = Kernel.RoleManager.GetRole<BaseNpc>(user.InteractingNpc);

                if (interacting == null || interacting.Type != BaseNpc.STORAGE_NPC)
                {
                    return;
                }

                if (interacting.MapIdentity != 5000 &&
                    (interacting.MapIdentity != user.MapIdentity || interacting.GetDistance(user) > Screen.VIEW_SIZE))
                {
                    if (user.IsPm())
                        await user.SendAsync($"NPC not in range, {Identity}");
                    return;
                }

                if (interacting.MapIdentity == 5000)
                {
                    switch (npc.MapIdentity)
                    {
                        case 1002: // twin
                        case 1036: // market
                            if (user.BaseVipLevel < 1)
                                return;
                            break;
                        case 1000: // desert
                            if (user.BaseVipLevel < 2)
                                return;
                            break;
                        case 1020: // canyon
                            if (user.BaseVipLevel < 3)
                                return;
                            break;
                        case 1015: // bird
                            if (user.BaseVipLevel < 4)
                                return;
                            break;
                        case 1011: // phoenix
                            if (user.BaseVipLevel < 5)
                                return;
                            break;
                        case 1213: // stone
                            if (user.BaseVipLevel < 6)
                                return;
                            break;
                    }
                }
            }
            else if (Mode == StorageType.Chest)
            {
                storageItem = user.UserPackage[Identity];
                if (storageItem == null)
                {
                    return;
                }
            }

            if (Action == WarehouseMode.Query)
            {
                foreach (var item in user.UserPackage.GetStorageItems(Identity, Mode))
                {
                    Items.Add(new WarehouseItem
                    {
                        Identity = item.Identity,
                        Type = item.Type,
                        SocketOne = item.SocketOne,
                        SocketTwo = item.SocketTwo,
                        Blessing = (byte) item.Blessing,
                        Enchantment = item.Enchantment,
                        Magic1 = item.Effect,
                        Magic3 = item.Plus,
                        Locked = item.IsLocked(),
                        Color = item.Color,
                        Suspicious = false,
                        CompositionProgress = item.CompositionProgress,
                        SocketProgress = item.SocketProgress,
                        Bound = item.IsBound,
                        Inscribed = item.SyndicateIdentity != 0 ? 1 : 0
                    });

                    if (Items.Count >= 20)
                    {
                        await user.SendAsync(this);
                        Items.Clear();
                    }
                }

                if (Items.Count > 0)
                    await user.SendAsync(this);
            }
            else if (Action == WarehouseMode.CheckIn)
            {
                Item storeItem = user.UserPackage[Param];
                if (storeItem == null)
                {
                    await user.SendAsync(Language.StrItemNotFound);
                    return;
                }

                if (!storeItem.CanBeStored())
                {
                    await user.SendAsync(Language.StrItemCannotBeStored);
                    return;
                }

                if (Mode == StorageType.Storage && npc?.IsStorageNpc() != true)
                    return;
                else if (Mode == StorageType.Chest && storageItem?.GetItemSort() != (Item.ItemSort?) 11)
                    return;

                if (user.UserPackage.StorageSize(Identity, Mode) >= 40) // all warehouses 40 blocks
                {
                    await user.SendAsync(Language.StrPackageFull);
                    return;
                }

                await user.UserPackage.AddToStorageAsync(Identity, storeItem, Mode, true);
            }
            else if (Action == WarehouseMode.CheckOut)
            {
                await user.UserPackage.GetFromStorageAsync(Identity, Param, Mode, true);
            }
        }
    }
}