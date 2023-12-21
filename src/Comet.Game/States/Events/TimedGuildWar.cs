// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - TimedGuildWar.cs
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
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.Database;
using Comet.Game.Packets;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.NPCs;
using Comet.Game.States.Syndicates;
using Comet.Game.World.Maps;
using Comet.Shared;

namespace Comet.Game.States.Events
{
    public sealed class TimedGuildWar : GameEvent
    {
        private ConcurrentDictionary<uint, SyndicateData> m_synData = new ConcurrentDictionary<uint, SyndicateData>();

        private TimeOut m_updateScreens = new TimeOut(10);
        private TimeOut m_updatePoints = new TimeOut(10);

        private const int MAP_ID = 2058;
        private const int MAX_DEATHS_PER_PLAYER = 20;
        private const int MIN_LEVEL = 70;
        private const int MIN_TIME_IN_SYNDICATE = 7; // in days

        private const int STARTUP_TIME = 6210000;
        private const int END_TIME = 6220000;

        private const int MAX_MONEY_REWARD = 120000000;
        private const int MAX_EMONEY_REWARD = 3000;

        private const int MAX_MONEY_REWARD_PER_USER = MAX_MONEY_REWARD/15;
        private const int MAX_EMONEY_REWARD_PER_USER = MAX_EMONEY_REWARD/15;

        private readonly (ushort x, ushort y)[] m_revivePoints =
        {
            (40, 147),
            (55, 147),
            (55, 128),
            (40, 128),
            (204, 107),
            (220, 107),
            (220, 127),
            (204, 127)
        };

        private readonly uint[] m_poleIdentities = new uint[]
        {
            100000,
            100001,
            100002,
        };

        private EventStage m_stage = EventStage.Idle;

        public TimedGuildWar()
            : base("GuildWar", 800)
        {
        }

        public override EventType Identity { get; } = EventType.TimedGuildWar;

        #region Override Base

        public override bool IsAllowedToJoin(Role sender)
        {
            if (!(sender is Character user))
                return false;

            if (sender.Level < MIN_LEVEL)
                return false;

            if (user.SyndicateIdentity == 0)
                return false;

            if ((DateTime.Now - user.Syndicate.CreationDate).TotalDays > MIN_TIME_IN_SYNDICATE)
            {
                if ((DateTime.Now - user.SyndicateMember.JoinDate).TotalDays < MIN_TIME_IN_SYNDICATE)
                    return false;
            }

            UserData data = FindUser(user.Identity);
            if (data?.Deaths >= MAX_DEATHS_PER_PLAYER)
                return false;
            return true;
        }

        public override async Task<bool> CreateAsync()
        {
            Map = Kernel.MapManager.GetMap(MAP_ID);
            if (Map == null)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"Could not start TimedGuildWar, invalid mapid {MAP_ID}");
                return false;
            }
            return true;
        }

        public override Task<bool> OnReviveAsync(Character sender, bool selfRevive)
        {
            if (sender == null || !selfRevive)
                return Task.FromResult(false);

            UserData data = FindUser(sender.Identity) ?? CreateUserData(sender);
            data.Deaths += 1;

            return Task.FromResult(true);
        }

        public override async Task<(uint id, ushort x, ushort y)> GetRevivePositionAsync(Character sender)
        {
            UserData data = FindUser(sender.Identity) ?? CreateUserData(sender);
            if (data.Deaths < MAX_DEATHS_PER_PLAYER)
            {
                var pos = m_revivePoints[await Kernel.NextAsync(m_revivePoints.Length)%m_revivePoints.Length];
                return (MAP_ID, pos.x, pos.y);
            }
            return await base.GetRevivePositionAsync(sender);
        }

        public override async Task OnTimerAsync()
        {
            int dow = (int) DateTime.Now.DayOfWeek;
            if (dow == 0)
                dow = 7;
            int now = dow * 1000000 + int.Parse(DateTime.Now.ToString("HHmmss"));
            if (now < STARTUP_TIME || now > END_TIME)
            {
                if (m_stage == EventStage.Running)
                {
                    await DeliverRewardsAsync();
                    m_stage = EventStage.Idle;
                }
                return;
            }

            if (m_stage == EventStage.Idle) // cleanup and startup
            {
                m_synData.Clear();
                m_updateScreens.Update();
                m_updatePoints.Update();
                m_stage = EventStage.Running;
                return;
            }

            // do event stuff
            if (m_updatePoints.ToNextTime())
            {
                foreach (var idNpc in m_poleIdentities)
                {
                    DynamicNpc npc = Map.QueryRole<DynamicNpc>(idNpc);
                    if (npc == null || npc.OwnerIdentity == 0)
                        continue;

                    AddPoints(1, npc.OwnerIdentity);
                }
            }

            if (m_updateScreens.ToNextTime())
            {
                await Map.BroadcastMsgAsync(new MsgTalk(0, MsgTalk.TalkChannel.GuildWarRight1, Color.Yellow, Language.StrWarRankingStart));
                int i = 0;
                foreach (var data in m_synData.Values
                    .OrderByDescending(x => x.Points)
                    .ThenBy(x => x.Players.Count))
                {
                    if (i++ >= 4)
                        break;

                    await Map.BroadcastMsgAsync(new MsgTalk(0, MsgTalk.TalkChannel.GuildWarRight2, Color.Yellow,
                        string.Format(Language.StrWarRankingNo, i, data.Name, data.Points)));
                }

                foreach (var synData in m_synData.Values)
                {
                    foreach (UserData userData in synData.Players.Values)
                    {
                        Character user = Kernel.RoleManager.GetUser(userData.Identity);
                        if (user != null)
                            await user.SendAsync(new MsgTalk(0, MsgTalk.TalkChannel.GuildWarRight2, Color.Yellow,
                                string.Format(Language.StrWarYourScore, userData.Points, MAX_DEATHS_PER_PLAYER - userData.Deaths)));
                    }
                }
            }
        }

        #endregion

        public void UnsubscribeSyndicate(uint idSyn)
        {
            m_synData.TryRemove(idSyn, out _);
        }

        public void LeaveSyndicate(uint sender, uint idSyn)
        {
            SyndicateData syndicateData = FindSyndicate(idSyn);
            if (syndicateData == null)
                return;

            if (syndicateData.Players.ContainsKey(sender))
                syndicateData.Players.Remove(sender);
        }

        public void AddPoints(int amount, uint synId)
        {
            Syndicate syn = Kernel.SyndicateManager.GetSyndicate((int) synId);
            if (syn == null || syn.Deleted)
                return;

            if (!m_synData.TryGetValue(synId, out var synData))
            {
                synData = new SyndicateData
                {
                    Identity = syn.Identity,
                    Name = syn.Name
                };
                m_synData.TryAdd(syn.Identity, synData);
            }

            synData.Points = Math.Max(0, synData.Points + amount);

            if (amount > 0)
            {
                synData.TotalPoints += amount;

                foreach (var user in Kernel.RoleManager.QueryUserSetByMap(Map.Identity)
                    .Where(x => x.SyndicateIdentity == synId))
                {
                    if (!synData.Players.TryGetValue(user.Identity, out var userData))
                    {
                        userData = new UserData
                        {
                            Identity = user.Identity,
                            Name = user.Name,
                            Kills = 0,
                            Deaths = 0,
                            Points = 0,
                            IpAddress = user.Client.IPAddress
                        };
                        synData.Players.Add(userData.Identity, userData);
                    }

                    userData.Points += amount;
                }
            }
        }

        private async Task DeliverRewardsAsync()
        {
            SyndicateData winner = m_synData.Values.OrderByDescending(x => x.Points).ThenBy(x => x.Players.Count).FirstOrDefault();
            if (winner == null)
                return;

            List<string> ipAddresses = new List<string>();
            int totalMoney = MAX_MONEY_REWARD;
            int totalEmoney = MAX_EMONEY_REWARD;
            foreach (var userData in winner.Players.Values)
            {
                Character user = Kernel.RoleManager.GetUser(userData.Identity);
                if (ipAddresses.Contains(userData.IpAddress))
                {
                    if (user != null)
                        await user.SendAsync(Language.StrSynWarIpRewarded);
                    continue;
                }

                ipAddresses.Add(userData.IpAddress);

                double percentual = userData.Points / (double) winner.TotalPoints;
                int money = (int) Math.Min(MAX_MONEY_REWARD_PER_USER, totalMoney * percentual);
                int emoney = (int) Math.Min(MAX_EMONEY_REWARD_PER_USER, totalEmoney * percentual);

                totalMoney -= money;
                totalEmoney -= emoney;

                if (user != null)
                {
                    await user.AwardMoneyAsync(money);
                    await user.AwardConquerPointsAsync(emoney);
                    await user.SendAsync(string.Format(Language.StrSynWarRewardNotify, money, emoney));
                }
                else
                {
                    await BaseRepository.ScalarAsync($"UPDATE `cq_user` SET `money`=`money`+{money}, `emoney`=`emoney`+{emoney} WHERE `id`={userData.Identity} LIMIT 1");
                }

                await Log.GmLogAsync($"TimedGuildWarRewards",
                    $"{userData.Identity},[{userData.Name}],{userData.IpAddress},{userData.MacAddress},{userData.Kills},{userData.Deaths},{userData.Points},{winner.Points},{winner.TotalPoints},{money},{emoney}");
            }
        }

        private SyndicateData CreateSyndicateData(Syndicate syn)
        {
            SyndicateData data = FindSyndicate(syn.Identity);
            if (data != null)
                return data;

            data = new SyndicateData
            {
                Identity = syn.Identity,
                Name = syn.Name
            };

            m_synData.TryAdd(syn.Identity, data);
            return data;
        }

        private SyndicateData FindSyndicate(uint idSyn)
        {
            return m_synData.TryGetValue(idSyn, out var value) ? value : null;
        }

        private UserData CreateUserData(Character sender)
        {
            SyndicateData synData = FindSyndicate(sender.SyndicateIdentity) ?? CreateSyndicateData(sender.Syndicate);
            UserData data;
            if ((data = FindUser(sender.Identity)) != null)
                return data;

            data = new UserData
            {
                Identity = sender.Identity,
                Name = sender.Name,
                Kills = 0,
                Deaths = 0,
                Points = 0,
                IpAddress = sender.Client.IPAddress
            };
            synData.Players.TryAdd(data.Identity, data);
            return data;
        }

        private UserData FindUser(uint idUser)
        {
            foreach (var syn in m_synData.Values)
            {
                if (syn.Players.TryGetValue(idUser, out var data))
                    return data;
            }
            return null;
        }

        private class UserData
        {
            public uint Identity { get; set; }
            public string Name { get; set; }
            public long Points { get; set; }
            public int Kills { get; set; }
            public int Deaths { get; set; }
            
            public string IpAddress { get; set; }
            public string MacAddress { get; set; }
        }

        private class SyndicateData
        {
            public uint Identity { get; set; }
            public string Name { get; set; }
            public long Points { get; set; }
            public long TotalPoints { get; set; }

            public Dictionary<uint, UserData> Players = new Dictionary<uint, UserData>();
        }
    }
}