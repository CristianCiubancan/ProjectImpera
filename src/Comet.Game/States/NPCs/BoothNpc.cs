// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - BoothNpc.cs
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
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Game.Packets;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Items;
using Comet.Game.World.Maps;

namespace Comet.Game.States.NPCs
{
    public class BoothNpc : BaseNpc
    {
        private Npc m_ownerNpc;
        private Character m_owner;
        private ConcurrentDictionary<uint, BoothItem> m_items = new ConcurrentDictionary<uint, BoothItem>();

        public BoothNpc(Character owner) 
            : base(owner.Identity % 1000000 + owner.Identity / 1000000 * 100000)
        {
            m_owner = owner;
        }

        public override async Task<bool> InitializeAsync()
        {
            m_ownerNpc = m_owner.Screen.Roles.Values.FirstOrDefault(x => x is Npc && x.MapX == m_owner.MapX - 2 && x.MapY == m_owner.MapY) as Npc;
            if (m_ownerNpc == null)
                return false;

            m_idMap = m_owner.MapIdentity;
            m_posX = (ushort)(m_owner.MapX + 1);
            m_posY = m_owner.MapY;

            Name = $"{m_owner.Name}";

            await m_owner.SetDirectionAsync(FacingDirection.SouthEast);
            await m_owner.SetActionAsync(EntityAction.Sit);

            return await base.InitializeAsync();
        }

        public string HawkMessage { get; set; }

        #region Items management

        public async Task QueryItemsAsync(Character requester)
        {
            if (GetDistance(requester) > Screen.VIEW_SIZE)
                return;

            foreach (var item in m_items.Values)
            {
                if (!ValidateItem(item.Identity))
                {
                    m_items.TryRemove(item.Identity, out _);
                    continue;
                }

                await requester.SendAsync(new MsgItemInfoEx(item) { TargetIdentity = Identity });
            }
        }

        public bool AddItem(Item item, uint value, MsgItem.Moneytype type)
        {
            BoothItem boothItem = new BoothItem();
            if (!boothItem.Create(item, Math.Min(value, int.MaxValue), type == MsgItem.Moneytype.Silver))
                return false;
            return m_items.TryAdd(boothItem.Identity, boothItem);
        }

        public BoothItem QueryItem(uint idItem)
        {
            return m_items.Values.FirstOrDefault(x => x.Identity == idItem);
        }

        public bool RemoveItem(uint idItem)
        {
            return m_items.TryRemove(idItem, out _);
        }

        public bool ValidateItem(uint id)
        {
            Item item = m_owner.UserPackage[id];
            if (item == null)
                return false;
            if (item.IsBound)
                return false;
            if (item.IsLocked())
                return false;
            if (item.IsSuspicious())
                return false;
            return true;
        }

        #endregion

        #region Enter and Leave Map

        public override Task EnterMapAsync()
        {
            return base.EnterMapAsync();
        }

        public override async Task LeaveMapAsync()
        {
            if (m_ownerNpc != null && m_owner.Connection != Character.ConnectionStage.Disconnected)
            {
                await m_owner.SetActionAsync(EntityAction.Stand);
                m_owner = null;
                m_ownerNpc = null;
            }
            m_items.Clear();
            await base.LeaveMapAsync();
        }

        #endregion

        #region Socket

        public override async Task SendSpawnToAsync(Character player)
        {
            await player.SendAsync(new MsgNpcInfoEx
            {
                Identity = Identity,
                Lookface = 406,
                Sort = 0,
                PosX = MapX,
                PosY = MapY,
                Name = Name,
                NpcType = BOOTH_NPC,
                Life = Life,
                MaxLife = MaxLife
            });

            if (!string.IsNullOrEmpty(HawkMessage))
                await player.SendAsync(new MsgTalk(m_owner.Identity, MsgTalk.TalkChannel.Vendor, Color.White,
                    HawkMessage));
        }

        #endregion
    }
}