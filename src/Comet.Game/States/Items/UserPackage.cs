// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - User Package.cs
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Game.Database.Models;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets;
using Comet.Game.States.NPCs;
using Comet.Game.World;
using Comet.Shared;
using Org.BouncyCastle.Asn1.Esf;

#endregion

namespace Comet.Game.States.Items
{
    public sealed class UserPackage
    {
        public const int MAX_INVENTORY_CAPACITY = 40;

        private readonly Character m_user;

        private readonly ConcurrentDictionary<Item.ItemPosition, Item> m_dicEquipment;
        private readonly ConcurrentDictionary<uint, Item> m_dicInventory;
        private readonly ConcurrentDictionary<uint, List<Item>> m_dicWarehouses;
        private readonly ConcurrentDictionary<uint, List<Item>> m_dicSashes;

        public UserPackage(Character user)
        {
            m_user = user;

            m_dicEquipment = new ConcurrentDictionary<Item.ItemPosition, Item>();
            m_dicInventory = new ConcurrentDictionary<uint, Item>();
            m_dicWarehouses = new ConcurrentDictionary<uint, List<Item>>();
            m_dicSashes = new ConcurrentDictionary<uint, List<Item>>();
        }

        public Item this[Item.ItemPosition position] => m_dicEquipment.TryGetValue(position, out var item) ? item : null;

        public Item this[uint uid] => m_dicInventory.TryGetValue(uid, out var item) ? item : null;

        public Item this[Item.ItemPosition pos, uint uid] =>
            m_dicEquipment.TryGetValue(pos, out var item) && item.Identity == uid ? item : null;

        public Item this[uint storage, uint uid] => m_dicWarehouses.TryGetValue(storage, out var items)
            ? items.FirstOrDefault(x => x.Identity == uid)
            : null;

        public Item this[string name] => m_dicInventory.Values.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        public Item GetEquipmentById(uint idItem)
        {
            return m_dicEquipment.Values.FirstOrDefault(x => x.Identity == idItem);
        }

        public async Task<bool> CreateAsync()
        {
            var items = await ItemRepository.GetAsync(m_user.Identity);
            foreach (var dbItem in items.OrderBy(x => x.OwnerId).ThenBy(x => x.Position).ThenBy(x => x.Id))
            {
                Item item = new Item(m_user);
                if (!await item.CreateAsync(dbItem))
                {
                    await Log.WriteLogAsync(LogLevel.Error, $"Failed to load item {dbItem.Id} to user {m_user.Identity}");
                    await item.DeleteAsync(Item.ChangeOwnerType.InvalidItemType);
                    continue;
                }

                if (item.IsSuspicious()
                    && item.Position >= Item.ItemPosition.EquipmentBegin &&
                    item.Position <= Item.ItemPosition.EquipmentEnd)
                    item.Position = Item.ItemPosition.Inventory;

                if (item.Position >= Item.ItemPosition.EquipmentBegin &&
                    item.Position <= Item.ItemPosition.EquipmentEnd)
                {
                    if (!m_dicEquipment.TryAdd(item.Position, item))
                        m_dicInventory.TryAdd(item.Identity, item);
                }
                else if (item.Position == Item.ItemPosition.Inventory)
                {
                    if (!m_dicInventory.TryAdd(item.Identity, item))
                        await Log.WriteLogAsync(LogLevel.Warning,
                            $"Failed to insert inventory item {item.Identity}: duplicate???");
                }
                else if (item.Position == Item.ItemPosition.Floor)
                {
                    await item.DeleteAsync();
                }
                else if (item.Position == Item.ItemPosition.Storage || item.Position == Item.ItemPosition.Trunk)
                {
                    BaseNpc npc = Kernel.RoleManager.GetRole(item.OwnerIdentity) as BaseNpc;
                    if (npc == null)
                    {
                        await Log.WriteLogAsync(LogLevel.Error, $"Unexistent warehouse {item.OwnerIdentity} for item {item.Identity}");
                        continue;
                    }

                    await AddToStorageAsync(npc.Identity, item, MsgPackage.StorageType.Storage, false);
                }
                else if (item.Position == Item.ItemPosition.Chest)
                {
                    await AddToStorageAsync(item.OwnerIdentity, item, MsgPackage.StorageType.Chest, false);
                }
                else
                {
                    await Log.WriteLogAsync(LogLevel.Warning,
                        $"Item {item.Identity} on '{item.Position}' cannot be loaded (unhandled)");
                }
            }

            return true;
        }

        public async Task<bool> AwardItemAsync(uint type, Item.ItemPosition pos = Item.ItemPosition.Inventory)
        {
            DbItemtype itemtype = Kernel.ItemManager.GetItemtype(type);
            if (itemtype == null)
                return false;

            Item item = new Item(m_user);
            if (!await item.CreateAsync(itemtype, pos))
                return false;
            
            return m_dicInventory.TryAdd(item.Identity, item) && await AddItemAsync(item);
        }

        public async Task<bool> UseItemAsync(uint idItem, Item.ItemPosition position)
        {
            if (!TryItem(idItem, position))
                return false;

            Item item = this[idItem];
            if (item == null)
                return false;

            if (item.IsSuspicious())
                return false;

            if (item.Type == Item.TYPE_EXP_BALL)
            {
                if (!m_user.CanUseExpBall())
                    return false;

                m_user.IncrementExpBall();
                await m_user.AwardExperienceAsync(m_user.CalculateExpBall());
                await SpendItemAsync(item);
                return true;
            }

            if (item.Type == Item.TYPE_EXP_POTION)
            {
                if (m_user.ExperienceMultiplier > 2 && m_user.RemainingExperienceSeconds > 0)
                {
                    await m_user.SendAsync(Language.StrExpPotionInUse);
                    return true;
                }

                await m_user.SetExperienceMultiplierAsync(3600);
                await SpendItemAsync(item);
                return true;
            }

            if (item.IsEquipEnable())
                return await EquipItemAsync(item, position);

            if (item.IsMedicine())
            {
                if (item.Life > 0 && m_user.Life == m_user.MaxLife ||
                    item.Mana > 0 && m_user.MaxMana == m_user.Mana)
                    return false;

                if (!await SpendItemAsync(item))
                    return false;

                if (item.Life > 0 && m_user.QueryStatus(StatusSet.POISON_STAR) == null)
                    m_user.QueueAction(() => m_user.AddAttributesAsync(ClientUpdateType.Hitpoints, item.Life)); // may be on queue, may not
                if (item.Mana > 0)
                    m_user.QueueAction(() => m_user.AddAttributesAsync(ClientUpdateType.Mana, item.Mana)); // may be on queue, may not

                return false;
            }

            var type = item.GetItemtype()/10000;

            if (item.Itemtype.IdAction > 0)
            {
                m_user.InteractingItem = item.Identity;

                bool result = await GameAction.ExecuteActionAsync(item.Itemtype.IdAction, m_user, null, item, "") &&
                         item.GetItemSort() == Item.ItemSort.ItemsortUsable2 && type != 8 && item.Itemtype.IdAction > 0;

                // todo validate auto
                if (item.GetItemSubType() == 106)
                    await SpendItemAsync(item);

                return result;
            }

            return false;
        }

        public async Task<bool> EquipItemAsync(Item item, Item.ItemPosition position)
        {
            if (item == null)
                return false;

            if (position == Item.ItemPosition.Inventory)
            {
                if (m_user.IsWing && !item.IsArrowSort())
                    return false;

                position = item.GetPosition();
                if (this[Item.ItemPosition.RightHand] != null
                    && this[Item.ItemPosition.LeftHand] == null
                    && item.IsWeaponOneHand()
                    && !item.IsBackswordType())
                    position = Item.ItemPosition.LeftHand;
            }

            switch (position)
            {
                case Item.ItemPosition.RightHand:
                    if (!item.IsHoldEnable())
                        return false;

                    if (item.IsWeaponTwoHand())
                    {
                        if (!(this[Item.ItemPosition.RightHand] != null
                              && this[Item.ItemPosition.LeftHand] != null
                              && !IsPackSpare(2)))
                        {
                            await UnEquipAsync(Item.ItemPosition.RightHand);

                            if (this[Item.ItemPosition.LeftHand] != null 
                                && !this[Item.ItemPosition.LeftHand].IsArrowSort() 
                                && item.IsBow() 
                                && this[Item.ItemPosition.LeftHand]?.IsShield() == true)
                                await UnEquipAsync(Item.ItemPosition.LeftHand);
                        }
                    }
                    else if (item.IsWeaponOneHand() || item.IsWeaponProBased())
                    {
                        Item pLeft = this[Item.ItemPosition.LeftHand];
                        if (!(pLeft != null && pLeft.IsArrowSort() && !IsPackSpare(2)))
                        {
                            await UnEquipAsync(Item.ItemPosition.RightHand);
                            if (pLeft != null && pLeft.IsArrowSort())
                                await UnEquipAsync(Item.ItemPosition.LeftHand);
                        }
                    }

                    break;

                case Item.ItemPosition.LeftHand:
                    if (!item.IsHoldEnable())
                        return false;

                    Item pRight = this[Item.ItemPosition.RightHand];
                    if (pRight == null)
                        return false;

                    if (item.IsBackswordType() || pRight.IsBackswordType())
                        return false;

                    if (pRight.IsBackswordType() && item.IsShield())
                        return false;

                    if (!pRight.IsBow() && item.IsArrowSort())
                        return false;

                    if (pRight.IsWeaponOneHand() && !pRight.IsBackswordType() &&
                        (item.IsWeaponOneHand() || item.IsShield())
                        || pRight.IsBow() && item.IsArrowSort())
                    {
                        await UnEquipAsync(Item.ItemPosition.LeftHand);
                    }
                    else if (pRight.IsWeaponProBased())
                    {
                        if (pRight.GetItemSubType() == item.GetItemSubType())
                        {
                            if (item.IsShield())
                                return false;

                            await UnEquipAsync(Item.ItemPosition.LeftHand);
                        }
                    }

                    break;

                default:
                    await UnEquipAsync(position);
                    break;
            }

            if (!await RemoveFromInventoryAsync(item, RemovalType.UnEquip))
                return false;

            if (!m_dicEquipment.TryAdd(position, item))
            {
                await AddItemAsync(item);
                return false;
            }

            item.Position = position;
            await m_user.SendAsync(new MsgItem(item.Identity, MsgItem.ItemActionType.EquipmentWear, (uint) position));

            await item.SaveAsync();

            switch (position)
            {
                case Item.ItemPosition.Headwear:
                case Item.ItemPosition.RightHand:
                case Item.ItemPosition.LeftHand:
                case Item.ItemPosition.Armor:
                case Item.ItemPosition.Garment:
                    await m_user.Screen.BroadcastRoomMsgAsync(new MsgPlayer(m_user), false);
                    break;
            }

            if (m_user.Team != null)
                await m_user.Team.SyncFamilyBattlePowerAsync();

            if (m_user.ApprenticeCount > 0)
                await m_user.SynchroApprenticesSharedBattlePowerAsync();

            return true;
        }

        public async Task<bool> UnEquipAsync(Item.ItemPosition position, RemovalType mode = RemovalType.RemoveOnly)
        {
            Item item = this[position];
            if (item == null)
                return false;

            if (position == Item.ItemPosition.RightHand
                && this[Item.ItemPosition.LeftHand] != null)
            {
                if (!IsPackSpare(2) && mode != RemovalType.Delete)
                    return false;

                if (!await UnEquipAsync(Item.ItemPosition.LeftHand))
                    return false;
            }
            else
            {
                if (!IsPackSpare(1) && mode != RemovalType.Delete)
                    return false;
            }

            m_dicEquipment.TryRemove(position, out _);

            item.Position = Item.ItemPosition.Inventory;
            if (mode != RemovalType.Delete && mode != RemovalType.RemoveAndDisappear)
            {
                await m_user.SendAsync(new MsgItem(item.Identity, MsgItem.ItemActionType.EquipmentRemove, (uint)position));
                await m_user.SendAsync(new MsgItemInfo(item));
            }
            else
            {
                await m_user.SendAsync(new MsgItem(item.Identity, MsgItem.ItemActionType.EquipmentRemove, (uint) position));
                await m_user.SendAsync(new MsgItemInfo(item));
                await m_user.SendAsync(new MsgItem(item.Identity, MsgItem.ItemActionType.InventoryRemove));
            }

            if (mode == RemovalType.Delete)
                await item.DeleteAsync();
            else if (mode != RemovalType.RemoveAndDisappear)
            {
                await AddItemAsync(item);
                await item.SaveAsync();
            }
            else
            {
                await item.SaveAsync();
            }

            switch (position)
            {
                case Item.ItemPosition.Headwear:
                case Item.ItemPosition.RightHand:
                case Item.ItemPosition.LeftHand:
                case Item.ItemPosition.Armor:
                case Item.ItemPosition.Garment:
                    await m_user.Screen.BroadcastRoomMsgAsync(new MsgPlayer(m_user), false);
                    break;
            }

            if (m_user.Team != null)
                await m_user.Team.SyncFamilyBattlePowerAsync();

            if (m_user.ApprenticeCount > 0)
                await m_user.SynchroApprenticesSharedBattlePowerAsync();

            return true;
        }

        public bool TryItem(uint idItem, Item.ItemPosition position)
        {
            Item item = this[idItem];
            if (item == null)
                return false;

            if (item.IsSuspicious())
                return false;

            if (item.IsShield() && m_user.Map.IsLineSkillMap())
                return false;

            if (item.RequiredLevel > m_user.Level)
                return false;

            if (item.RequiredGender > 0 && item.RequiredGender != m_user.Gender)
                return false;

            if (item.Durability == 0 && !item.IsExpend())
                return false;

            if (m_user.Metempsychosis > 0 && m_user.Level >= 70)
                return true;

            if (item.RequiredProfession > 0)
            {
                int nRequireProfSort = item.RequiredProfession % 1000 / 10;
                int nRequireProfLevel = item.RequiredProfession % 10;
                int nProfSort = m_user.ProfessionSort;
                int nProfLevel = m_user.ProfessionLevel;

                if (nRequireProfSort == 19)
                {
                    if (nProfSort < 10 || position == Item.ItemPosition.LeftHand)
                        return false;
                }
                else
                {
                    if (nRequireProfSort != nProfSort)
                        return false;
                }

                if (nProfLevel < nRequireProfLevel)
                    return false;
            }

            if (item.RequiredForce > m_user.Strength
                || item.RequiredAgility > m_user.Agility
                || item.RequiredVitality > m_user.Vitality
                || item.RequiredSpirit > m_user.Spirit)
                return false;

            if (item.RequiredWeaponSkill > 0 &&
                m_user.WeaponSkill[(ushort) item.GetItemtype()]?.Level < item.RequiredWeaponSkill)
                return false;

            return true;
        }

        public async Task<bool> CombineArrowAsync(uint idItem, uint idOther)
        {
            return m_dicInventory.TryGetValue(idItem, out var item)
                   && m_dicInventory.TryGetValue(idOther, out var other)
                   && await CombineArrowAsync(item, other);
        }

        public async Task<bool> CombineArrowAsync(Item item, Item other)
        {
            if (item == null || other == null || !item.IsArrowSort() || item.Type != other.Type)
                return false;

            ushort nNewNum = (ushort)(item.Durability + other.Durability);
            if (nNewNum > item.MaximumDurability)
            {
                item.Durability = (ushort)(nNewNum - other.MaximumDurability);
                other.Durability = item.MaximumDurability;
                await m_user.SendAsync(new MsgItemInfo(other, MsgItemInfo.ItemMode.Update));
                await m_user.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
            }
            else
            {
                item.Durability = 0;
                await RemoveFromInventoryAsync(item.Identity, RemovalType.Delete);
                other.Durability = nNewNum;
                await m_user.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
            }

            return true;
        }

        public async Task<bool> AddItemAsync(Item item)
        {
            if (!IsPackSpare((int) item.AccumulateNum))
                return false;
            
            item.PlayerIdentity = m_user.Identity;
            item.Position = Item.ItemPosition.Inventory;

            if (item.IsCountable())
            {
                Item combine = FindCombineItem(item);
                if (combine != null)
                {
                    do
                    {
                        await CombineItemAsync(item, combine);
                    } while (item.AccumulateNum > 1 && item.AccumulateNum > item.MaxAccumulateNum &&
                             (combine = FindCombineItem(item)) != null);

                    if (item.AccumulateNum == 0)
                    {
                        await RemoveFromInventoryAsync(item, RemovalType.Delete);
                        return false;
                    }
                }
            }

            if (item.AccumulateNum > item.MaxAccumulateNum)
            {
                var amount = item.AccumulateNum - item.MaxAccumulateNum;
                item.AccumulateNum = item.MaxAccumulateNum;

                DbItem newDbItem = Item.CreateEntity(item.Type, item.IsBound);
                newDbItem.AccumulateNum = amount;
                if (!item.IsPileEnable())
                {
                    newDbItem.ReduceDmg = item.ReduceDamage;
                    newDbItem.AddLife = item.Enchantment;
                    newDbItem.AntiMonster = item.AntiMonster;
                    newDbItem.Data = item.Data;
                }

                Item newItem = new Item(m_user);
                if (await newItem.CreateAsync(newDbItem))
                    await AddItemAsync(newItem);
            }

            m_dicInventory.TryAdd(item.Identity, item);
            await item.SaveAsync();
            await m_user.SendAsync(new MsgItemInfo(item));
            return true;
        }

        public async Task<bool> RemoveFromInventoryAsync(uint idItem, RemovalType mode = RemovalType.RemoveOnly)
        {
            return m_dicInventory.TryGetValue(idItem, out var item) && await RemoveFromInventoryAsync(item, mode);
        }

        public async Task<bool> RemoveFromInventoryAsync(Item item, RemovalType mode = RemovalType.RemoveOnly)
        {
            if (!m_dicInventory.TryRemove(item.Identity, out _))
                return false;

            switch (mode)
            {
                case RemovalType.DropItem:
                    item.Position = Item.ItemPosition.Floor;
                    break;

                case RemovalType.Delete:
                    await Log.GmLogAsync("delete_item",
                        $"{item.Identity}, {item.Name}, {item.PlayerIdentity}\r\n\t{item.ToJson()}");
                    await item.DeleteAsync();
                    break;
            }

            if (mode != RemovalType.UnEquip)
                await m_user.SendAsync(new MsgItem(item.Identity, MsgItem.ItemActionType.InventoryRemove));
            return true;
        }

        public async Task<bool> SpendItemAsync(uint type, int nNum = 1, bool bSynchro = true)
        {
            return await SpendItemAsync(GetItemByType(type), nNum, bSynchro);
        }

        public async Task<bool> SpendItemAsync(Item item, int nNum = 1, bool bSynchro = true)
        {
            if (item == null)
                return false;

            return item.Position == Item.ItemPosition.Inventory &&
                   await RemoveFromInventoryAsync(item.Identity, RemovalType.Delete);
        }

        public async Task<bool> CombineItemAsync(uint idItem, uint idCombine)
        {
            return m_dicInventory.TryGetValue(idItem, out var item)
                   && m_dicInventory.TryGetValue(idCombine, out var combine)
                   && await CombineItemAsync(item, combine);
        }

        public async Task<bool> CombineItemAsync(Item item, Item combine)
        {
            if (item == null || combine == null || !item.IsPileEnable() || item.Type != combine.Type)
                return false;

            uint newNum = item.AccumulateNum + combine.AccumulateNum;
            if (newNum > item.MaxAccumulateNum)
            {
                item.AccumulateNum = newNum - combine.MaxAccumulateNum;
                combine.AccumulateNum = item.MaxAccumulateNum;
                await m_user.SendAsync(new MsgItemInfo(combine, MsgItemInfo.ItemMode.Update));
                await m_user.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
            }
            else
            {
                item.AccumulateNum = 0;
                await RemoveFromInventoryAsync(item, RemovalType.Delete);
                combine.AccumulateNum = newNum;
                await m_user.SendAsync(new MsgItemInfo(combine, MsgItemInfo.ItemMode.Update));
            }

            return true;
        }

        public Item FindCombineItem(Item item)
        {
            return m_dicInventory.Values.FirstOrDefault(x => x.Type == item.Type
                                                             && x.AccumulateNum < x.MaxAccumulateNum
                                                             && x.IsBound == item.IsBound);
        }

        public int MeteorAmount()
        {
            int amount = 0;
            foreach (var item in m_dicInventory.Values)
            {
                switch (item.Type)
                {
                    case Item.TYPE_METEOR:
                    case Item.TYPE_METEORTEAR:
                        amount++;
                        break;
                    case Item.TYPE_METEOR_SCROLL:
                        amount += 10;
                        break;
                }
            }

            return amount;
        }

        public int DragonBallAmount(bool bound = true, bool onlyBound = false)
        {
            int amount = 0;
            foreach (var item in m_dicInventory.Values)
            {
                if (!bound && item.IsBound)
                    continue;
                if (!item.IsBound && onlyBound)
                    continue;

                switch (item.Type)
                {
                    case Item.TYPE_DRAGONBALL:
                        amount++;
                        break;
                    case Item.TYPE_DRAGONBALL_SCROLL:
                        amount += 10;
                        break;
                }
            }

            return amount;
        }

        public async Task<bool> SpendMeteorsAsync(int amount)
        {
            if (amount > MeteorAmount())
                return false;

            List<Item> items = new List<Item>();
            int meteor = MultiGetItem(Item.TYPE_METEOR, Item.TYPE_METEORTEAR, 0, ref items);
            int meteorScroll = MultiGetItem(Item.TYPE_METEOR_SCROLL, Item.TYPE_METEOR_SCROLL, 0, ref items);

            int taken = 0;
            if (meteor >= amount)
            {
                foreach (var item in items.OrderBy(x => x.Type).Reverse())
                {
                    if (item.Type == Item.TYPE_METEOR || item.Type == Item.TYPE_METEORTEAR)
                    {
                        await RemoveFromInventoryAsync(item, RemovalType.Delete);
                        items.Remove(item);
                        taken++;
                    }

                    if (taken >= amount)
                        break;
                }

                return taken >= amount;
            }

            if (amount % 10 > 0 && meteor % 10 >= amount % 10)
            {
                int take = amount % 10;
                foreach (var item in items.OrderBy(x => x.Type).Reverse())
                {
                    if (item.Type == Item.TYPE_METEOR || item.Type == Item.TYPE_METEORTEAR)
                    {
                        await RemoveFromInventoryAsync(item, RemovalType.Delete);
                        items.Remove(item);
                        taken++;
                        take--;
                        amount--;
                    }

                    if (take == 0)
                        break;
                }
            }

            int amountMS = (int) Math.Ceiling(amount / 10f);
            int returnMeteor = 0;
            if (amount % 10 > 0)
                returnMeteor = 10 - amount % 10;

            foreach (var item in items.Where(x => x.Type == Item.TYPE_METEOR_SCROLL).Reverse())
            {
                await RemoveFromInventoryAsync(item, RemovalType.Delete);
                items.Remove(item);
                taken += 10;

                if (taken >= amount)
                {
                    while (returnMeteor > 0)
                    {
                        await AwardItemAsync(Item.TYPE_METEOR);
                        returnMeteor--;
                    }

                    return true;
                }
            }

            return true;
        }

        public async Task<bool> SpendDragonBallsAsync(int amount, bool allowBound = false)
        {
            if (amount > DragonBallAmount())
                return false;

            List<Item> items = new List<Item>();
            int db = MultiGetItem(Item.TYPE_DRAGONBALL, Item.TYPE_DRAGONBALL, 0, ref items);
            int dbScroll = MultiGetItem(Item.TYPE_DRAGONBALL_SCROLL, Item.TYPE_DRAGONBALL_SCROLL, 0,
                ref items);

            int taken = 0;
            if (db >= amount)
            {
                foreach (var item in items.OrderBy(x => x.Type).Reverse())
                {
                    if (!allowBound && item.IsBound)
                        continue;

                    if (item.Type == Item.TYPE_DRAGONBALL)
                    {
                        await RemoveFromInventoryAsync(item, RemovalType.Delete);
                        items.Remove(item);
                        taken++;
                    }

                    if (taken >= amount)
                        break;
                }

                return taken >= amount;
            }

            if (amount % 10 > 0 && db % 10 >= amount % 10)
            {
                int take = amount % 10;
                foreach (var item in items.OrderBy(x => x.Type).Reverse())
                {
                    if (!allowBound && item.IsBound)
                        continue;

                    if (item.Type == Item.TYPE_DRAGONBALL)
                    {
                        await RemoveFromInventoryAsync(item, RemovalType.Delete);
                        items.Remove(item);
                        taken++;
                        take--;
                        amount--;
                    }

                    if (take == 0)
                        break;
                }
            }

            int amountDbs = (int) Math.Ceiling(amount / 10f);

            if (dbScroll < amountDbs)
                return false;

            int returnDb = 0;
            if (amount % 10 > 0)
                returnDb = 10 - amount % 10;

            foreach (var item in items.Where(x => x.Type == Item.TYPE_DRAGONBALL_SCROLL).Reverse())
            {
                if (!allowBound && item.IsBound)
                    continue;

                await RemoveFromInventoryAsync(item, RemovalType.Delete);
                items.Remove(item);
                taken += 10;

                if (taken >= amount)
                {
                    while (returnDb > 0)
                    {
                        await AwardItemAsync(Item.TYPE_DRAGONBALL);
                        returnDb--;
                    }

                    return true;
                }
            }

            return true;
        }

        public async Task<bool> MultiSpendItemAsync(uint idFirst, uint idLast, int nNum, bool dontAllowBound = false)
        {
            int temp = nNum;
            List<Item> items = new List<Item>();
            if (MultiGetItem(idFirst, idLast, nNum, ref items, dontAllowBound) < nNum)
                return false;

            foreach (var item in items)
            {
                if (dontAllowBound && item.IsBound && (item.Itemtype.Monopoly & 1) == 0)
                    continue;

                nNum--;
                await RemoveFromInventoryAsync(item, RemovalType.Delete);
            }

            if (nNum > 0)
                await Log.WriteLogAsync(LogLevel.Error, $"Something went wrong when MultiSpendItem({idFirst}, {idLast}, {temp}) {nNum} left???");
            return nNum == 0;
        }

        public int MultiGetItem(uint idFirst, uint idLast, int nNum, ref List<Item> pItems, bool dontAllowBound = false)
        {
            int nAmount = 0;
            foreach (var item in m_dicInventory
                .Values
                .Where(x => x.Type >= idFirst && x.Type <= idLast)
                .OrderBy(x => x.Type))
            {
                if (dontAllowBound && item.IsBound && (item.Itemtype.Monopoly & 1) == 0)
                    continue;

                pItems.Add(item);
                nAmount += 1;

                if (nNum > 0 && nAmount >= nNum)
                    return nAmount;
            }

            return nAmount;
        }

        public bool MultiCheckItem(uint idFirst, uint idLast, int nNum, bool disallowBound = false)
        {
            int nAmount = 0;
            foreach (var item in m_dicInventory.Values.Where(x => x.Type >= idFirst && x.Type <= idLast))
            {
                if (disallowBound && item.IsBound && (item.Itemtype.Monopoly & 1) == 0)
                    continue;

                nAmount += 1;
                if (nAmount >= nNum)
                    return true;
            }

            return nAmount >= nNum;
        }

        public async Task<int> RandDropItemAsync(int nMapType, int nChance)
        {
            if (m_user == null)
                return 0;
            int nDropNum = 0;
            foreach (var item in m_dicInventory.Values)
            {
                if (await Kernel.ChanceCalcAsync(nChance))
                {
                    if (item.IsNeverDropWhenDead() || item.IsSuspicious())
                        continue;

                    switch (nMapType)
                    {
                        case 0: // NONE
                            break;
                        case 1: // PK
                        case 2: // SYN
                            if (!item.IsArmor())
                                continue;
                            break;
                        case 3: // PRISON
                            break;
                    }

                    var pos = new Point(m_user.MapX, m_user.MapY);
                    if (m_user.Map.FindDropItemCell(5, ref pos))
                    {
                        if (!await RemoveFromInventoryAsync(item.Identity, RemovalType.DropItem))
                            continue;

                        await item.ChangeOwnerAsync(0, Item.ChangeOwnerType.DropItem);

                        var pMapItem = new MapItem((uint) IdentityGenerator.MapItem.GetNextIdentity);
                        if (await pMapItem.CreateAsync(m_user.Map, pos, item, m_user.Identity))
                        {
                            await pMapItem.EnterMapAsync();
                            await item.SaveAsync();
                            await Log.GmLogAsync("drop_item",
                                $"{m_user.Name}({m_user.Identity}) drop item:[id={item.Identity}, type={item.Type}], dur={item.Durability}, max_dur={item.MaximumDurability}\n\t{item.ToJson()}");
                        }
                        else
                        {
                            IdentityGenerator.MapItem.ReturnIdentity(pMapItem.Identity);
                        }

                        nDropNum++;
                    }
                }
            }

            return nDropNum;
        }

        public async Task<int> RandDropItemAsync(int nDropNum)
        {
            if (m_user == null)
                return 0;

            var temp = new List<Item>();
            foreach (var item in m_dicInventory.Values)
            {
                if (item.IsNeverDropWhenDead() || item.IsSuspicious())
                    continue;
                temp.Add(item);
            }

            int nTotalItemCount = temp.Count;
            if (nTotalItemCount == 0)
                return 0;
            int nRealDropNum = 0;
            for (int i = 0; i < Math.Min(nDropNum, nTotalItemCount); i++)
            {
                int nIdx = await Kernel.NextAsync(nTotalItemCount);
                Item item;
                try
                {
                    item = temp[nIdx];
                }
                catch
                {
                    continue;
                }

                var pos = new Point(m_user.MapX, m_user.MapY);
                if (m_user.Map.FindDropItemCell(5, ref pos))
                {
                    if (!await RemoveFromInventoryAsync(item.Identity, RemovalType.DropItem))
                        continue;

                    await item.ChangeOwnerAsync(0, Item.ChangeOwnerType.DropItem);

                    var pMapItem = new MapItem((uint) IdentityGenerator.MapItem.GetNextIdentity);
                    if (await pMapItem.CreateAsync(m_user.Map, pos, item, m_user.Identity))
                    {
                        await pMapItem.EnterMapAsync();
                        await item.SaveAsync();
                        await Log.GmLogAsync("drop_item",
                            $"{m_user.Name}({m_user.Identity}) drop item:[id={item.Identity}, type={item.Type}], dur={item.Durability}, max_dur={item.MaximumDurability}\n\t{item.ToJson()}");
                    }
                    else
                    {
                        IdentityGenerator.MapItem.ReturnIdentity(pMapItem.Identity);
                    }

                    nRealDropNum++;
                }
            }

            return nRealDropNum;
        }

        public async Task<bool> RandDropEquipmentAsync(Character attacker)
        {
            if (m_dicEquipment.Count == 0 || attacker == null)
                return false;

            List<Item> items = m_dicEquipment.Values.Where(x => !x.IsArrowSort() && !x.IsGourd() && !x.IsGourd() && !x.IsSuspicious()).ToList();
            Item item = items[await Kernel.NextAsync(items.Count) % items.Count];

            if (!await UnEquipAsync(item.Position))
                return false;

            await item.DoUnlockAsync();

            if (await m_user.DropItemAsync(item.Identity, m_user.MapX, m_user.MapY, true))
            {
                await Kernel.RoleManager.BroadcastMsgAsync(string.Format(Language.StrDropEquipment, m_user.Name), MsgTalk.TalkChannel.Talk, Color.Red);
                await Log.GmLogAsync("detain",
                    $"[{m_user.Identity}] {m_user.Name} has dropped {item.Identity} to [{attacker.Identity}] {attacker.Name} dying at {m_user.MapIdentity}[{m_user.MapX},{m_user.MapY}]");
            }
            else
            {
                await attacker.UserPackage.AddItemAsync(item);
            }
            return true;
        }

        public int InventoryCount => m_dicInventory.Count;

        public bool IsPackSpare(int size)
        {
            return m_dicInventory.Count + size <= MAX_INVENTORY_CAPACITY;
        }

        public bool IsPackFull()
        {
            return m_dicInventory.Count >= MAX_INVENTORY_CAPACITY;
        }

        public async Task<bool> AddToStorageAsync(uint idStorage, Item item, MsgPackage.StorageType mode, bool sync)
        {
            if (item.Position != Item.ItemPosition.Inventory && sync)
                return false;

            BaseNpc npc = null;
            Item chestItem = null;
            List<Item> items = null;
            int maxStorage = 0;
            if (mode == MsgPackage.StorageType.Storage || mode == MsgPackage.StorageType.Trunk)
            {
                npc = Kernel.RoleManager.GetRole(idStorage) as BaseNpc;
                if (npc == null)
                    return false;

                if (npc.Data3 != 0)
                    maxStorage = npc.Data3;
                else maxStorage = 40;

                if (!m_dicWarehouses.TryGetValue(idStorage, out items))
                    m_dicWarehouses.TryAdd(idStorage, items = new List<Item>());
            }
            else if (mode == MsgPackage.StorageType.Chest)
            {
                chestItem = this[idStorage];
                if (chestItem == null || chestItem.GetItemSort() != (Item.ItemSort) 11)
                    return false;

                maxStorage = (int) (chestItem.Type % 1000);
                if (!m_dicSashes.TryGetValue(idStorage, out items))
                    m_dicSashes.TryAdd(idStorage, items = new List<Item>());
            }
            else
            {
                if (m_user.IsPm())
                    await m_user.SendAsync($"AddToStorageAsync::Invalid storage type: {mode}");
                return false;
            }

            if (!item.CanBeStored())
                return false;

            if (items == null || StorageSize(idStorage, mode) >= maxStorage)
                return false;

            if (sync && !await RemoveFromInventoryAsync(item, RemovalType.RemoveAndDisappear))
                return false;

            item.OwnerIdentity = idStorage;
            item.Position = (Item.ItemPosition)(200 + (byte)mode / 10);
            items.Add(item);
            await item.SaveAsync();

            if (sync)
            {
                await m_user.SendAsync(new MsgPackage
                {
                    Identity = item.OwnerIdentity,
                    Action = MsgPackage.WarehouseMode.CheckIn,
                    Mode = mode,
                    Items = new List<MsgPackage.WarehouseItem>
                    {
                        new MsgPackage.WarehouseItem
                        {
                            Identity = item.Identity,
                            Type = item.Type,
                            SocketOne = item.SocketOne,
                            SocketTwo = item.SocketTwo,
                            Blessing = (byte) item.Blessing,
                            Enchantment = item.Enchantment,
                            Magic1 = item.Effect,
                            Magic3 = item.Plus,
                            Color = item.Color,
                            Locked = item.IsLocked(),
                            Bound = item.IsBound
                        }
                    }
                });
            }
            return true;
        }

        public async Task<bool> GetFromStorageAsync(uint idStorage, uint idItem, MsgPackage.StorageType mode, bool sync)
        {
            List<Item> storage = null;
            if (mode == MsgPackage.StorageType.Storage || mode == MsgPackage.StorageType.Trunk)
            {
                if (!m_dicWarehouses.TryGetValue(idStorage, out storage))
                    return false;
            }
            else if (mode == MsgPackage.StorageType.Chest)
            {
                if (!m_dicSashes.TryGetValue(idStorage, out storage))
                    return false;
            }

            if (storage == null || storage.All(x => x.Identity != idItem))
                return false;

            if (!m_user.UserPackage.IsPackSpare(1))
                return false;

            Item item = storage.Find(x => x.Identity == idItem);
            if (item == null || storage.RemoveAll(x => x.Identity == idItem) == 0)
                return false;

            if (sync)
            {
                await m_user.SendAsync(new MsgPackage
                {
                    Identity = item.OwnerIdentity,
                    Action = MsgPackage.WarehouseMode.CheckOut,
                    Mode = mode,
                    Param = item.Identity
                });
            }

            await m_user.UserPackage.AddItemAsync(item);
            return true;
        }

        public List<Item> GetStorageItems(uint idStorage, MsgPackage.StorageType type)
        {
            switch (type)
            {
                case MsgPackage.StorageType.Storage:
                case MsgPackage.StorageType.Trunk:
                    return m_dicWarehouses.TryGetValue(idStorage, out var storage) ? storage : new List<Item>();
                case MsgPackage.StorageType.Chest:
                    return m_dicSashes.TryGetValue(idStorage, out var chest) ? chest : new List<Item>();
            }
            return new List<Item>();
        }

        public int StorageSize(uint idStorage, MsgPackage.StorageType type)
        {
            switch (type)
            {
                case MsgPackage.StorageType.Storage:
                    return m_dicWarehouses.TryGetValue(idStorage, out var storage) ? storage.Count : 0;
                case MsgPackage.StorageType.Chest:
                    return m_dicSashes.TryGetValue(idStorage, out var chest) ? chest.Count : 0;
            }
            return 0;
        }

        /// <summary>
        ///     Sent only on login!!!
        /// </summary>
        public async Task SendAsync()
        {
            foreach (var item in m_dicEquipment.Values)
            {
                await m_user.SendAsync(new MsgItemInfo(item));
            }

            foreach (var item in m_dicInventory.Values)
            {
                await m_user.SendAsync(new MsgItemInfo(item));
            }
        }

        public Item GetItemByType(uint type)
        {
            return m_dicInventory.Values.FirstOrDefault(x => x.Type == type);
        }

        public Item FindByIdentity(uint id)
        {
            return m_dicEquipment.Values.FirstOrDefault(x => x.Identity == id) ?? m_dicInventory.Values.FirstOrDefault(x => x.Identity == id);
        }

        public Item FindByIdentityAnywhere(uint id)
        {
            Item item = FindByIdentity(id);
            if (item != null)
                return item;

            if (m_dicSashes.Values.Any(sash => (item = sash.FirstOrDefault(x => x.Identity == id)) != null))
                return item;

            if (m_dicWarehouses.Values.Any(
                warehouse => (item = warehouse.FirstOrDefault(x => x.Identity == id)) != null))
                return item;
            return null;
        }

        public async Task ClearInventoryAsync()
        {
            foreach (var item in m_dicInventory.Values)
            {
                await RemoveFromInventoryAsync(item, RemovalType.RemoveAndDisappear);
                await item.DeleteAsync(Item.ChangeOwnerType.ClearInventory);
                await Log.GmLogAsync("clear_inventory", $"User[{m_user.Identity}:{m_user.Name}] deleted item {item.Identity}.\r\n{item.ToJson()}");
            }
        }

        public enum RemovalType
        {
            /// <summary>
            ///     Item will be removed and disappear, but wont be deleted.
            /// </summary>
            RemoveAndDisappear,

            /// <summary>
            ///     Item will be internally removed only. No client interaction and also wont be deleted.
            /// </summary>
            RemoveOnly,

            /// <summary>
            ///     Item will be removed and deleted.
            /// </summary>
            Delete,

            /// <summary>
            ///     Item will be set to floor and will be updated. No delete.
            /// </summary>
            DropItem,
            UnEquip
        }
    }
}