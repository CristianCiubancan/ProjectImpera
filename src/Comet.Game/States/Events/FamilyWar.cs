// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - FamilyWar.cs
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
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Families;
using Comet.Game.States.NPCs;
using Comet.Game.World.Maps;

namespace Comet.Game.States.Events
{
    public sealed class FamilyWar : GameEvent
    {
        private const uint REWARD_STC_U = 100020;

        private readonly Dictionary<uint, uint[]> m_prizePool = new Dictionary<uint, uint[]>
        {
            { 10026, new uint[]{ 722458, 722457, 722456, 722455, 722454 }},
            { 10027, new uint[]{ 722458, 722457, 722456, 722455, 722454 }},
            { 10028, new uint[]{ 722478, 722477, 722476, 722475, 722474 }},
            { 10029, new uint[]{ 722478, 722477, 722476, 722475, 722474 }},
            { 10030, new uint[]{ 722473, 722472, 722471, 722470, 722469 }},
            { 10031, new uint[]{ 722473, 722472, 722471, 722470, 722469 }},
            { 10032, new uint[]{ 722468, 722467, 722466, 722465, 722464 }},
            { 10033, new uint[]{ 722468, 722467, 722466, 722465, 722464 }},
            { 10034, new uint[]{ 722463, 722462, 722461, 722460, 722459 }},
            { 10035, new uint[]{ 722463, 722462, 722461, 722460, 722459 }}
        };

        private readonly Dictionary<uint, uint> m_goldFee = new Dictionary<uint, uint>
        {
            { 10026, 1000000 },
            { 10027, 1000000 },
            { 10028, 200000 },
            { 10029, 200000 },
            { 10030, 400000 },
            { 10031, 400000 },
            { 10032, 600000 },
            { 10033, 600000 },
            { 10034, 800000 },
            { 10035, 800000 }
        };

        private readonly List<double> m_expRewards = new List<double>
        { 
            0.01,
            0.015d,
            0.02,
            0.025d,
            0.03,
            0.035d,
            0.05
        };

        private int m_lastUpdate;

        public FamilyWar()
            : base("Clan War", 1000)
        {
        }

        #region Override

        public override bool IsInTime => uint.Parse(DateTime.Now.ToString("HHmmss")) >= 203000
                                         && uint.Parse(DateTime.Now.ToString("HHmmss")) < 210000;

        public override bool IsAllowedToJoin(Role sender)
        {
            return uint.Parse(DateTime.Now.ToString("HHmmss")) >= 203000
                   && uint.Parse(DateTime.Now.ToString("HHmmss")) < 203500;
        }

        public override async Task OnTimerAsync()
        {
            if (int.Parse(DateTime.Now.ToString("HHmm")) == 2100 && m_lastUpdate != int.Parse(DateTime.Now.ToString("yyyyMMdd")))
            {
                foreach (var family in Kernel.FamilyManager.QueryFamilies(x => x.ChallengeMap != 0))
                {
                    family.ChallengeMap = 0;
                    await family.SaveAsync();
                }

                m_lastUpdate = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
            }
        }

        #endregion

        private int GetRewardIdx(uint occupyDays)
        {
            return (int) Math.Max(0, Math.Min(4, occupyDays / 7));
        }

        private int GetExpRewardIdx(uint occupyDays)
        {
            occupyDays = Math.Max(1, occupyDays);
            return (int) ((occupyDays - 1) % m_expRewards.Count);
        }

        public uint GetNextReward(Character sender, uint idNpc = 0)
        {
            DynamicNpc npc;
            if (idNpc != 0)
            {
                npc = Kernel.RoleManager.FindRole<DynamicNpc>(idNpc);
                if (npc == null || !m_prizePool.ContainsKey(idNpc))
                    return 0;

                if (sender.FamilyIdentity == 0 || sender.Family.FamilyMap == 0 || sender.Family.FamilyMap != npc.Data1)
                    return m_prizePool[npc.Identity][0];

                return m_prizePool[idNpc][GetRewardIdx(sender.Family.OccupyDays)];
            }

            if (sender.Family == null)
                return 0;
            
            // I want the ID of my next reward. This means that I'll get the reward to my Family Map.
            npc = Kernel.RoleManager.FindRole<DynamicNpc>(x => x.Data1 == sender.Family.FamilyMap);
            if (npc == null || !m_prizePool.ContainsKey(npc.Identity))
                return 0;

            return m_prizePool[npc.Identity][GetRewardIdx(sender.Family.OccupyDays)];
        }

        public uint GetNextWeekReward(Character sender, uint idNpc)
        {
            if (idNpc == 0)
                return 0;

            var npc = Kernel.RoleManager.FindRole<DynamicNpc>(idNpc);
            if (npc == null)
                return 0;

            if (sender.Family == null || sender.Family.FamilyMap == 0 || sender.Family.FamilyMap != npc.Data1)
                return m_prizePool[npc.Identity][m_prizePool[npc.Identity].Length - 1];

            return m_prizePool[npc.Identity][Math.Min(GetRewardIdx(sender.Family.OccupyDays) + 1, m_prizePool[npc.Identity].Length - 1)];
        }

        public Family GetFamilyOwner(uint idNpc)
        {
            var npc = Kernel.RoleManager.FindRole<DynamicNpc>(idNpc);
            if (npc == null)
                return null;
            return Kernel.FamilyManager.GetOccupyOwner((uint) npc.Data1);
        }

        public DynamicNpc GetChallengeNpc(Family family)
        {
            if (family == null || family.ChallengeMap == 0)
                return null;
            return Kernel.RoleManager.FindRole<DynamicNpc>(x => x.Type == BaseNpc.ROLE_FAMILY_WAR_FLAG && x.Data1 == family.ChallengeMap);
        }

        public DynamicNpc GetDominatingNpc(Family family)
        {
            if (family == null || family.FamilyMap == 0)
                return null;
            return Kernel.RoleManager.FindRole<DynamicNpc>(x => x.Type == BaseNpc.ROLE_FAMILY_WAR_FLAG && x.Data1 == family.FamilyMap);
        }

        public uint GetGoldFee(uint idNpc)
        {
            if (!m_goldFee.ContainsKey(idNpc))
                return 0;
            return m_goldFee[idNpc];
        }

        public GameMap GetMap(uint idNpc)
        {
            return Kernel.MapManager.GetMap(idNpc);
        }

        public List<Family> GetChallengers(uint idNpc)
        {
            return Kernel.FamilyManager.QueryFamilies(x => x.ChallengeMap == idNpc);
        }

        public bool IsChallenged(uint idNpc)
        {
            if (idNpc == 0)
                return false;

            var challengers = GetChallengers(idNpc);
            return challengers != null && challengers.Count > 0;
        }

        private bool ValidateRewardTime(DateTime time)
        {
            DateTime now = DateTime.Now;
            if (now.Year != time.Year)
                return true;

            uint nowTime = uint.Parse(now.ToString("HHmmss"));
            uint lastTime = uint.Parse(now.ToString("HHmmss"));
            if (lastTime >= 210000 && lastTime <= 235959)
            {
                if (nowTime >= 210000 && nowTime <= 235959 && now.DayOfYear != time.DayOfYear)
                    return true;

                if (nowTime < 203000)
                    return true;
            }
            else if (lastTime <= 202959)
            {
                if (nowTime <= 202959 && now.DayOfYear != time.DayOfYear)
                    return true;

                if (nowTime >= 210000)
                    return true;
            }
            // may be error - must fix manually
            return false;
        }

        public bool HasExpToClaim(Character user)
        {
            if (IsInTime)
                return false;

            if (user?.Family == null)
                return false;

            var npc = GetDominatingNpc(user.Family);
            if (npc == null)
                return false;

            var last = user.Statistic.GetStc(REWARD_STC_U)?.Timestamp;
            if (last.HasValue)
                return ValidateRewardTime(last.Value);
            return true;
        }

        public async Task SetExpRewardAwardedAsync(Character user)
        {
            var currStc = user.Statistic.GetStc(REWARD_STC_U);
            if (currStc == null)
            {
                if (!await user.Statistic.AddOrUpdateAsync(REWARD_STC_U, 0, 0, true))
                    return;

                currStc = user.Statistic.GetStc(REWARD_STC_U);
                if (currStc == null)
                    return;
            }

            await user.Statistic.AddOrUpdateAsync(REWARD_STC_U, 0, currStc.Data + 1, true);
        }

        public double GetNextExpReward(Character user)
        {
            if (!HasExpToClaim(user))
                return 0;
            return m_expRewards[GetExpRewardIdx(user.Family.OccupyDays)];
        }

        public bool HasRewardToClaim(Character user)
        {
            if (IsInTime)
                return false;

            if (user?.Family == null)
                return false;

            var npc = GetDominatingNpc(user.Family);
            if (npc == null)
                return false;

            if (DateTime.TryParseExact(npc.DataStr, "O", Thread.CurrentThread.CurrentCulture, DateTimeStyles.AssumeLocal, out var date) && !ValidateRewardTime(date))
                return false;
            return true;
        }

        public async Task SetRewardAwardedAsync(Character user)
        {
            var npc = GetDominatingNpc(user.Family);
            if (npc == null)
                return;

            var currStc = user.Statistic.GetStc(REWARD_STC_U, 1);
            if (currStc == null)
            {
                if (!await user.Statistic.AddOrUpdateAsync(REWARD_STC_U, 1, 0, true))
                    return;

                currStc = user.Statistic.GetStc(REWARD_STC_U, 1);
                if (currStc == null)
                    return;
            }

            npc.DataStr = DateTime.Now.ToString("O");
            await npc.SaveAsync();
            await user.Statistic.AddOrUpdateAsync(REWARD_STC_U, 1, currStc.Data + 1, true);
        }
    }
}