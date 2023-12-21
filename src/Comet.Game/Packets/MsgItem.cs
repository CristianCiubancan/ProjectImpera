// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgItem.cs
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
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Game.Database.Models;
using Comet.Game.States;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Items;
using Comet.Game.States.NPCs;
using Comet.Game.World.Maps;
using Comet.Network.Packets;
using Comet.Shared;
using Org.BouncyCastle.Crypto.Tls;

#endregion

namespace Comet.Game.Packets
{
    /// <remarks>Packet Type 1009</remarks>
    /// <summary>
    ///     Message containing an item action command. Item actions are usually performed to
    ///     manage player equipment, inventory, money, or item shop purchases and sales. It
    ///     is serves a second purpose for measuring client ping.
    /// </summary>
    public sealed class MsgItem : MsgBase<Client>
    {
        public enum Moneytype
        {
            Silver,
            ConquerPoints,
            /// <summary>
            /// CPs(B)
            /// </summary>
            ConquerPointsMono
        }

        public MsgItem()
        {
            Type = PacketType.MsgItem;
        }

        public MsgItem(uint identity, ItemActionType action, uint cmd = 0, uint param = 0)
        {
            Type = PacketType.MsgItem;

            Identity = identity;
            Command = cmd;
            Action = action;
            Timestamp = (uint)Environment.TickCount;
            Argument = param;
        }

        // Packet Properties
        public uint Identity { get; set; }
        public uint Command { get; set; }
        public uint Timestamp { get; set; }
        public uint Argument { get; set; }
        public ItemActionType Action { get; set; }
        public uint Argument2 { get; set; }

        /// <summary>
        ///     Decodes a byte packet into the packet structure defined by this message class.
        ///     Should be invoked to structure data from the client for processing. Decoding
        ///     follows TQ Digital's byte ordering rules for an all-binary protocol.
        /// </summary>
        /// <param name="bytes">Bytes from the packet processor or client socket</param>
        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            Command = reader.ReadUInt32();
            Action = (ItemActionType)reader.ReadUInt32();
            Timestamp = reader.ReadUInt32();
            Argument = reader.ReadUInt32();
            Argument2 = reader.ReadUInt32();
        }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort)Type);
            writer.Write(Identity);
            writer.Write(Command);
            writer.Write((uint)Action);
            writer.Write(Timestamp);
            writer.Write(Argument);
            writer.Write(Argument2);
            return writer.ToArray();
        }

        /// <summary>
        ///     Process can be invoked by a packet after decode has been called to structure
        ///     packet fields and properties. For the server implementations, this is called
        ///     in the packet handler after the message has been dequeued from the server's
        ///     <see cref="PacketProcessor{TClient}" />.
        /// </summary>
        /// <param name="client">Client requesting packet processing</param>
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            BaseNpc npc = null;
            Item item = null;
            switch (Action)
            {
                case ItemActionType.ShopPurchase:
                    npc = Kernel.RoleManager.GetRole<BaseNpc>(Identity);
                    if (npc == null)
                        return;
                    if (npc.MapIdentity != 5000 && npc.MapIdentity != user.MapIdentity)
                        return;
                    if (npc.MapIdentity != 5000 && npc.GetDistance(user) > Screen.VIEW_SIZE)
                        return;

                    DbGoods goods = npc.ShopGoods.FirstOrDefault(x => x.Itemtype == Command);
                    if (goods == null)
                    {
                        await Log.WriteLogAsync(LogLevel.Cheat, $"Invalid goods itemtype {Command} for Shop {Identity}");
                        return;
                    }

                    if (Item.IsShield(Command) || Item.IsHelmet(Command) || Item.IsArmor(Command))
                        Command = Command - (Command % 1000 - Command % 100);

                    DbItemtype itemtype = Kernel.ItemManager.GetItemtype(Command);
                    if (itemtype == null)
                    {
                        await Log.WriteLogAsync(LogLevel.Cheat,
                            $"Invalid goods itemtype (not existent) {Command} for Shop {Identity}");
                        return;
                    }

                    int amount = (int)Math.Max(1, Argument);
                    if (!user.UserPackage.IsPackSpare(amount))
                    {
                        await user.SendAsync(Language.StrYourBagIsFull);
                        return;
                    }

                    const byte MONOPOLY_NONE_B = 0;
                    const byte MONOPOLY_BOUND_B = (byte)Item.ITEM_MONOPOLY_MASK;
                    byte monopoly = MONOPOLY_NONE_B;
                    switch ((Moneytype)goods.Moneytype)
                    {
                        case Moneytype.Silver:
                            if ((Moneytype)goods.Moneytype != Moneytype.Silver)
                                return;

                            if (itemtype.Price == 0)
                                return;

                            if (!await user.SpendMoneyAsync((int)(itemtype.Price * amount), true))
                                return;
                            break;
                        case Moneytype.ConquerPoints:
                            if ((Moneytype)goods.Moneytype != Moneytype.ConquerPoints)
                                return;

                            if (itemtype.EmoneyPrice == 0)
                                return;

                            if (!await user.SpendConquerPointsAsync((int)(itemtype.EmoneyPrice * amount), true))
                                return;
                            break;
                        default:
                            await Log.WriteLogAsync(LogLevel.Cheat,
                                $"Invalid moneytype {(Moneytype)Argument}/{Identity}/{Command} - {user.Identity}({user.Name})");
                            return;
                    }

                    for (int i = 0; i < amount; i++)
                    {
                        DbItem dbItem = Item.CreateEntity(itemtype.Type, monopoly != 0);
                        if (dbItem == null)
                            return;

                        item = new Item(user);
                        if (!await item.CreateAsync(dbItem))
                            return;

                        await user.UserPackage.AddItemAsync(item);
                    }

                    break;

                case ItemActionType.ShopSell:
                    {
                        if (Identity == 2888)
                            return;

                        npc = user.Map.QueryRole<BaseNpc>(Identity);
                        if (npc == null)
                            return;

                        if (npc.MapIdentity != user.MapIdentity || npc.GetDistance(user) > Screen.VIEW_SIZE)
                            return;

                        item = user.UserPackage[Command];
                        if (item == null)
                            return;

                        if (item.IsLocked())
                            return;

                        int price = item.GetSellPrice();
                        if (!await user.UserPackage.SpendItemAsync(item))
                            return;

                        await user.AwardMoneyAsync(price);
                        break;
                    }

                case ItemActionType.InventoryDropItem:
                case ItemActionType.InventoryRemove:
                    await user.DropItemAsync(Identity, user.MapX, user.MapY);
                    break;

                case ItemActionType.InventoryDropSilver:
                    await user.DropSilverAsync(Identity);
                    break;

                case ItemActionType.InventoryEquip:
                case ItemActionType.EquipmentWear:
                    if (!await user.UserPackage.UseItemAsync(Identity, (Item.ItemPosition)Command))
                        await user.SendAsync(Language.StrUnableToUseItem, MsgTalk.TalkChannel.TopLeft, Color.Red);
                    break;

                case ItemActionType.EquipmentRemove:
                    if (!await user.UserPackage.UnEquipAsync((Item.ItemPosition)Command))
                        await user.SendAsync(Language.StrYourBagIsFull, MsgTalk.TalkChannel.TopLeft, Color.Red);
                    break;

                case ItemActionType.EquipmentCombine:
                    {
                        item = user.UserPackage[Identity];
                        Item target = user.UserPackage[Command];
                        await user.UserPackage.CombineArrowAsync(item, target);
                        break;
                    }

                case ItemActionType.BankQuery:
                    Command = user.StorageMoney;
                    await user.SendAsync(this);
                    break;

                case ItemActionType.BankDeposit:
                    if (user.Silvers < Command)
                        return;

                    if (Command + user.StorageMoney > Role.MAX_STORAGE_MONEY)
                    {
                        await user.SendAsync(string.Format(Language.StrSilversExceedAmount, int.MaxValue));
                        return;
                    }

                    if (!await user.SpendMoneyAsync((int)Command, true))
                        return;

                    user.StorageMoney += Command;

                    Action = ItemActionType.BankQuery;
                    Command = user.StorageMoney;
                    await user.SendAsync(this);
                    await user.SaveAsync();
                    break;

                case ItemActionType.BankWithdraw:
                    if (Command > user.StorageMoney)
                        return;

                    if (Command + user.Silvers > int.MaxValue)
                    {
                        await user.SendAsync(string.Format(Language.StrSilversExceedAmount, int.MaxValue));
                        return;
                    }

                    user.StorageMoney -= Command;

                    await user.AwardMoneyAsync((int)Command);

                    Action = ItemActionType.BankQuery;
                    Command = user.StorageMoney;
                    await user.SendAsync(this);
                    await user.SaveAsync();
                    break;

                case ItemActionType.EquipmentRepair:
                    item = user.UserPackage[Identity];
                    if (item != null && item.Position == Item.ItemPosition.Inventory)
                        await item.RepairItemAsync();
                    break;

                case ItemActionType.EquipmentRepairAll:
                    if (user.VipLevel < 2)
                        return;

                    for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin;
                        pos <= Item.ItemPosition.EquipmentEnd;
                        pos++)
                    {
                        if (user.UserPackage[pos] != null)
                            await user.UserPackage[pos].RepairItemAsync();
                    }

                    break;

                case ItemActionType.EquipmentImprove:
                    {
                        item = user.UserPackage[Identity];
                        if (item == null || item.Position != Item.ItemPosition.Inventory)
                            return;

                        if (item.IsSuspicious())
                            return;

                        if (item.Durability / 100 != item.MaximumDurability / 100)
                        {
                            await user.SendAsync(Language.StrItemErrRepairItem);
                            return;
                        }

                        if (item.Type % 10 == 0)
                        {
                            await user.SendAsync(Language.StrItemErrUpgradeFixed);
                            return;
                        }

                        uint idNewType = 0;
                        double nChance = 0.00;

                        if (!item.GetUpEpQualityInfo(out nChance, out idNewType) || idNewType == 0)
                        {
                            await user.SendAsync(Language.StrItemCannotImprove);
                            return;
                        }

                        if (item.Type % 10 < 6 && item.Type % 10 > 0)
                        {
                            nChance = 100.00;
                        }

                        if (!await user.UserPackage.SpendDragonBallsAsync(1, item.IsBound))
                        {
                            await user.SendAsync(Language.StrItemErrNoDragonBall);
                            return;
                        }

                        if (user.IsLucky && await Kernel.ChanceCalcAsync(10, 2000))
                        {
                            await user.SendEffectAsync("LuckyGuy", true);
                            await user.SendAsync(Language.StrLuckyGuySuccessUpgrade);
                            nChance = 100.00;
                        }

                        if (await Kernel.ChanceCalcAsync(nChance))
                        {
                            await item.ChangeTypeAsync(idNewType);
                        }
                        else
                        {
                            if (user.IsLucky && await Kernel.ChanceCalcAsync(2))
                            {
                                await user.SendEffectAsync("LuckyGuy", true);
                                await user.SendAsync(Language.StrLuckyGuyNoDuraDown);
                            }
                            else
                            {
                                item.Durability = (ushort)(item.MaximumDurability / 2);
                            }
                        }

                        if (item.SocketOne == Item.SocketGem.NoSocket && await Kernel.ChanceCalcAsync(5, 1000))
                        {
                            item.SocketOne = Item.SocketGem.EmptySocket;
                            await user.SendAsync(Language.StrUpgradeAwardSocket);
                        }

                        await item.SaveAsync();
                        await user.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
                        await Log.GmLogAsync("improve",
                            $"{user.Identity},{user.Name};{item.Identity};{item.Type};{Item.TYPE_DRAGONBALL}");
                        break;
                    }
                case ItemActionType.EquipmentLevelUp:
                    {
                        item = user.UserPackage[Identity];
                        if (item == null || item.Position != Item.ItemPosition.Inventory)
                            return;

                        if (item.IsSuspicious())
                            return;

                        if (item.Durability / 100 != item.MaximumDurability / 100)
                        {
                            await user.SendAsync(Language.StrItemErrRepairItem);
                            return;
                        }

                        if (item.Type % 10 == 0)
                        {
                            await user.SendAsync(Language.StrItemErrUpgradeFixed);
                            return;
                        }

                        int idNewType = 0;
                        double nChance = 0.00;

                        if (!item.GetUpLevelChance(out nChance, out idNewType) || idNewType == 0)
                        {
                            await user.SendAsync(Language.StrItemErrMaxLevel);
                            return;
                        }

                        DbItemtype dbNewType = Kernel.ItemManager.GetItemtype((uint)idNewType);
                        if (dbNewType == null)
                        {
                            await user.SendAsync(Language.StrItemErrMaxLevel);
                            return;
                        }

                        if (!await user.UserPackage.SpendMeteorsAsync(1))
                        {
                            await user.SendAsync(string.Format(Language.StrItemErrNotEnoughMeteors, 1));
                            return;
                        }

                        if (user.IsLucky && await Kernel.ChanceCalcAsync(10, 2000))
                        {
                            await user.SendEffectAsync("LuckyGuy", true);
                            await user.SendAsync(Language.StrLuckyGuySuccessUplevel);
                            nChance = 100.00;
                        }

                        if (await Kernel.ChanceCalcAsync(nChance))
                        {
                            await item.ChangeTypeAsync((uint)idNewType);
                        }
                        else
                        {
                            if (user.IsLucky && await Kernel.ChanceCalcAsync(2))
                            {
                                await user.SendEffectAsync("LuckyGuy", true);
                                await user.SendAsync(Language.StrLuckyGuyNoDuraDown);
                            }
                            else
                            {
                                item.Durability = (ushort)(item.MaximumDurability / 2);
                            }
                        }

                        if (item.SocketOne == Item.SocketGem.NoSocket && await Kernel.ChanceCalcAsync(5, 1000))
                        {
                            item.SocketOne = Item.SocketGem.EmptySocket;
                            await user.SendAsync(Language.StrUpgradeAwardSocket);
                            await item.SaveAsync();
                        }

                        await item.SaveAsync();
                        await user.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
                        await Log.GmLogAsync("uplev",
                            $"{user.Identity},{user.Name};{item.Identity};{item.Type};{Item.TYPE_METEOR}");
                        break;
                    }

                case ItemActionType.BoothQuery:
                    {
                        var targetNpc = user.Screen.Roles.Values.FirstOrDefault(x =>
                            x is Character targetUser && targetUser.Booth?.Identity == Identity) as Character;
                        if (targetNpc?.Booth == null)
                            return;

                        await targetNpc.Booth.QueryItemsAsync(user);
                        break;
                    }

                case ItemActionType.BoothSell:
                    {
                        if (user.AddBoothItem(Identity, Command, Moneytype.Silver))
                            await user.SendAsync(this);
                        break;
                    }

                case ItemActionType.BoothRemove:
                    {
                        if (user.RemoveBoothItem(Identity))
                            await user.SendAsync(this);
                        break;
                    }

                case ItemActionType.BoothPurchase:
                    {
                        var targetNpc = user.Screen.Roles.Values.FirstOrDefault(x =>
                            x is Character targetUser && targetUser.Booth?.Identity == Command) as Character;
                        if (targetNpc?.Booth == null)
                            return;

                        if (await targetNpc.SellBoothItemAsync(Identity, user))
                        {
                            Action = ItemActionType.BoothRemove;
                            await targetNpc.SendAsync(this);
                            await user.SendAsync(this);
                        }
                        break;
                    }

                case ItemActionType.BoothSellPoints:
                    {
                        if (user.AddBoothItem(Identity, Command, Moneytype.ConquerPoints))
                            await user.SendAsync(this);
                        break;
                    }

                case ItemActionType.ClientPing:
                    Character testUser = Kernel.RoleManager.GetUser(user.Identity);
                    if (testUser == null || !testUser.Client.GUID.Equals(user.Client.GUID))
                    {
                        await Kernel.RoleManager.KickOutAsync(user.Identity, "Duplicated client!!!!");
                        user.Client.Disconnect();
                        return;
                    }

                    await client.SendAsync(this);
                    await client.SendAsync(new MsgData(DateTime.Now));
                    break;

                case ItemActionType.EquipmentEnchant:
                    {
                        item = user.UserPackage[Identity];
                        Item gem = user.UserPackage[Command];

                        if (item == null || gem == null)
                            return;

                        if (item.IsSuspicious())
                            return;

                        if (item.Enchantment >= byte.MaxValue)
                            return;

                        if (!gem.IsGem())
                            return;

                        await user.UserPackage.SpendItemAsync(gem);

                        byte min, max;
                        switch ((Item.SocketGem)(gem.Type % 1000))
                        {
                            case Item.SocketGem.NormalPhoenixGem:
                            case Item.SocketGem.NormalDragonGem:
                            case Item.SocketGem.NormalFuryGem:
                            case Item.SocketGem.NormalKylinGem:
                            case Item.SocketGem.NormalMoonGem:
                            case Item.SocketGem.NormalTortoiseGem:
                            case Item.SocketGem.NormalVioletGem:
                                min = 1;
                                max = 59;
                                break;
                            case Item.SocketGem.RefinedPhoenixGem:
                            case Item.SocketGem.RefinedVioletGem:
                            case Item.SocketGem.RefinedMoonGem:
                                min = 60;
                                max = 109;
                                break;
                            case Item.SocketGem.RefinedFuryGem:
                            case Item.SocketGem.RefinedKylinGem:
                            case Item.SocketGem.RefinedTortoiseGem:
                                min = 40;
                                max = 89;
                                break;
                            case Item.SocketGem.RefinedDragonGem:
                                min = 100;
                                max = 159;
                                break;
                            case Item.SocketGem.RefinedRainbowGem:
                                min = 80;
                                max = 129;
                                break;
                            case Item.SocketGem.SuperPhoenixGem:
                            case Item.SocketGem.SuperTortoiseGem:
                            case Item.SocketGem.SuperRainbowGem:
                                min = 170;
                                max = 229;
                                break;
                            case Item.SocketGem.SuperVioletGem:
                            case Item.SocketGem.SuperMoonGem:
                                min = 140;
                                max = 199;
                                break;
                            case Item.SocketGem.SuperDragonGem:
                                min = 200;
                                max = 255;
                                break;
                            case Item.SocketGem.SuperFuryGem:
                                min = 90;
                                max = 149;
                                break;
                            case Item.SocketGem.SuperKylinGem:
                                min = 70;
                                max = 119;
                                break;
                            default:
                                return;
                        }

                        byte enchant = (byte)await Kernel.NextAsync(min, max);
                        if (enchant > item.Enchantment)
                        {
                            item.Enchantment = enchant;
                            await item.SaveAsync();
                            await Log.GmLogAsync("enchant",
                                $"User[{user.Identity}] Enchant[Gem: {gem.Type}|{gem.Identity}][Target: {item.Type}|{item.Identity}] with {enchant} points.");
                        }

                        Command = enchant;
                        await user.SendAsync(this);
                        await user.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
                        break;
                    }

                case ItemActionType.RedeemEquipment:
                    {
                        if (await Kernel.ItemManager.ClaimDetainedItemAsync(Identity, user))
                        {
                            Command = user.Identity;
                            await user.SendAsync(this);
                        }
                        break;
                    }

                case ItemActionType.DetainEquipment:
                    {
                        if (await Kernel.ItemManager.ClaimDetainRewardAsync(Identity, user))
                        {
                            Command = user.Identity;
                            await user.SendAsync(this);
                        }
                        break;
                    }

                case ItemActionType.DetainRewardClose:
                    {
                        break;
                    }

                case ItemActionType.TalismanProgress:
                    {
                        item = user.UserPackage.GetEquipmentById(Identity);
                        Item target = user.UserPackage[Command];

                        if (item == null || target == null)
                            return;

                        if (target.IsBound && !item.IsBound)
                            return;

                        if (!item.IsTalisman())
                            return;

                        if (target.IsTalisman() || target.IsMount() || !target.IsEquipment())
                            return;

                        if (target.GetQuality() < 6)
                            return;

                        item.SocketProgress += target.CalculateSocketProgress();
                        if (item.SocketOne == Item.SocketGem.NoSocket && item.SocketProgress >= 8000)
                        {
                            item.SocketProgress = 0;
                            item.SocketOne = Item.SocketGem.EmptySocket;
                        }
                        else if (item.SocketOne != Item.SocketGem.NoSocket && item.SocketTwo == Item.SocketGem.NoSocket && item.SocketProgress >= 20000)
                        {
                            item.SocketProgress = 0;
                            item.SocketTwo = Item.SocketGem.EmptySocket;
                        }

                        await user.UserPackage.RemoveFromInventoryAsync(target, UserPackage.RemovalType.Delete);
                        await item.SaveAsync();
                        await user.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
                        await user.SendAsync(this);
                        break;
                    }

                case ItemActionType.TalismanProgressEmoney:
                    {
                        item = user.UserPackage.GetEquipmentById(Identity);

                        if (item == null)
                            return;

                        if (item.SocketOne == Item.SocketGem.NoSocket)
                        {
                            if (item.SocketProgress < 2400)
                                return;

                            if (!await user.SpendConquerPointsAsync((int)(5600 * (1 - item.SocketProgress / 8000f)), true))
                                return;

                            item.SocketProgress = 0;
                            item.SocketOne = Item.SocketGem.EmptySocket;
                        }
                        else if (item.SocketOne != Item.SocketGem.NoSocket && item.SocketTwo == Item.SocketGem.NoSocket)
                        {
                            if (item.SocketProgress < 2400)
                                return;

                            if (!await user.SpendConquerPointsAsync((int)(14000 * (1 - item.SocketProgress / 20000f)), true))
                                return;

                            item.SocketProgress = 0;
                            item.SocketTwo = Item.SocketGem.EmptySocket;
                        }

                        await item.SaveAsync();
                        await user.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
                        await user.SendAsync(this);
                        break;
                    }

                default:
                    await client.SendAsync(this);
                    if (client.Character.IsGm())
                        await client.SendAsync(new MsgTalk(client.Identity, MsgTalk.TalkChannel.Service,
                            $"Missing packet {Type}, Action {Action}, Length {Length}"));
                    Console.WriteLine("Missing packet {0}, action {1}, Length {2}\n{3}", Type, Action, Length, PacketDump.Hex(Encode()));
                    break;
            }
        }

        /// <summary>
        ///     Enumeration type for defining item actions that may be requested by the user,
        ///     or given to by the server. Allows for action handling as a packet subtype.
        ///     Enums should be named by the action they provide to a system in the context
        ///     of the player item.
        /// </summary>
        public enum ItemActionType
        {
            ShopPurchase = 1,
            ShopSell,
            InventoryRemove,
            InventoryEquip,
            EquipmentWear,
            EquipmentRemove,
            EquipmentSplit,
            EquipmentCombine,
            BankQuery,
            BankDeposit,
            BankWithdraw,
            //InventoryDropSilver,
            EquipmentRepair = 14,
            EquipmentRepairAll,
            EquipmentImprove = 19,
            EquipmentLevelUp,
            BoothQuery,
            BoothSell,
            BoothRemove,
            BoothPurchase,
            EquipmentAmount,
            ClientPing = 27,
            EquipmentEnchant,
            BoothSellPoints,
            RedeemEquipment = 32,
            DetainEquipment = 33,
            DetainRewardClose = 34,
            TalismanProgress = 35,
            TalismanProgressEmoney = 36,
            InventoryDropItem = 37,
            InventoryDropSilver = 38
        }
    }
}