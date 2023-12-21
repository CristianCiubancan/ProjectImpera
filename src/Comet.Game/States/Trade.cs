// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Trade.cs
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Packets;
using Comet.Game.States.Items;

namespace Comet.Game.States
{
    public sealed class Trade
    {
        private const int MAX_TRADE_ITEMS = 20;
        private const int MAX_TRADE_MONEY = 1000000000;
        private const int MAX_TRADE_EMONEY = 1000000000;
        
        private ConcurrentDictionary<uint, Item> m_dicItems1 = new ConcurrentDictionary<uint, Item>();
        private ConcurrentDictionary<uint, Item> m_dicItems2 = new ConcurrentDictionary<uint, Item>();

        private uint m_money1 = 0, m_money2 = 0;
        private uint m_emoney1 = 0, m_emoney2 = 0;

        private bool m_accept1 = false, m_accept2 = false;

        public Trade(Character p1, Character p2)
        {
            User1 = p1;
            User2 = p2;

            User1.Trade = this;
            User2.Trade = this;
        }
        
        public Character User1 { get; }
        public Character User2 { get; }

        public bool Accepted => m_accept1 && m_accept2;

        public async Task<bool> AddItemAsync(uint idItem, Character sender)
        {
            if (sender.Identity != User1.Identity
                && sender.Identity != User2.Identity)
                return false;

            Character target = sender.Identity == User1.Identity ? User2 : User1;
            ConcurrentDictionary<uint, Item> items = User1.Identity == sender.Identity ? m_dicItems1 : m_dicItems2;

            Item item = sender.UserPackage[idItem];
            if (item == null)
            {
                await sender.SendAsync(Language.StrNotToTrade);
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            if (items.ContainsKey(idItem))
            {
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            if (item.IsMonopoly() || item.IsBound)
            {
                await sender.SendAsync(Language.StrNotToTrade);
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            if (item.IsSuspicious())
            {
                await sender.SendAsync(Language.StrNotToTrade);
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            if (item.IsLocked() && !sender.IsValidTradePartner(target.Identity))
            {
                await sender.SendAsync(Language.StrNotToTrade);
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            if (item.SyndicateIdentity != 0)
            {
                await sender.SendAsync(Language.StrNotToTrade);
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            if (sender.Booth?.QueryItem(item.Identity) != null)
            {
                await sender.SendAsync(Language.StrNotToTrade);
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }
            
            if (items.Count >= MAX_TRADE_ITEMS)
            {
                await sender.SendAsync(Language.StrTradeSashFull);
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            if (!target.UserPackage.IsPackSpare(1))
            {
                await target.SendAsync(Language.StrTradeYourBagIsFull);
                await sender.SendAsync(Language.StrTradeTargetBagIsFull);
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            items.TryAdd(item.Identity, item);
            await target.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Trade));
            return true;
        }

        public async Task<bool> AddMoneyAsync(uint amount, Character sender)
        {
            if (sender.Identity != User1.Identity
                && sender.Identity != User2.Identity)
                return false;

            Character target = sender.Identity == User1.Identity ? User2 : User1;

            if (amount > MAX_TRADE_MONEY)
            {
                await sender.SendAsync(string.Format(Language.StrTradeMuchMoney, MAX_TRADE_MONEY));
                await SendCloseAsync();
                return false;
            }

            if (sender.Silvers < amount)
            {
                await sender.SendAsync(Language.StrNotEnoughMoney);
                await SendCloseAsync();
                return false;
            }

            if (sender.Identity == User1.Identity)
                m_money1 = amount;
            else
                m_money2 = amount;

            await target.SendAsync(new MsgTrade
            {
                Data = amount,
                Action = MsgTrade.TradeAction.ShowMoney
            });
            return true;
        }

        public async Task<bool> AddEmoneyAsync(uint amount, Character sender)
        {
            if (sender.Identity != User1.Identity
                && sender.Identity != User2.Identity)
                return false;

            Character target = sender.Identity == User1.Identity ? User2 : User1;

            if (amount > MAX_TRADE_EMONEY)
            {
                await sender.SendAsync(string.Format(Language.StrTradeMuchEmoney, MAX_TRADE_EMONEY));
                await SendCloseAsync();
                return false;
            }

            if (sender.ConquerPoints < amount)
            {
                await sender.SendAsync(Language.StrNotEnoughMoney);
                await SendCloseAsync();
                return false;
            }

            if (sender.Identity == User1.Identity)
                m_emoney1 = amount;
            else
                m_emoney2 = amount;

            await target.SendAsync(new MsgTrade
            {
                Data = amount,
                Action = MsgTrade.TradeAction.ShowConquerPoints
            });
            return true;
        }

        public async Task AcceptAsync(uint acceptId)
        {
            if (acceptId == User1.Identity)
            {
                m_accept1 = true;
                await User2.SendAsync(new MsgTrade
                {
                    Action = MsgTrade.TradeAction.Accept,
                    Data = acceptId
                });
            }
            else if (acceptId == User2.Identity)
            {
                m_accept2 = true;
                await User1.SendAsync(new MsgTrade
                {
                    Action = MsgTrade.TradeAction.Accept,
                    Data = acceptId
                });
            }

            if (!Accepted)
                return;

            bool success = m_dicItems1.Values.All(x => User1.UserPackage[x.Identity] != null && !x.IsBound && !x.IsMonopoly());
            success = m_dicItems2.Values.All(x => User2.UserPackage[x.Identity] != null && !x.IsBound && !x.IsMonopoly());

            if (!User1.UserPackage.IsPackSpare(m_dicItems2.Count))
                success = false;
            if (!User2.UserPackage.IsPackSpare(m_dicItems1.Count))
                success = false;

            if (m_money1 > User1.Silvers || m_emoney1 > User1.ConquerPoints)
                success = false;
            if (m_money2 > User2.Silvers || m_emoney2 > User2.ConquerPoints)
                success = false;

            if (!success)
            {
                await SendCloseAsync();
                return;
            }

            DbTrade dbTrade = new DbTrade
            {
                UserIpAddress = User1.Client.IPAddress,
                UserMacAddress = User1.Client.MacAddress,
                TargetIpAddress = User2.Client.IPAddress,
                TargetMacAddress = User2.Client.MacAddress,
                MapIdentity = User1.MapIdentity,
                TargetEmoney = m_emoney2,
                TargetMoney = m_money2,
                UserEmoney = m_emoney1,
                UserMoney = m_money1,
                TargetIdentity = User2.Identity,
                UserIdentity = User1.Identity,
                TargetX = User2.MapX,
                TargetY = User2.MapY,
                UserX = User1.MapX,
                UserY = User1.MapY,
                Timestamp = DateTime.Now
            };
            await BaseRepository.SaveAsync(dbTrade);

            await SendCloseAsync();

            await User1.SpendMoneyAsync((int) m_money1);
            await User2.AwardMoneyAsync((int) m_money1);

            await User2.SpendMoneyAsync((int) m_money2);
            await User1.AwardMoneyAsync((int) m_money2);

            await User1.SpendConquerPointsAsync((int) m_emoney1);
            await User2.AwardConquerPointsAsync((int) m_emoney1);

            await User2.SpendConquerPointsAsync((int) m_emoney2);
            await User1.AwardConquerPointsAsync((int) m_emoney2);

            List<DbTradeItem> dbItemsRecordTrack = new List<DbTradeItem>(41);
            foreach (var item in m_dicItems1.Values)
            {
                if (item.IsMonopoly() || item.IsBound)
                    continue;

                await User1.UserPackage.RemoveFromInventoryAsync(item, UserPackage.RemovalType.RemoveAndDisappear);
                await item.ChangeOwnerAsync(User2.Identity, Item.ChangeOwnerType.TradeItem);
                await User2.UserPackage.AddItemAsync(item);

                dbItemsRecordTrack.Add(new DbTradeItem
                {
                    TradeIdentity = dbTrade.Identity,
                    SenderIdentity = User1.Identity,
                    ItemIdentity = item.Identity,
                    Itemtype = item.Type,
                    Chksum = (uint) item.ToJson().GetHashCode(),
                    JsonData = item.ToJson()
                });
            }

            foreach (var item in m_dicItems2.Values)
            {
                if (item.IsMonopoly() || item.IsBound)
                    continue;

                await User2.UserPackage.RemoveFromInventoryAsync(item, UserPackage.RemovalType.RemoveAndDisappear);
                await item.ChangeOwnerAsync(User1.Identity, Item.ChangeOwnerType.TradeItem);
                await User1.UserPackage.AddItemAsync(item);

                dbItemsRecordTrack.Add(new DbTradeItem
                {
                    TradeIdentity = dbTrade.Identity,
                    SenderIdentity = User2.Identity,
                    ItemIdentity = item.Identity,
                    Itemtype = item.Type,
                    Chksum = (uint)item.ToJson().GetHashCode(),
                    JsonData = item.ToJson()
                });
            }

            await BaseRepository.SaveAsync(dbItemsRecordTrack);

            await User1.SendAsync(Language.StrTradeSuccess);
            await User2.SendAsync(Language.StrTradeSuccess);
        }

        public async Task SendCloseAsync()
        {
            User1.Trade = null;
            User2.Trade = null;

            if (User1.Connection != Character.ConnectionStage.Disconnected)
            {
                await User1.SendAsync(new MsgTrade
                {
                    Action = MsgTrade.TradeAction.Fail,
                    Data = User2.Identity
                });
            }

            if (User2.Connection != Character.ConnectionStage.Disconnected)
            {
                await User2.SendAsync(new MsgTrade
                {
                    Action = MsgTrade.TradeAction.Fail,
                    Data = User1.Identity
                });
            }
        }

        private MsgTrade RemoveMsg(uint id)
        {
            return new MsgTrade
            {
                Action = MsgTrade.TradeAction.AddItemFail,
                Data = id
            };
        }
    }
}