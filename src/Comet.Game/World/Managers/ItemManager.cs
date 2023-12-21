// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Item Manager.cs
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
using System.Linq;
using System.Threading.Tasks;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets;
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Game.States.NPCs;
using Comet.Shared;

#endregion

namespace Comet.Game.World.Managers
{
    public sealed class ItemManager
    {
        private Dictionary<uint, DbItemtype> m_dicItemtype;
        private Dictionary<ulong, DbItemAddition> m_dicItemAddition;
        public static BaseNpc Confiscator => Kernel.RoleManager.FindRole<BaseNpc>(4450);

        public async Task InitializeAsync()
        {
            m_dicItemtype = new Dictionary<uint, DbItemtype>();
            foreach (var item in await ItemtypeRepository.GetAsync())
            {
                m_dicItemtype.TryAdd(item.Type, item);
            }

            m_dicItemAddition = new Dictionary<ulong, DbItemAddition>();
            foreach (var addition in await ItemAdditionRepository.GetAsync())
            {
                m_dicItemAddition.TryAdd(AdditionKey(addition.TypeId, addition.Level), addition);
            }
        }

        public List<DbItemtype> GetByRange(int mobLevel, int tolerationMin, int tolerationMax, int maxLevel = 120)
        {
            return m_dicItemtype.Values.Where(x =>
                x.ReqLevel >= mobLevel - tolerationMin && x.ReqLevel <= mobLevel + tolerationMax &&
                x.ReqLevel <= maxLevel).ToList();
        }

        public DbItemtype GetItemtype(uint type)
        {
            return m_dicItemtype.TryGetValue(type, out var item) ? item : null;
        }

        public DbItemAddition GetItemAddition(uint type, byte level)
        {
            return m_dicItemAddition.TryGetValue(AdditionKey(type, level), out var item) ? item : null;
        }

        private ulong AdditionKey(uint type, byte level)
        {
            uint key = type;
            Item.ItemSort sort = Item.GetItemSort(type);
            if (sort == Item.ItemSort.ItemsortWeaponSingleHand && Item.GetItemSubType(type) != 421)
            {
                key = type / 100000 * 100000 + type % 1000 + 44000 - type % 10;
            }
            else if (sort == Item.ItemSort.ItemsortWeaponDoubleHand && !Item.IsBow(type))
            {
                key = type / 100000 * 100000 + type % 1000 + 55000 - type % 10;
            }
            else
            {
                key = type / 1000 * 1000 + (type % 1000 - type % 10);
            }

            return (key + ((ulong) level << 32));
        }

        public async Task<bool> DetainItemAsync(Character discharger, Character detainer)
        {
            List<Item> items = new List<Item>();
            for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
            {
                switch (pos)
                {
                    case Item.ItemPosition.Headwear:
                    case Item.ItemPosition.Necklace:
                    case Item.ItemPosition.Ring:
                    case Item.ItemPosition.RightHand:
                    case Item.ItemPosition.Armor:
                    case Item.ItemPosition.LeftHand:
                    case Item.ItemPosition.Boots:
                    case Item.ItemPosition.AttackTalisman:
                    case Item.ItemPosition.DefenceTalisman:
                    case Item.ItemPosition.Crop:
                        {
                            if (discharger.UserPackage[pos] == null) continue;
                            if (discharger.UserPackage[pos].IsArrowSort()) continue;
                            if (discharger.UserPackage[pos].IsSuspicious()) continue;
                            items.Add(discharger.UserPackage[pos]);
                            continue;
                        }
                }
            }

            Item item = items[await Kernel.NextAsync(items.Count)%items.Count];

            if (item == null)
                return false;
            if (item.IsArrowSort())
                return false;
            if (item.IsMount())
                return false;
            if (item.IsSuspicious())
                return false;
            if (item.PlayerIdentity != discharger.Identity) // item must be owned by the discharger
                return false;

            await discharger.UserPackage.UnEquipAsync(item.Position, UserPackage.RemovalType.RemoveAndDisappear);
            item.Position = Item.ItemPosition.Detained;
            await item.SaveAsync();

            await Log.GmLogAsync("discharge_item", $"did:{discharger.Identity},dname:{discharger.Name},detid:{detainer.Identity},detname:{detainer.Name},itemid:{item.Identity},mapid:{discharger.MapIdentity}");

            var dbDetain = new DbDetainedItem
            {
                ItemIdentity = item.Identity,
                TargetIdentity = discharger.Identity,
                TargetName = discharger.Name, 
                HunterIdentity = detainer.Identity,
                HunterName = detainer.Name,
                HuntTime = UnixTimestamp.Now(),
                RedeemPrice = (ushort) GetDetainPrice(item)
            };
            if (!await BaseRepository.SaveAsync(dbDetain))
                return false;

            await discharger.BroadcastRoomMsgAsync(new MsgAction
            {
                Identity = discharger.Identity,
                Data = dbDetain.Identity,
                X = discharger.MapX,
                Y = discharger.MapY,
                Action = MsgAction.ActionType.ItemDetainedEx
                
            }, true);
            await discharger.SendAsync(new MsgAction
            {
                Identity = discharger.Identity,
                X = discharger.MapX,
                Y = discharger.MapY,
                Action = MsgAction.ActionType.ItemDetained
            });
            var detainFloorId = IdentityGenerator.MapItem.GetNextIdentity;
            await discharger.BroadcastRoomMsgAsync(new MsgMapItem
            {
                Identity = (uint) detainFloorId,
                Itemtype = item.Type,
                MapX = (ushort) (discharger.MapX + 2),
                MapY = discharger.MapY,
                Mode = DropType.DetainItem
            }, true);
            IdentityGenerator.MapItem.ReturnIdentity(detainFloorId);

            await discharger.SendAsync(new MsgDetainItemInfo(dbDetain, item, MsgDetainItemInfo.Mode.DetainPage));
            await detainer.SendAsync(new MsgDetainItemInfo(dbDetain, item, MsgDetainItemInfo.Mode.ClaimPage));

            if (Confiscator != null)
            {
                await discharger.SendAsync(string.Format(Language.StrDropEquip, item.Name, detainer.Name, Confiscator.Name, Confiscator.MapX, Confiscator.MapY), MsgTalk.TalkChannel.Talk);
                await detainer.SendAsync(string.Format(Language.StrKillerEquip, discharger.Name));
            }
            return true;
        }

        /// <summary>
        /// Claims an expired item or Conquer Points to the hunter.
        /// </summary>
        /// <returns>True if the item has been successfully claimed.</returns>
        public async Task<bool> ClaimDetainRewardAsync(uint idDetain, Character user)
        {
            DbDetainedItem dbDetain = await DbDetainedItem.GetByIdAsync(idDetain);
            if (dbDetain == null)
                return false;

            if (dbDetain.HunterIdentity != user.Identity)
                return false;

            if (dbDetain.ItemIdentity != 0 && dbDetain.HuntTime + MsgDetainItemInfo.MAX_REDEEM_SECONDS > UnixTimestamp.Now())
                return false;

            if (!user.UserPackage.IsPackSpare(1))
            {
                await user.SendAsync(Language.StrYourBagIsFull);
                return false;
            }

            if (dbDetain.ItemIdentity == 0) // Conquer Points
            {
                await user.AwardConquerPointsAsync(dbDetain.RedeemPrice);

                await Kernel.RoleManager.BroadcastMsgAsync(string.Format(Language.StrGetEmoneyBonus, dbDetain.HunterName, dbDetain.TargetName, dbDetain.RedeemPrice), 
                    MsgTalk.TalkChannel.Talk);
            }
            else
            {
                DbItem dbItem = await ItemRepository.GetByIdAsync(dbDetain.ItemIdentity);
                if (dbItem == null)
                    return false;

                Item item = new Item();
                if (!await item.CreateAsync(dbItem) || item.Position != Item.ItemPosition.Detained)
                    return false;

                await item.ChangeOwnerAsync(user.Identity, Item.ChangeOwnerType.DetainEquipment);
                item.Position = Item.ItemPosition.Inventory;

                await user.UserPackage.AddItemAsync(item);

                await Kernel.RoleManager.BroadcastMsgAsync(string.Format(Language.StrGetEquipBonus, dbDetain.HunterName, dbDetain.TargetName, item.FullName),
                    MsgTalk.TalkChannel.Talk);
            }

            await BaseRepository.DeleteAsync(dbDetain);
            return true;
        }

        /// <summary>
        /// Claim a detained item back to it's owner.
        /// </summary>
        /// <returns>True if the item has been successfully detained.</returns>
        public async Task<bool> ClaimDetainedItemAsync(uint idDetain, Character user)
        {
            DbDetainedItem dbDetain = await DbDetainedItem.GetByIdAsync(idDetain);
            if (dbDetain == null)
                return false;

            if (dbDetain.TargetIdentity != user.Identity)
                return false;

            if (dbDetain.HuntTime + MsgDetainItemInfo.MAX_REDEEM_SECONDS < UnixTimestamp.Now())
                return false;

            if (!user.UserPackage.IsPackSpare(1))
            {
                await user.SendAsync(Language.StrYourBagIsFull);
                return false;
            }

            if (dbDetain.ItemIdentity == 0)
                return false;

            DbItem dbItem = await ItemRepository.GetByIdAsync(dbDetain.ItemIdentity);
            if (dbItem == null)
                return false;

            Item item = new Item();
            if (!await item.CreateAsync(dbItem) || item.Position != Item.ItemPosition.Detained)
                return false;

            if (!await user.SpendConquerPointsAsync(dbDetain.RedeemPrice))
            {
                await user.SendAsync(Language.StrNotEnoughEmoney);
                return false;
            }

            item.Position = Item.ItemPosition.Inventory;
            await user.UserPackage.AddItemAsync(item);

            await Kernel.RoleManager.BroadcastMsgAsync(string.Format(Language.StrRedeemEquip, user.Name, dbDetain.RedeemPrice, dbDetain.HunterName),
                MsgTalk.TalkChannel.Talk);

            dbDetain.ItemIdentity = 0;
            await BaseRepository.SaveAsync(dbDetain);

            Character hunter = Kernel.RoleManager.GetUser(dbDetain.HunterIdentity);
            if (hunter != null)
            {
                await hunter.SendAsync(new MsgItem
                {
                    Action = MsgItem.ItemActionType.RedeemEquipment,
                    Identity = dbDetain.Identity,
                    Command = dbDetain.TargetIdentity,
                    Argument2 = dbDetain.RedeemPrice
                });

                if (Confiscator != null)
                {
                    await hunter.SendAsync(string.Format(Language.StrHasEmoneyBonus, dbDetain.TargetName, Confiscator.Name, Confiscator.MapX, Confiscator.MapY), MsgTalk.TalkChannel.Talk);
                }
            }

            return true;
        }

        public static int GetDetainPrice(Item item)
        {
            int dwPrice = 10;

            if (item.GetQuality() == 9) // if super +500CPs
                dwPrice += 50;

            switch (item.Plus) // (+n)
            {
                case 1: dwPrice += 1; break;
                case 2: dwPrice += 2; break;
                case 3: dwPrice += 5; break;
                case 4: dwPrice += 10; break;
                case 5: dwPrice += 30; break;
                case 6: dwPrice += 90; break;
                case 7: dwPrice += 270; break;
                case 8: dwPrice += 600; break;
                case 9:
                case 10:
                case 11:
                case 12: dwPrice += 1200; break;
            }

            if (item.IsWeapon()) // if weapon
            {
                if (item.SocketTwo > Item.SocketGem.NoSocket)
                    dwPrice += 100;
                else if (item.SocketOne > Item.SocketGem.NoSocket)
                    dwPrice += 10;
            }
            else // if not
            {
                if (item.SocketTwo > Item.SocketGem.NoSocket)
                    dwPrice += 150;
                else if (item.SocketOne > Item.SocketGem.NoSocket)
                    dwPrice += 500;
            }

            //if (item.ArtifactIsPermanent())
            //{
            //    switch (item.Artifact.ArtifactLevel)
            //    {
            //        case 1: dwPrice += 30; break;
            //        case 2: dwPrice += 90; break;
            //        case 3: dwPrice += 180; break;
            //        case 4: dwPrice += 300; break;
            //        case 5: dwPrice += 450; break;
            //        case 6: dwPrice += 600; break;
            //    }
            //}

            //if (item.RefineryIsPermanent())
            //{
            //    switch (item.RefineryLevel)
            //    {
            //        case 1: dwPrice += 30; break;
            //        case 2: dwPrice += 90; break;
            //        case 3: dwPrice += 200; break;
            //        case 4: dwPrice += 400; break;
            //        case 5: dwPrice += 600; break;
            //    }
            //}

            return dwPrice;
        }
    }
}