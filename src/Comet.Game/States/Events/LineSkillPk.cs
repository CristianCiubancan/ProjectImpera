// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - LineSkillPk.cs
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
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Packets;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Items;
using Comet.Game.States.Magics;
using Comet.Shared;

namespace Comet.Game.States.Events
{
    public sealed class LineSkillPk : GameEvent
    {
        private const int MAP_ID_I = 2080;
        private const int MAX_REWARDS = 3;

        private ConcurrentDictionary<uint, Participant> m_participants = new ConcurrentDictionary<uint, Participant>();

        private TimeOutMS m_updateScreen = new TimeOutMS(RANK_REFRESH_RATE_MS);

        private uint[] m_rewardsActionIds = {
            110000000, // 1st
            110000050, // 2nd
            110000100, // 3rd
            0, // 4th
            0, // 5th
            0, // 6th
            0, // 7th
            0, // 8th
            0 // >8th
        };

        public LineSkillPk()
            : base("LineSkillPk", 1000)
        {
        }

        public override EventType Identity => EventType.LineSkillPk;

        public override bool IsInTime => DateTime.Now.Minute >= 10 && DateTime.Now.Minute < 20;
        public override bool IsActive => Stage == EventStage.Running && IsInTime;
        public override bool IsEnded => Stage == EventStage.Running && !IsInTime;

        public override async Task<bool> CreateAsync()
        {
            Map = Kernel.MapManager.GetMap(MAP_ID_I);
            if (Map == null)
            {
                _ = Log.WriteLogAsync(LogLevel.Error, $"LineSkillPK init error map not found").ConfigureAwait(false);
                return false;
            }
            
            return true;
        }

        public override bool IsAllowedToJoin(Role sender)
        {
            if (!(sender is Character user))
                return false;

            foreach (var participant in m_participants.Values)
            {
                Character player = Kernel.RoleManager.GetUser(participant.Identity);
                if (player.Client.MacAddress.Equals(user.Client.MacAddress))
                    return false;
            }

            return true;
        }

        public override Task<int> GetDamageLimitAsync(Role attacker, Role target, int power)
        {
            return Task.FromResult(1);
        }

        public override Task OnAttackAsync(Character sender)
        {
            var user = GetUser(sender);
            user.Attacks++;
            return Task.CompletedTask;
        }

        public override Task OnHitAsync(Role attacker, Role target, Magic magic = null)
        {
            if (!attacker.IsPlayer())
                return Task.CompletedTask;

            if (magic == null || magic.Sort != MagicData.MagicSort.Line)
                return Task.CompletedTask;

            if (!IsActive)
                return Task.CompletedTask;

#if !DEBUG
            if (attacker is Character testPlayer)
                if (testPlayer.IsGm())
                    return Task.CompletedTask;

            if (target is Character testTgtPlayer)
                if (testTgtPlayer.IsGm())
                    return Task.CompletedTask;
#endif

            var user = GetUser(attacker as Character);
            if (attacker is Character player && target is Character tgtPlayer && !player.Client.MacAddress.Equals(tgtPlayer.Client.MacAddress))
                user.AttacksSuccess++;
            return Task.CompletedTask;
        }

        public override Task OnBeAttackAsync(Role attacker, Role target, int damage, Magic magic)
        {
            if (!attacker.IsPlayer())
                return Task.CompletedTask;

            if (!IsActive)
                return Task.CompletedTask;

#if !DEBUG
            if (attacker is Character player)
                if (player.IsGm())
                    return Task.CompletedTask;

            if (target is Character tgtPlayer)
                if (tgtPlayer.IsGm())
                    return Task.CompletedTask;
#endif

            var user = GetUser(target as Character);
            user.ReceivedAttacks++;
            return Task.CompletedTask;
        }

        public override async Task OnEnterAsync(Character sender)
        {
            await sender.DetachAllStatusAsync();
            if (sender.UserPackage[Item.ItemPosition.LeftHand]?.IsShield() == true)
                await sender.UserPackage.UnEquipAsync(Item.ItemPosition.LeftHand);

            await sender.SetPkModeAsync(PkModeType.FreePk);

            if (sender.SyndicateIdentity != 0)
            {
                _ = Map.BroadcastMsgAsync(string.Format(Language.StrLineSkillPktAnnounceSyn, sender.Name, sender.SyndicateName));
            }
            else
            {
                _ = Map.BroadcastMsgAsync(string.Format(Language.StrLineSkillPktAnnounce, sender.Name));
            }

            _ = Log.GmLogAsync("xianjinengbisai", $"Enter\t{sender.Identity},{sender.Name},{sender.Client.IPAddress}");
        }

        public override Task OnExitAsync(Character sender)
        {
            return sender.SetPkModeAsync(); // set to capture
        }

        public override async Task OnTimerAsync()
        {
            if (IsInTime && !IsActive)
            {
                m_participants.Clear();
                Stage = EventStage.Running;
                m_updateScreen.Startup(RANK_REFRESH_RATE_MS);
                return;
            }

            if (IsActive)
            {
                if (m_updateScreen.ToNextTime())
                {
                    await Map.BroadcastMsgAsync(Language.StrLineSkillPktTitleRank, MsgTalk.TalkChannel.GuildWarRight1);
                    var list = m_participants.Values
                        .Where(x => CalculatePoints(x.Attacks, x.AttacksSuccess, x.ReceivedAttacks) > 0)
                        .OrderByDescending(x => CalculatePoints(x.Attacks, x.AttacksSuccess, x.ReceivedAttacks))
                        .Take(8);
                    int i = 1;
                    foreach (var ranked in list)
                    {
                        double points = CalculatePoints(ranked.Attacks, ranked.AttacksSuccess, ranked.ReceivedAttacks);
                        _ = Map.BroadcastMsgAsync(string.Format(Language.StrLineSkillPktUsrRank, i++, ranked.Name,
                            points,
                            ranked.AttacksSuccess, ranked.ReceivedAttacks), MsgTalk.TalkChannel.GuildWarRight2);
                    }

                    foreach (var user in m_participants.Values)
                    {
                        Character player = Kernel.RoleManager.GetUser(user.Identity);
                        if (player == null)
                            continue;

                        _ = player.SendAsync(string.Format(Language.StrLineSkillPktOwnRank,
                            CalculatePoints(user.Attacks, user.AttacksSuccess, user.ReceivedAttacks),
                            user.AttacksSuccess,
                            user.ReceivedAttacks), MsgTalk.TalkChannel.GuildWarRight2);
                    }
                }
            }

            if (IsEnded)
            {
                m_updateScreen.Clear();
                try
                {
                    foreach (var player in m_participants.Values)
                    {
                        Character user = Kernel.RoleManager.GetUser(player.Identity);
                        if (user != null)
                            await user.FlyMapAsync(user.RecordMapIdentity, user.RecordMapX, user.RecordMapY);
                    }

                    await DeliverRewardsAsync();
                }
                catch (Exception ex)
                {
                    _ = Log.WriteLogAsync(LogLevel.Warning, ex.ToString()).ConfigureAwait(false);
                }
                finally
                {
                    m_participants.Clear();
                    Stage = EventStage.Idle;
                }
                return;
            }
        }

        private async Task DeliverRewardsAsync()
        {
            int idx = 0;
            foreach (var player in m_participants.Values
                .Where(x => CalculatePoints(x.Attacks, x.AttacksSuccess, x.ReceivedAttacks) > 0)
                .OrderByDescending(x => CalculatePoints(x.Attacks, x.AttacksSuccess, x.ReceivedAttacks))
                .Take(MAX_REWARDS))
            {
                idx = Math.Min(m_rewardsActionIds.Length - 1, idx);

                uint idReward = m_rewardsActionIds[idx++];
                if (idReward == 0)
                    continue;

                Character user = Kernel.RoleManager.GetUser(player.Identity);
                if (user != null)
                {
                    _ = GameAction.ExecuteActionAsync(idReward, user, null, null, "");
                }
                else
                {
                    _ = BaseRepository.SaveAsync(new DbBonus
                    {
                        AccountIdentity = player.AccountIdentity,
                        Action = idReward,
                        ReferenceCode = MAP_ID_I
                    });
                }
            }
        }

        private double CalculatePoints(int attacks, int dealt, int recv)
        {
            //if (dealt == 0)
            //    return 0;

            //attacks = Math.Max(1, attacks);
            //dealt = Math.Max(1, dealt);
            //recv = Math.Max(1, recv);
            //double currPoints = Math.Max(dealt / (double) attacks + dealt / (double) recv, 0.01d);
            return dealt;
        }
        
        private Participant GetUser(uint idUser)
        {
            return m_participants.TryGetValue(idUser, out var user) ? user : null;
        }

        private Participant GetUser(Character user)
        {
            if (m_participants.TryGetValue(user.Identity, out var player))
                return player;
            return !m_participants.TryAdd(user.Identity, player = new Participant
            {
                Identity = user.Identity,
                AccountIdentity = user.Client.AccountIdentity,
                Name = user.Name,
                IpAddress = user.Client.IPAddress
            }) ? null : player;
        }

        private class Participant
        {
            public uint Identity { get; set; }
            public uint AccountIdentity { get; set; }
            public string Name { get; set; }
            public int Attacks { get; set; }
            public int AttacksSuccess { get; set; }
            public int ReceivedAttacks { get; set; }
            public string IpAddress { get; set; }
        }
    }
}