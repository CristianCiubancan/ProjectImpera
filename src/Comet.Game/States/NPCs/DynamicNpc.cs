// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Dynamic Npc.cs
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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Core.Mathematics;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Packets;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Syndicates;

#endregion

namespace Comet.Game.States.NPCs
{
    public sealed class DynamicNpc : BaseNpc
    {
        private readonly DbDynanpc m_dbNpc;
        private readonly ConcurrentDictionary<uint, Score> m_dicScores = new ConcurrentDictionary<uint, Score>();
        private readonly TimeOut m_Death = new TimeOut(5);

        public DynamicNpc(DbDynanpc npc)
            : base(npc.Id)
        {
            m_dbNpc = npc;

            m_idMap = npc.Mapid;
            m_posX = npc.Cellx;
            m_posY = npc.Celly;

            Name = npc.Name;

            if (IsSynFlag() && OwnerIdentity > 0)
            {
                Syndicate syn = Kernel.SyndicateManager.GetSyndicate((int) OwnerIdentity);
                if (syn != null)
                    Name = syn.Name;
            }
        }

        #region Socket

        public override async Task SendSpawnToAsync(Character player)
        {
            await player.SendAsync(new MsgNpcInfoEx
            {
                Identity = Identity,
                Lookface = m_dbNpc.Lookface,
                Sort = m_dbNpc.Sort,
                PosX = MapX,
                PosY = MapY,
                Name = IsSynFlag() ? Name : "",
                NpcType = m_dbNpc.Type,
                Life = Life,
                MaxLife = MaxLife
            });
        }

        #endregion

        #region Type

        public override uint Mesh
        {
            get => m_dbNpc.Lookface;
            set => m_dbNpc.Lookface = (ushort) value;
        }

        public override ushort Type => m_dbNpc.Type;

        public void SetType(ushort type) => m_dbNpc.Type = type;

        public override NpcSort Sort => (NpcSort) m_dbNpc.Sort;

        public void SetSort(ushort sort) => m_dbNpc.Sort = sort;

        public override uint OwnerType
        {
            get => m_dbNpc.OwnerType;
            set => m_dbNpc.OwnerType = value;
        }

        public override uint OwnerIdentity
        {
            get => m_dbNpc.Ownerid;
            set => m_dbNpc.Ownerid = value;
        }

        #endregion

        #region Life

        public override uint Life
        {
            get => m_dbNpc.Life;
            set => m_dbNpc.Life = value;
        }

        public override uint MaxLife => m_dbNpc.Maxlife;

        #endregion

        #region Position

        public override async Task<bool> ChangePosAsync(uint idMap, ushort x, ushort y)
        {
            if (await base.ChangePosAsync(idMap, x, y))
            {
                m_dbNpc.Mapid = idMap;
                m_dbNpc.Celly = y;
                m_dbNpc.Cellx = x;
                await SaveAsync();
                return true;
            }
            return false;
        }

        #endregion

        #region Attributes

        public override async Task<bool> AddAttributesAsync(ClientUpdateType type, long value)
        {
            return await base.AddAttributesAsync(type, value) && await SaveAsync();
        }

        public override async Task<bool> SetAttributesAsync(ClientUpdateType type, ulong value)
        {
            switch (type)
            {
                case ClientUpdateType.Mesh:
                    Mesh = (uint) value;
                    await BroadcastRoomMsgAsync(new MsgNpcInfoEx(this), false);
                    return await SaveAsync();

                case ClientUpdateType.Hitpoints:
                    m_dbNpc.Life = Math.Min((uint)value, MaxLife);
                    await BroadcastRoomMsgAsync(new MsgUserAttrib(Identity, ClientUpdateType.Hitpoints, Life), false);
                    return await SaveAsync();

                case ClientUpdateType.MaxHitpoints:
                    m_dbNpc.Maxlife = (uint) value;
                    await BroadcastRoomMsgAsync(new MsgNpcInfoEx(this), false);
                    return await SaveAsync();
            }
            return await base.SetAttributesAsync(type, value) && await SaveAsync();
        }

        public bool IsGoal()
        {
            return Type == WEAPONGOAL_NPC || Type == MAGICGOAL_NPC;
        }

        public bool IsCityGate()
        {
            return Type == ROLE_CITY_GATE_NPC;
        }

        #endregion

        #region Task and Data

        public uint LinkId
        {
            get => m_dbNpc.Linkid;
            set => m_dbNpc.Linkid = value;
        }

        public void SetTask(int id, uint task)
        {
            switch (id)
            {
                case 0: m_dbNpc.Task0 = task; break;
                case 1: m_dbNpc.Task1 = task; break;
                case 2: m_dbNpc.Task2 = task; break;
                case 3: m_dbNpc.Task3 = task; break;
                case 4: m_dbNpc.Task4 = task; break;
                case 5: m_dbNpc.Task5 = task; break;
                case 6: m_dbNpc.Task6 = task; break;
                case 7: m_dbNpc.Task7 = task; break;
            }
        }

        public override uint Task0 => m_dbNpc.Task0;
        public override uint Task1 => m_dbNpc.Task1;
        public override uint Task2 => m_dbNpc.Task2;
        public override uint Task3 => m_dbNpc.Task3;
        public override uint Task4 => m_dbNpc.Task4;
        public override uint Task5 => m_dbNpc.Task5;
        public override uint Task6 => m_dbNpc.Task6;
        public override uint Task7 => m_dbNpc.Task7;

        public override int Data0
        {
            get => m_dbNpc.Data0;
            set => m_dbNpc.Data0 = value;
        }

        public override int Data1
        {
            get => m_dbNpc.Data1;
            set => m_dbNpc.Data1 = value;
        }

        public override int Data2
        {
            get => m_dbNpc.Data2;
            set => m_dbNpc.Data2 = value;
        }

        public override int Data3
        {
            get => m_dbNpc.Data3;
            set => m_dbNpc.Data3 = value;
        }

        public override string DataStr
        {
            get => m_dbNpc.Datastr;
            set => m_dbNpc.Datastr = value;
        }

        #endregion

        #region Battle

        public bool IsActive()
        {
            return !m_Death.IsActive();
        }

        public override bool IsAttackable(Role attacker)
        {
            if (!IsSynFlag() && !IsCtfFlag() && !IsGoal() && !IsCityGate())
                return false;

            Character attackerUser = attacker as Character;
            if (Data1 != 0 && Data2 != 0)
            {
                string strNow = "";
                DateTime now = DateTime.Now;
                strNow += ((int) now.DayOfWeek == 0 ? 7 : (int) now.DayOfWeek).ToString(CultureInfo.InvariantCulture);
                strNow += now.Hour.ToString("00");
                strNow += now.Minute.ToString("00");
                strNow += now.Second.ToString("00");

                int now0 = int.Parse(strNow);
                if ((now0 < Data1 || now0 >= Data2) && IsSynFlag())
                    return false;
            }

            if (OwnerType == 2 && attackerUser != null)
            {
                if (attackerUser.SyndicateIdentity != 0 && attackerUser.SyndicateIdentity == OwnerIdentity)
                    return false;
            }

            if (MaxLife <= 0)
                return false;

            return IsActive();
        }

        public override async Task<bool> BeAttackAsync(BattleSystem.MagicType magic, Role attacker, int nPower,
            bool bReflectEnable)
        {
            await AddAttributesAsync(ClientUpdateType.Hitpoints, nPower * -1);

            Character user = attacker as Character;
            if (IsSynFlag() && user != null)
            {
                Syndicate owner = Kernel.SyndicateManager.GetSyndicate((int) OwnerIdentity);
                int money = Calculations.MulDiv(nPower, SYNWAR_MONEY_PERCENT, 100);
                int proffer = Calculations.MulDiv(nPower, SYNWAR_PROFFER_PERCENT, 100);
                if (money > 0
                    && owner != null
                    && user.SyndicateIdentity != owner.Identity
                    && owner.Money > 0)
                {
                    owner.Money -= money;
                    await owner.SaveAsync();
                    await user.AwardMoneyAsync(money);

                    if (user.SyndicateIdentity > 0)
                    {
                        user.Syndicate.Money += proffer;
                        await user.Syndicate.SaveAsync();
                    }
                }
                else if (money > 0
                         && owner != null
                         && user.SyndicateIdentity != owner.Identity
                         && owner.Money <= 0)
                {
                    owner.Money -= money;
                    await owner.SaveAsync();
                }
            }

            if (!IsAlive)
                await BeKillAsync(attacker);

            return true;
        }

        public override async Task BeKillAsync(Role attacker)
        {
            if (m_dbNpc.Linkid != 0)
                await GameAction.ExecuteActionAsync(m_dbNpc.Linkid, attacker as Character, this, null, "");
        }

        public override async Task DelNpcAsync()
        {
            await SetAttributesAsync(ClientUpdateType.Hitpoints, 0);
            m_Death.Update();

            if (IsSynFlag() || IsCtfFlag())
            {
                await Map.SetStatusAsync(1, false);
            }
            else if (!IsGoal())
            {
                await DeleteAsync();
            }

            await LeaveMapAsync();
        }

        public async Task<bool> SetOwnerAsync(uint idOwner, bool withLinkMap = false)
        {
            if (idOwner == 0)
            {
                OwnerIdentity = 0;
                Name = "";

                await BroadcastRoomMsgAsync(new MsgNpcInfoEx(this), false);
                await SaveAsync();
                return true;
            }

            OwnerIdentity = idOwner;
            if (IsSynFlag())
            {
                Syndicate syn = Kernel.SyndicateManager.GetSyndicate((int) OwnerIdentity);
                if (syn == null)
                {
                    OwnerIdentity = 0;
                    Name = "";
                }
                else
                {
                    Name = syn.Name;
                }
            }

            // TODO
            /*if (withLinkMap)
            {
                foreach (var player in Kernel.RoleManager.QueryUserSetByMap(MapIdentity))
                {

                }
            }*/

            await SaveAsync();
            await BroadcastRoomMsgAsync(new MsgNpcInfoEx(this) { Lookface = m_dbNpc.Lookface }, false);
            return true;
        }

        public async Task CheckFightTimeAsync()
        {
            if (!IsSynFlag())
                return;

            if (Data1 == 0 || Data2 == 0)
                return;

            string strNow = "";
            DateTime now = DateTime.Now;
            strNow += ((int) now.DayOfWeek == 0 ? 7 : (int) now.DayOfWeek).ToString(CultureInfo.InvariantCulture);
            strNow += now.Hour.ToString("00");
            strNow += now.Minute.ToString("00");
            strNow += now.Second.ToString("00");

            int now0 = int.Parse(strNow);
            if (now0 < Data1 || now0 >= Data2)
            {
                if (Map.IsWarTime())
                    await OnFightEndAsync();
                return;
            }

            if (!Map.IsWarTime())
            {
                await Map.SetStatusAsync(1, true);
                await Map.BroadcastMsgAsync(Language.StrWarStart, MsgTalk.TalkChannel.System);
            }
        }

        public async Task OnFightEndAsync()
        {
            await Map.SetStatusAsync(1, false);
            await Map.BroadcastMsgAsync(Language.StrWarEnd, MsgTalk.TalkChannel.System);
            Map.ResetBattle();
        }

        public async Task BroadcastRankingAsync()
        {
            if (!IsSynFlag() || !IsAttackable(null) || m_dicScores.Count == 0)
                return;

            await Map.BroadcastMsgAsync(Language.StrWarRankingStart, MsgTalk.TalkChannel.GuildWarRight1);
            int i = 0;
            foreach (var score in m_dicScores.Values.OrderByDescending(x => x.Points))
            {
                if (i++ >= 5)
                    break;

                await Map.BroadcastMsgAsync(string.Format(Language.StrWarRankingNo, i, score.Name, score.Points),
                    MsgTalk.TalkChannel.GuildWarRight2);
            }
        }

        public void AddSynWarScore(Syndicate syn, long score)
        {
            if (syn == null)
                return;

            if (!m_dicScores.ContainsKey(syn.Identity))
                m_dicScores.TryAdd(syn.Identity, new Score(syn.Identity, syn.Name));

            m_dicScores[syn.Identity].Points += score;
        }

        public Score GetTopScore()
        {
            return m_dicScores.Values.OrderByDescending(x => x.Points).ThenBy(x => x.Identity).FirstOrDefault();
        }

        public void ClearScores()
        {
            m_dicScores.Clear();
        }

        #endregion

        #region Database

        public override async Task<bool> SaveAsync()
        {
            return await BaseRepository.SaveAsync(m_dbNpc);
        }

        public override async Task<bool> DeleteAsync()
        {
            return await BaseRepository.DeleteAsync(m_dbNpc);
        }

        #endregion

        public class Score
        {
            public Score(uint id, string name)
            {
                Identity = id;
                Name = name;
            }

            public uint Identity { get; }
            public string Name { get; }
            public long Points { get; set; }
        }
    }
}