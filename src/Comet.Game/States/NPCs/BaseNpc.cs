// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Base Npc.cs
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
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using Comet.Game.Database.Models;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Syndicates;
using Comet.Game.World.Maps;

#endregion

namespace Comet.Game.States.NPCs
{
    public abstract class BaseNpc : Role
    {
        #region Constants

        public const int
            NPC_NONE = 0, // Í¨ÓÃNPC
            SHOPKEEPER_NPC = 1, // ÉÌµêNPC
            TASK_NPC = 2, // ÈÎÎñNPC(ÒÑ×÷·Ï£¬½öÓÃÓÚ¼æÈÝ¾ÉÊý¾Ý)
            STORAGE_NPC = 3, // ¼Ä´æ´¦NPC
            TRUNCK_NPC = 4, // Ïä×ÓNPC
            FACE_NPC = 5, // ±äÍ·ÏñNPC
            FORGE_NPC = 6, // ¶ÍÔìNPC		(only use for client)
            EMBED_NPC = 7, // ÏâÇ¶NPC
            STATUARY_NPC = 9, // µñÏñNPC
            SYNFLAG_NPC = 10, // °ïÅÉ±ê¼ÇNPC
            ROLE_PLAYER = 11, // ÆäËûÍæ¼Ò		(only use for client)
            ROLE_HERO = 12, // ×Ô¼º			(only use for client)
            ROLE_MONSTER = 13, // ¹ÖÎï			(only use for client)
            BOOTH_NPC = 14, // °ÚÌ¯NPC		(CBooth class)
            SYNTRANS_NPC = 15, // °ïÅÉ´«ËÍNPC, ¹Ì¶¨ÄÇ¸ö²»ÒªÓÃ´ËÀàÐÍ! (ÓÃÓÚ00:00ÊÕ·Ñ)(LINKIDÎª¹Ì¶¨NPCµÄID£¬ÓëÆäËüÊ¹ÓÃLINKIDµÄ»¥³â)
            ROLE_BOOTH_FLAG_NPC = 16, // Ì¯Î»±êÖ¾NPC	(only use for client)
            ROLE_MOUSE_NPC = 17, // Êó±êÉÏµÄNPC	(only use for client)
            ROLE_MAGICITEM = 18, // ÏÝÚå»ðÇ½		(only use for client)
            ROLE_DICE_NPC = 19, // ÷»×ÓNPC
            ROLE_SHELF_NPC = 20, // ÎïÆ·¼Ü
            WEAPONGOAL_NPC = 21, // ÎäÆ÷°Ð×ÓNPC
            MAGICGOAL_NPC = 22, // Ä§·¨°Ð×ÓNPC
            BOWGOAL_NPC = 23, // ¹­¼ý°Ð×ÓNPC
            ROLE_TARGET_NPC = 24, // °¤´ò£¬²»´¥·¢ÈÎÎñ	(only use for client)
            ROLE_FURNITURE_NPC = 25, // ¼Ò¾ßNPC	(only use for client)
            ROLE_CITY_GATE_NPC = 26, // ³ÇÃÅNPC	(only use for client)
            ROLE_NEIGHBOR_DOOR = 27, // ÁÚ¾ÓµÄÃÅ
            ROLE_CALL_PET = 28, // ÕÙ»½ÊÞ	(only use for client)
            EUDEMON_TRAINPLACE_NPC = 29, // »ÃÊÞÑ±ÑøËù
            AUCTION_NPC = 30, // ÅÄÂòNPC	ÎïÆ·ÁìÈ¡NPC  LW
            ROLE_FAMILY_WAR_FLAG = 31,
            ROLE_CTFBASE_NPC = 40,
            ROLE_3DFURNITURE_NPC = 101, // 3D¼Ò¾ßNPC 
            SYN_NPC_WARLETTER = 110; //Ôö¼ÓÐÂµÄ£Î£Ð£ÃÀàÐÍ¡¡×¨ÃÅÓÃÀ´¡¡ÏÂÕ½ÊéµÄ¡¡°ïÅÉ£Î£Ð£Ã

        [Flags]
        public enum NpcSort
        {
            None = 0,
            Task = 1,           // ÈÎÎñÀà
            Recycle = 2,            // ¿É»ØÊÕÀà
            Scene = 4,          // ³¡¾°Àà(´øµØÍ¼Îï¼þ)
            LinkMap = 8,            // ¹ÒµØÍ¼Àà(LINKIDÎªµØÍ¼ID£¬ÓëÆäËüÊ¹ÓÃLINKIDµÄ»¥³â)
            DieAction = 16,         // ´øËÀÍöÈÎÎñ(LINKIDÎªACTION_ID£¬ÓëÆäËüÊ¹ÓÃLINKIDµÄ»¥³â)
            DelEnable = 32,         // ¿ÉÒÔÊÖ¶¯É¾³ý(²»ÊÇÖ¸Í¨¹ýÈÎÎñ)
            Event = 64,         // ´ø¶¨Ê±ÈÎÎñ, Ê±¼äÔÚdata3ÖÐ£¬¸ñÊ½ÎªMMWWHHMMSS¡£(LINKIDÎªACTION_ID£¬ÓëÆäËüÊ¹ÓÃLINKIDµÄ»¥³â)
            Table = 128,            // ´øÊý¾Ý±íÀàÐÍ

            //		NPCSORT_SHOP		= ,			// ÉÌµêÀà
            //		NPCSORT_DICE		= ,			// ÷»×ÓNPC

            NpcsortUseLinkId = LinkMap | DieAction | Event,
        };

        #endregion

        protected BaseNpc(uint idNpc)
        {
            Identity = idNpc;
        }

        public virtual async Task<bool> InitializeAsync()
        {
            if (IsShopNpc())
            {
                ShopGoods = await GoodsRepository.GetAsync(Identity);
            }

            await EnterMapAsync();
            return true;
        }

        #region Type Identity
        
        public virtual ushort Type { get; }

        public virtual uint OwnerType { get; set; }

        public virtual NpcSort Sort { get; }

        #endregion

        #region Position

        public virtual async Task<bool> ChangePosAsync(uint idMap, ushort x, ushort y)
        {
            GameMap map = Kernel.MapManager.GetMap(idMap);
            if (map != null)
            {
                if (!map.IsStandEnable(x, y) && idMap != 5000)
                    return false;

                await LeaveMapAsync();
                m_idMap = idMap;
                m_posX = x;
                m_posY = y;
                await EnterMapAsync();
                return true;
            }
            return false;
        }

        #endregion
        
        #region Map

        public override async Task EnterMapAsync()
        {
            Map = Kernel.MapManager.GetMap(MapIdentity);
            if (Map != null)
            {
                await Map.AddAsync(this);
            }
        }

        public override async Task LeaveMapAsync()
        {
            if (Map != null)
            {
                await Map.RemoveAsync(Identity);
            }
        }

        #endregion

        #region Task and Data

        public int GetData(string szAttr)
        {
            switch (szAttr.ToLower())
            {
                case "data0": return Data0;
                case "data1": return Data1;
                case "data2": return Data2;
                case "data3": return Data3;
            }
            return 0;
        }

        public bool SetData(string szAttr, int value)
        {
            switch (szAttr.ToLower())
            {
                case "data0": Data0 = value; return true;
                case "data1": Data1 = value; return true;
                case "data2": Data2 = value; return true;
                case "data3": Data3 = value; return true;
            }

            return false;
        }

        public bool AddData(string szAttr, int value)
        {
            switch (szAttr.ToLower())
            {
                case "data0": Data0 += value; return true;
                case "data1": Data1 += value; return true;
                case "data2": Data2 += value; return true;
                case "data3": Data3 += value; return true;
            }

            return false;
        }

        public uint GetTask(string task)
        {
            switch (task.ToLower())
            {
                case "task0": return Task0;
                case "task1": return Task1;
                case "task2": return Task2;
                case "task3": return Task3;
                case "task4": return Task4;
                case "task5": return Task5;
                case "task6": return Task6;
                case "task7": return Task7;
                default: return 0;
            }
        }

        public virtual uint OwnerIdentity { get; set; }
        public virtual bool Vending { get; set; }
        public virtual uint Task0 { get; }
        public virtual uint Task1 { get; }
        public virtual uint Task2 { get; }
        public virtual uint Task3 { get; }
        public virtual uint Task4 { get; }
        public virtual uint Task5 { get; }
        public virtual uint Task6 { get; }
        public virtual uint Task7 { get; }

        public virtual int Data0 { get; set; }
        public virtual int Data1 { get; set; }
        public virtual int Data2 { get; set; }
        public virtual int Data3 { get; set; }

        public virtual string DataStr { get; set; }

        #endregion

        #region Functions

        #region Shop

        public List<DbGoods> ShopGoods = new List<DbGoods>();

        #endregion

        #region Task

        public async Task<bool> ActivateNpc(Character user)
        {
            bool result = false;

            uint task = await TestTasks(user);
            if (task != 0)
                result = await GameAction.ExecuteActionAsync(task, user, this, null, "");
            else if (user.IsPm())
            {
                await user.SendAsync($"Unhandled NPC[{Identity}:{Name}]->{Task0},{Task1},{Task2},{Task3},{Task4},{Task5},{Task6},{Task6},{Task7}", MsgTalk.TalkChannel.Talk, Color.Red);
            }

            return result;
        }

        private async Task<uint> TestTasks(Character user)
        {
            for (int i = 0; i < 8; i++)
            {
                DbTask task = Kernel.EventManager.GetTask(GetTask($"task{i}"));
                if (task != null && await user.TestTaskAsync(task))
                    return task.IdNext;
            }
            return 0;
        }

        #endregion

        #endregion

        #region Common Checks

        public bool IsLinkNpc()
        {
            return (Sort & NpcSort.LinkMap) != 0;
        }

        public bool IsShopNpc()
        {
            return Type == SHOPKEEPER_NPC;
        }

        public bool IsTaskNpc()
        {
            return Type == TASK_NPC;
        }

        public bool IsStorageNpc()
        {
            return Type == STORAGE_NPC;
        }

        public bool IsUserNpc()
        {
            return OwnerType == 1;
        }

        public bool IsSynNpc()
        {
            return OwnerType == 2;
        }

        public bool IsFamilyNpc()
        {
            return OwnerType == 4;
        }

        public bool IsSynFlag()
        {
            return (Type == SYNFLAG_NPC || Type == ROLE_CTFBASE_NPC) && IsSynNpc();
        }

        public bool IsSysTrans()
        {
            return Type == SYNTRANS_NPC;
        }

        public bool IsCtfFlag()
        {
            return IsSynFlag() && Type == ROLE_CTFBASE_NPC;
        }

        public bool IsAwardScore()
        {
            return IsSynFlag() || IsCtfFlag();
        }

        public bool IsSynMoneyEmpty()
        {
            if (!IsSynFlag())
                return false;

            Syndicate syn = Kernel.SyndicateManager.GetSyndicate((int) OwnerIdentity);
            return syn != null && syn.Money <= 0;
        }

        #endregion

        #region Management

        public virtual Task DelNpcAsync()
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Database

        public virtual Task<bool> SaveAsync()
        {
            return Task.FromResult(true);
        }

        public virtual Task<bool> DeleteAsync()
        {
            return Task.FromResult(true);
        }


        #endregion
    }
}