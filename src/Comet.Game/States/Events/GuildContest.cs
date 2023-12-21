// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - GuildContest.cs
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
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Magics;
using Comet.Game.States.NPCs;
using Comet.Game.States.Syndicates;
using Comet.Game.World.Maps;
using Comet.Shared;

#endregion

namespace Comet.Game.States.Events
{
    public sealed class GuildContest : GameEvent
    {
        private const uint NPC_ID_U = 16012;
        private const int MAP_ID = 2060;
        private const int MAX_DEATHS_PER_PLAYER = 20;
        private const int MIN_LEVEL = 40;
        private const int MIN_TIME_IN_SYNDICATE = 0; // in days

        private readonly int[] REWARD_MONEY =
        {
            60000000,
            50000000,
            40000000,
            35000000,
            30000000,
            25000000,
            20000000,
            15000000
        };
        private readonly int[] REWARD_EMONEY =
        {
            1000,
            800,
            600,
            500,
            400,
            300,
            250,
            200
        };

        private ushort[] mReviveX = { 125, 118, 110, 124, 139, 142, 130 };
        private ushort[] mReviveY = { 125, 112, 117, 137, 143, 131, 122 };

        private TimeOut mUpdateScreen = new TimeOut(10);

        private ConcurrentDictionary<uint, SyndicateData> mSyndicateData = new ConcurrentDictionary<uint, SyndicateData>();

        private Npc mNpc;

        public GuildContest()
            : base("Syndicate Contest", 1000)
        {
        }

        private ContestPlayerData GetPlayerData(uint idPlayer)
        {
            Character player = Kernel.RoleManager.GetUser(idPlayer);
            if (player == null)
                return null;

            if (player.SyndicateIdentity == 0)
                return null;

            if (!mSyndicateData.TryGetValue(player.SyndicateIdentity, out var synData))
            {
                mSyndicateData.TryAdd(player.SyndicateIdentity, synData = new SyndicateData
                {
                    Identity = player.SyndicateIdentity,
                    Name = player.SyndicateName,
                    PlayerData = new ConcurrentDictionary<uint, ContestPlayerData>()
                });
            }

            if (!synData.PlayerData.TryGetValue(player.Identity, out var userData))
            {
                synData.PlayerData.TryAdd(player.Identity, userData = new ContestPlayerData
                {
                    Identity = player.Identity,
                    Name = player.Name,

                    SyndicateData = synData
                });
            }

            return userData;
        }

        public void RemoveUserFromEvent(uint idUser, uint idSyndicate)
        {
            if (mSyndicateData.TryGetValue(idSyndicate, out var synData))
                synData.PlayerData.TryRemove(idUser, out _);
        }

        public void RemoveFromEvent(uint idSyndicate)
        {
            mSyndicateData.TryRemove(idSyndicate, out _);
        }

        #region Override

        public override EventType Identity { get; } = EventType.GuildContest;

        public override bool IsAllowedToJoin(Role sender)
        {
            if (!(sender is Character user))
                return false;

            if (mNpc.Data0 != 2)
                return false;

            if (sender.Level < MIN_LEVEL)
                return false;

            if (user.SyndicateIdentity == 0)
                return false;

            if ((DateTime.Now - user.SyndicateMember.JoinDate).TotalDays < MIN_TIME_IN_SYNDICATE)
                return false;

            ContestPlayerData data = GetPlayerData(sender.Identity);
            if (data != null && data.Deaths > MAX_DEATHS_PER_PLAYER)
                return false;

            return true;
        }

        public override async Task<bool> CreateAsync()
        {
            Map = Kernel.MapManager.GetMap(MAP_ID);
            if (Map == null)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"Could not start GuildContest, invalid mapid {MAP_ID}");
                return false;
            }

            mNpc = Kernel.RoleManager.FindRole<Npc>(NPC_ID_U);
            if (mNpc == null)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"Could not start GuildContest, invalid npcid {NPC_ID_U}");
                return false;
            }

            mNpc.Data0 = 0;
            return true;
        }

        public override Task OnKillAsync(Role attacker, Role target, Magic magic = null)
        {
            if (!(attacker is Character))
                return Task.CompletedTask;

            ContestPlayerData attackerData = GetPlayerData(attacker.Identity);
            ContestPlayerData targetData = null;
            if (target is Character tgtUser)
                targetData = GetPlayerData(tgtUser.Identity);
            else tgtUser = null;
            
            if (attackerData != null && mNpc.Data0 == 2)
            {
                bool doPoint = false;
                int delta = attacker.Level - target.Level;
                if (targetData != null)
                {
                    if (attacker.Level < 100)
                    {
                        doPoint = true;
                    }
                    else if (attacker.Level >= 100 && attacker.Level < 120)
                    {
                        if (delta <= 10)
                            doPoint = true;
                    }
                    else if (attacker.Level >= 120 && attacker.Level < 130)
                    {
                        if (delta <= 10)
                            doPoint = true;
                    }
                    else
                    {
                        if (delta <= 10)
                            doPoint = true;
                    }

                    if (doPoint)
                    {
                        attackerData.Kills += 1;
                        attackerData.Points += 3;

                        attackerData.SyndicateData.Points += 3;
                    }
                }
                else
                {
                    if (attacker.Level < 100)
                    {
                        doPoint = true;
                    }
                    else if (attacker.Level >= 100 && attacker.Level < 120)
                    {
                        if (target.Level >= 100)
                            doPoint = true;
                    }
                    else if (attacker.Level >= 120 && attacker.Level < 130)
                    {
                        if (target.Level >= 120)
                            doPoint = true;
                    }
                    else
                    {
                        if (target.Level >= 128)
                            doPoint = true;
                    }

                    if (doPoint)
                    {
                        attackerData.Points += 10;
                        attackerData.MonsterKills += 1;

                        attackerData.SyndicateData.Points += 10;
                    }
                }
            }
            return Task.CompletedTask;
        }

        public override async Task<bool> OnReviveAsync(Character sender, bool selfRevive)
        {
            if (selfRevive)
            {
                var data = GetPlayerData(sender.Identity);
                if (data != null)
                {
                    data.Deaths += 1;

                    if (data.Deaths > MAX_DEATHS_PER_PLAYER)
                    {
                        await sender.FlyMapAsync(sender.RecordMapIdentity, sender.RecordMapX, sender.RecordMapY);
                        await Kernel.RoleManager.BroadcastMsgAsync(string.Format(Language.StrSyndicateContestElimination, 
                            sender.Name, sender.SyndicateName, data.Deaths));
                    }
                }
            }
            return true;
        }

        public override async Task OnTimerAsync()
        {
            if (mNpc.Data0 == 1) // starting
            {
                mSyndicateData.Clear();

                mUpdateScreen.Update();
            }
            else if (mNpc.Data0 == 2) // running
            {
                foreach (var data in Kernel.RoleManager.QueryRoleByMap<Character>(MAP_ID))
                {
                    if (data.SyndicateIdentity == 0)
                        await data.FlyMapAsync(data.RecordMapIdentity, data.RecordMapX, data.RecordMapY);
                }

                if (mUpdateScreen.ToNextTime())
                {
                    await Map.BroadcastMsgAsync(Language.StrSyndicateContestRankTitle, MsgTalk.TalkChannel.GuildWarRight1);
                    int idx = 0;
                    foreach (var data in mSyndicateData.Values.Where(x => x.Points > 0).OrderByDescending(x => x.Points))
                    {
                        Syndicate syndicate = Kernel.SyndicateManager.GetSyndicate((int)data.Identity);
                        if (syndicate == null)
                            continue;

                        if (idx++ > 7)
                            break;
                        await Map.BroadcastMsgAsync(string.Format(Language.StrSyndicateContestRankNo, idx, data.Name, data.Points),
                            MsgTalk.TalkChannel.GuildWarRight2);
                    }
                }
            }
            else if (mNpc.Data0 == 3) // ending
            {
                int rank = 0;
                foreach (var data in mSyndicateData.Values.Where(x => x.Points > 0).OrderByDescending(x => x.Points))
                {
                    int moneyReward = REWARD_MONEY[rank];
                    int emoneyReward = REWARD_EMONEY[rank];

                    await Log.GmLogAsync("syndicate_contest_rewards", $"{data.Identity:00000},{data.Name:-16},{data.Points},{moneyReward},{emoneyReward}");

                    Syndicate syndicate = Kernel.SyndicateManager.GetSyndicate((int)data.Identity);
                    if (syndicate == null)
                        continue;

                    int userRank = 1;
                    foreach (var memberData in data.PlayerData.Values.Where(x => x.Points > 0).OrderByDescending(x => x.Points))
                    {
                        if (syndicate.QueryMember(memberData.Identity) == null)
                            continue;

                        double delta = memberData.Points / data.Points;
                        int userMoneyReward = (int)(moneyReward * delta);
                        int userEmoneyReward = (int)(emoneyReward * delta);

                        Character user = Kernel.RoleManager.GetUser(memberData.Identity);
                        if (user != null)
                        {
                            await user.AwardMoneyAsync(userMoneyReward);
                            await user.AwardConquerPointsAsync(userEmoneyReward);

                            await user.SendAsync(string.Format(Language.StrSyndicateContestRewardNotify,
                                userRank, userMoneyReward, userEmoneyReward, rank + 1), MsgTalk.TalkChannel.Talk);

                            await user.FlyMapAsync(user.RecordMapIdentity, user.RecordMapX, user.RecordMapY);
                        }
                        else
                        {
                            DbCharacter dbUser = await CharactersRepository.FindByIdentityAsync(memberData.Identity);
                            if (dbUser != null)
                            {
                                dbUser.Silver = (uint)Math.Max(0, Math.Min(int.MaxValue, dbUser.Silver + userMoneyReward));
                                dbUser.ConquerPoints = (uint)Math.Max(0, Math.Min(int.MaxValue, dbUser.ConquerPoints + userEmoneyReward));
                                await BaseRepository.SaveAsync(dbUser);
                            }
                        }
                        userRank++;
                        await Log.GmLogAsync("syndicate_contest_rewards", $"{data.Identity:00000},{data.Name:-16},{memberData.Identity},{memberData.Name},{memberData.Points},{delta:0.0000},{userMoneyReward},{userEmoneyReward}");
                    }

                    if (++rank > 7)
                        break;
                }
                mNpc.Data0 = 0;
            }
        }

        public override async Task<(uint id, ushort x, ushort y)> GetRevivePositionAsync(Character sender)
        {
            var data = GetPlayerData(sender.Identity);
            if (data != null && data.Deaths > MAX_DEATHS_PER_PLAYER)
                return (sender.RecordMapIdentity, sender.RecordMapX, sender.RecordMapY);

            int rand = (await Kernel.NextAsync(0, mReviveX.Length))%mReviveX.Length;
            return (MAP_ID, mReviveX[rand], mReviveY[rand]);
        }

        #endregion

        private class SyndicateData
        {
            public uint Identity { get; set; }
            public string Name { get; set; }
            public int Points { get; set; }

            public ConcurrentDictionary<uint, ContestPlayerData> PlayerData { get; set; }
        }

        private class ContestPlayerData
        {
            public SyndicateData SyndicateData { get; set; }

            public uint Identity { get; set; }
            public string Name { get; set; }
            public long Points { get; set; }
            public int Kills { get; set; }
            public int Deaths { get; set; }
            public int MonsterKills { get; set; }
        }
    }
}