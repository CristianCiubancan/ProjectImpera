// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Team.cs
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
using Comet.Game.Database.Models;
using Comet.Game.Packets;
using Comet.Game.States.BaseEntities;
using Comet.Game.World.Maps;
using Comet.Network.Packets;

namespace Comet.Game.States
{
    public sealed class Team
    {
        public const int MAX_MEMBERS = 5;

        private Character m_leader;
        private ConcurrentDictionary<uint, Character> m_dicPlayers = new ConcurrentDictionary<uint, Character>();

        public Team(Character leader)
        {
            m_leader = leader;
            JoinEnable = true;
            MoneyEnable = true;
        }

        public Character Leader => m_leader;

        public bool JoinEnable { get; set; }
        public bool MoneyEnable { get; set; }
        public bool ItemEnable { get; set; }
        public bool JewelEnable { get; set; }

        public ICollection<Character> Members => m_dicPlayers.Values;

        public int MemberCount => m_dicPlayers.Count;


        public bool Create()
        {
            if (m_leader.Team != null)
                return false;

            m_dicPlayers.TryAdd(m_leader.Identity, m_leader);
            m_leader.Team = this;
            return true;
        }

        /// <summary>
        /// Erase the team.
        /// </summary>
        public async Task<bool> DismissAsync(Character request, bool disconnect = false)
        {
            if (request.Identity != m_leader.Identity)
            {
                await request.SendAsync(Language.StrTeamDismissNoLeader);
                return false;
            }

            await SendAsync(new MsgTeam
            {
                Action = MsgTeam.TeamAction.Dismiss,
                Identity = m_leader.Identity
            }, disconnect ? request.Identity : 0);

            foreach (var member in m_dicPlayers.Values)
            {
                member.Team = null;
            }

            return true;
        }

        public async Task<bool> DismissMemberAsync(Character user)
        {
            if (!m_dicPlayers.TryRemove(user.Identity, out var target))
                return false;

            await SendAsync(new MsgTeam
            {
                Identity = user.Identity,
                Action = MsgTeam.TeamAction.LeaveTeam
            });
            user.Team = null;

            await SyncFamilyBattlePowerAsync();
            return true;
        }

        public async Task<bool> KickMemberAsync(Character leader, uint idTarget)
        {
            if (!IsLeader(leader.Identity) || !m_dicPlayers.TryGetValue(idTarget, out var target))
                return false;

            await SendAsync(new MsgTeam
            {
                Identity = idTarget,
                Action = MsgTeam.TeamAction.Kick
            });

            m_dicPlayers.TryRemove(idTarget, out _);
            target.Team = null;

            await SyncFamilyBattlePowerAsync();
            return true;
        }

        public async Task<bool> EnterTeamAsync(Character target)
        {
            if (!m_dicPlayers.TryAdd(target.Identity, target))
                return false;

            target.Team = this;
            await SendShowAsync(target);

            await target.SendAsync(string.Format(Language.StrPickupSilvers, MoneyEnable ? Language.StrOpen : Language.StrClose));
            await target.SendAsync(string.Format(Language.StrTeamItems, ItemEnable ? Language.StrOpen : Language.StrClose));
            await target.SendAsync(string.Format(Language.StrTeamGems, JewelEnable ? Language.StrOpen : Language.StrClose));

            await SyncFamilyBattlePowerAsync();
            return true;
        }

        public bool IsLeader(uint id)
        {
            return m_leader.Identity == id;
        }

        public bool IsMember(uint id)
        {
            return m_dicPlayers.ContainsKey(id);
        }

        public async Task SendShowAsync(Character target)
        {
            await target.SendAsync(new MsgTeamMember
            {
                Action = MsgTeamMember.ADD_MEMBER_B,
                Members = new List<MsgTeamMember.TeamMember>
                {
                    new MsgTeamMember.TeamMember
                    {
                        Identity = Leader.Identity,
                        Name = Leader.Name,
                        MaxLife = (ushort) Leader.MaxLife,
                        Life = (ushort) Leader.Life,
                        Lookface = Leader.Mesh
                    }
                }
            });

            await target.SendAsync(new MsgTeamMember
            {
                Action = MsgTeamMember.ADD_MEMBER_B,
                Members = new List<MsgTeamMember.TeamMember>
                {
                    new MsgTeamMember.TeamMember
                    {
                        Identity = target.Identity,
                        Name = target.Name,
                        MaxLife = (ushort) target.MaxLife,
                        Life = (ushort) target.Life,
                        Lookface = target.Mesh
                    }
                }
            });

            foreach (var member in m_dicPlayers.Values)
            {
                await member.SendAsync(new MsgTeamMember
                {
                    Action = MsgTeamMember.ADD_MEMBER_B,
                    Members = new List<MsgTeamMember.TeamMember>
                    {
                        new MsgTeamMember.TeamMember
                        {
                            Identity = target.Identity,
                            Name = target.Name,
                            MaxLife = (ushort) target.MaxLife,
                            Life = (ushort) target.Life,
                            Lookface = target.Mesh
                        }
                    }
                });

                if (target.Identity != member.Identity)
                    await target.SendAsync(new MsgTeamMember
                    {
                        Action = MsgTeamMember.ADD_MEMBER_B,
                        Members = new List<MsgTeamMember.TeamMember>
                        {
                            new MsgTeamMember.TeamMember
                            {
                                Identity = member.Identity,
                                Name = member.Name,
                                MaxLife = (ushort) member.MaxLife,
                                Life = (ushort) member.Life,
                                Lookface = member.Mesh
                            }
                        }
                    });
            }
        }

        public async Task SendAsync(IPacket msg, uint exclude = 0)
        {
            foreach (var player in m_dicPlayers.Values)
            {
                if (exclude == player.Identity)
                    continue;
                await player.SendAsync(msg);
            }
        }

        public async Task BroadcastMemberLifeAsync(Character user, bool maxLife = false)
        {
            if (user == null || !IsMember(user.Identity))
                return;

            MsgUserAttrib msg = new MsgUserAttrib(user.Identity, ClientUpdateType.Hitpoints, user.Life);
            if (maxLife)
                msg.Append(ClientUpdateType.MaxHitpoints, user.MaxLife);

            foreach (var member in m_dicPlayers.Values)
            {
                if (member.Identity != user.Identity)
                {
                    await member.SendAsync(msg);
                }
            }
        }

        public async Task AwardMemberExpAsync(uint idKiller, Role target, long exp)
        {
            if (target == null || exp == 0)
                return;

            if (!m_dicPlayers.TryGetValue(idKiller, out var killer))
                return;

            foreach (var user in m_dicPlayers.Values)
            {
                if (user.Identity == idKiller)
                    continue;

                if (!user.IsAlive)
                    continue;

                if (user.MapIdentity != killer.MapIdentity)
                    continue;

                if (user.GetDistance(killer) > Screen.VIEW_SIZE * 2)
                    continue;

                DbLevelExperience dbExp = Kernel.RoleManager.GetLevelExperience(user.Level);
                if (dbExp == null)
                    continue;

                long addExp = user.AdjustExperience(target, exp, false);
                addExp = (long) Math.Min(dbExp.Exp, (ulong) addExp);
                addExp = Math.Max(1, Math.Min(user.Level * 360, addExp));

                addExp = (int)Math.Min(addExp, user.Level * 360);

                if (user.IsMate(killer) || user.IsApprentice(idKiller))
                    addExp *= 2;

                await user.AwardBattleExpAsync(addExp, true);
                await user.SendAsync(string.Format(Language.StrTeamExperience, addExp));
            }
        }

        public int FamilyBattlePower(Character user, out uint idProvider)
        {
            idProvider = 0;
            if (!m_dicPlayers.ContainsKey(user.Identity))
                return 0;

            if (user.FamilyIdentity == 0)
                return 0;

            Character clanMember = m_dicPlayers.Values
                .OrderByDescending(x => x.PureBattlePower)
                .FirstOrDefault(x => x.Identity != user.Identity && x.MapIdentity == user.MapIdentity && x.FamilyIdentity == user.FamilyIdentity);
            
            if (clanMember == null || clanMember.PureBattlePower <= user.PureBattlePower)
                return 0;

            var limit = Kernel.FamilyManager.GetSharedBattlePowerLimit(user.PureBattlePower);
            if (limit == null)
                return 0;

            idProvider = clanMember.Identity;
            int value = (int) ((clanMember.PureBattlePower - user.PureBattlePower) * (user.Family.SharedBattlePowerFactor / 100d));
            value = Math.Min(Math.Max(0, value), limit.ShareLimit);
            return value;
        }

        public async Task SyncFamilyBattlePowerAsync()
        {
            foreach (var member in m_dicPlayers.Values)
            {
                await member.SynchroFamilyBattlePowerAsync();
            }
        }
    }
}