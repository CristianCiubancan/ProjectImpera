// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Syndicate.cs
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
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets;
using Comet.Game.States.Events;
using Comet.Game.States.Items;
using Comet.Network.Packets;
using Comet.Shared;
using Microsoft.EntityFrameworkCore;

namespace Comet.Game.States.Syndicates
{
    public sealed class Syndicate
    {
        #region Private Getters

        private int MaxDeputyLeader => Level < 4 ? 2 : Level < 7 ? 3 : 4;

        private int MaxHonoraryDeputyLeader => Level < 6 ? 1 : 2;

        private int MaxHonoraryManager => Level < 5 ? 1 : Level < 7 ? 2 : Level < 9 ? 4 : 6;

        private int MaxHonorarySupervisor => Level < 5 ? 1 : Level < 7 ? 2 : Level < 9 ? 4 : 6;

        private int MaxHonorarySteward => Level < 3 ? 1 : Level < 5 ? 2 : Level < 7 ? 4 : Level < 9 ? 6 : 8;

        private int MaxAide => Level < 4 ? 2 : Level < 6 ? 4 : 6;

        private int MaxManager
        {
            get
            {
                switch (Level)
                {
                    case 1:
                    case 2: return 1;
                    case 3:
                    case 4: return 2;
                    case 5:
                    case 6: return 4;
                    case 7:
                    case 8: return 6;
                    case 9: return 8;
                    default: return 0;
                }
            }
        }

        private int MaxSupervisor => Level < 4 ? 0 : Level < 8 ? 1 : 2;

        private int MaxSteward
        {
            get
            {
                switch (Level)
                {
                    case 0:
                    case 1: return 0;
                    case 2: return 1;
                    case 3: return 2;
                    case 4: return 3;
                    case 5: return 4;
                    case 6: return 5;
                    case 7: return 6;
                    case 8:
                    case 9: return 8;
                    default: return 0;
                }
            }
        }

        #endregion

        public const uint HONORARY_DEPUTY_LEADER_PRICE = 6500,
            HONORARY_MANAGER_PRICE = 3200,
            HONORARY_SUPERVISOR_PRICE = 2500,
            HONORARY_STEWARD_PRICE = 1000;

        private const int MEMBER_MIN_LEVEL = 15;
        private const int MAX_MEMBER_SIZE = 800;
        private const int DISBAND_MONEY = 100000;
        public const int SYNDICATE_ACTION_COST = 50000;
        private const int EXIT_MONEY = 20000;
        private const string DEFAULT_ANNOUNCEMENT_S = "This is a new guild.";
        private const int MAX_ALLY = 5;
        private const int MAX_ENEMY = 5;

        private DbSyndicate m_syndicate;
        private SyndicateMember m_leader;
        private ConcurrentDictionary<uint, SyndicateMember> m_dicMembers = new ConcurrentDictionary<uint, SyndicateMember>();
        private ConcurrentDictionary<ushort, Syndicate> m_dicAllies = new ConcurrentDictionary<ushort, Syndicate>();
        private ConcurrentDictionary<ushort, Syndicate> m_dicEnemies = new ConcurrentDictionary<ushort, Syndicate>();

        public Syndicate()
        {
            
        }

        public ushort Identity => m_syndicate.Identity;
        public string Name => m_syndicate.Name;
        public int MemberCount => m_dicMembers.Count;

        public int Money
        {
            get => (int) Math.Min(int.MaxValue, Math.Max(int.MinValue, m_syndicate.Money));
            set => m_syndicate.Money = value;
        }

        public byte Level
        {
            get => m_syndicate.Level;
            set => m_syndicate.Level = value;
        }

        public uint ConquerPoints
        {
            get => m_syndicate.ConquerPoints;
            set => m_syndicate.ConquerPoints = value;
        }

        public string Announce
        {
            get => m_syndicate.Announce;
            set => m_syndicate.Announce = value;
        }     

        public DateTime AnnounceDate
        {
            get => m_syndicate.AnnounceDate;
            set => m_syndicate.AnnounceDate = value;
        }

        public DateTime CreationDate => m_syndicate.CreationDate;

        public bool Deleted => m_syndicate.DelFlag != null;

        public SyndicateMember Leader => m_dicMembers.Values.FirstOrDefault(x => x.Rank == SyndicateMember.SyndicateRank.GuildLeader);

        #region Position Count

        public int DeputyLeaderCount => m_dicMembers.Values.Count(x => x.Rank == SyndicateMember.SyndicateRank.DeputyLeader);
        public int HonoraryDeputyLeaderCount => m_dicMembers.Values.Count(x => x.Rank == SyndicateMember.SyndicateRank.HonoraryDeputyLeader);
        public int HonoraryManagerCount => m_dicMembers.Values.Count(x => x.Rank == SyndicateMember.SyndicateRank.HonoraryManager);
        public int HonorarySupervisorCount => m_dicMembers.Values.Count(x => x.Rank == SyndicateMember.SyndicateRank.HonorarySupervisor);
        public int HonoraryStewardCount => m_dicMembers.Values.Count(x => x.Rank == SyndicateMember.SyndicateRank.HonorarySteward);

        #endregion

        #region Create

        public async Task<bool> CreateAsync(DbSyndicate syn)
        {
            m_syndicate = syn;

            if (Deleted)
                return true;

            List<DbSyndicateAttr> members = await SyndicateAttrRepository.GetAsync(Identity);
            foreach (var dbMember in members)
            {
                SyndicateMember member = new SyndicateMember();
                if (!await member.CreateAsync(dbMember, this))
                    continue;

                if (m_dicMembers.TryAdd(member.UserIdentity, member))
                {
                    if (member.Rank == SyndicateMember.SyndicateRank.GuildLeader)
                        m_leader = member;
                }
            }

            if (MemberCount == 0)
                m_syndicate.DelFlag = DateTime.Now;

            m_syndicate.Amount = (uint) MemberCount;

            Level = 1;

            var totemAdds = await DbTotemAdd.GetAsync(Identity);
            var items = await ItemRepository.GetBySyndicateAsync(Identity) ?? new List<DbItem>();            
            for (TotemPoleType totemPole = TotemPoleType.Headgear; totemPole < TotemPoleType.None; totemPole++)
            {
                TotemPole pole = new TotemPole(totemPole, totemAdds.FirstOrDefault(x => x.TotemType == (int)totemPole));
                switch (totemPole)
                {
                    case TotemPoleType.Headgear: pole.Locked = !HasTotemHeadgear; break;
                    case TotemPoleType.Necklace: pole.Locked = !HasTotemNecklace; break;
                    case TotemPoleType.Ring: pole.Locked = !HasTotemRing; break;
                    case TotemPoleType.Weapon: pole.Locked = !HasTotemWeapon; break;
                    case TotemPoleType.Armor: pole.Locked = !HasTotemArmor; break;
                    case TotemPoleType.Boots: pole.Locked = !HasTotemBoots; break;
                    case TotemPoleType.Fan: pole.Locked = !HasTotemFan; break;
                    case TotemPoleType.Tower: pole.Locked = !HasTotemTower; break;
                }

                if (!pole.Locked)
                {
                    foreach (var item in items.Where(x => GetTotemPoleType(x.Type) == totemPole))
                    {
                        if (!m_dicMembers.TryGetValue(item.PlayerId, out var member))
                        {
                            item.Syndicate = 0;
                            await BaseRepository.SaveAsync(item);
                            continue;
                        }

                        pole.Totems.TryAdd(item.Id, new Totem(item, member.UserName));
                    }
                }

                m_totemPoles.TryAdd(totemPole, pole);

                if (pole.Donation >= 5000000)
                    Level += 1;
            }

            UpdateBattlePower();

            foreach (var member in m_dicMembers.Values.Where(x => !IsUserSetPosition(x.Rank)))
            {
                if (Leader.MateIdentity == member.UserIdentity 
                    && member.Rank != SyndicateMember.SyndicateRank.LeaderSpouse)
                {
                    member.Rank = SyndicateMember.SyndicateRank.LeaderSpouse;
                    await member.SaveAsync();
                    continue;
                }

                member.Rank = SyndicateMember.SyndicateRank.Member;
            }

            uint amount = 0;
            uint maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.Manager);

            #region Manager

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.UsableDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.Manager;
                await member.SaveAsync();
            }

            #endregion

            #region Rose Supervisor

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.RoseSupervisor);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.RedRoseDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.RoseSupervisor;
                await member.SaveAsync();
            }

            #endregion
            #region White Rose Supervisor

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.LilySupervisor);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.WhiteRoseDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.LilySupervisor;
                await member.SaveAsync();
            }

            #endregion
            #region Orchid Supervisor

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.OrchidSupervisor);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.OrchidDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.OrchidSupervisor;
                await member.SaveAsync();
            }

            #endregion
            #region Tulip Supervisor

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.TulipSupervisor);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.TulipDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.TulipSupervisor;
                await member.SaveAsync();
            }

            #endregion
            #region Pk Supervisor

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.PkSupervisor);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.PkDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.PkSupervisor;
                await member.SaveAsync();
            }

            #endregion
            #region Guide Supervisor

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.GuideSupervisor);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.GuideDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.GuideSupervisor;
                await member.SaveAsync();
            }

            #endregion
            #region Silver Supervisor

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.SilverSupervisor);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.Silvers))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.SilverSupervisor;
                await member.SaveAsync();
            }

            #endregion
            #region CPs Supervisor

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.CpSupervisor);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.ConquerPointsDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.CpSupervisor;
                await member.SaveAsync();
            }

            #endregion
            #region Arsenal Supervisor

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.ArsenalSupervisor);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.ArsenalDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.ArsenalSupervisor;
                await member.SaveAsync();
            }

            #endregion
            #region Supervisor

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.Supervisor);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.UsableDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.Supervisor;
                await member.SaveAsync();
            }

            #endregion

            #region Steward

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.Steward);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.UsableDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.Steward;
                await member.SaveAsync();
            }

            #endregion
            #region Deputy Steward

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member && x.UsableDonation >= 170000))
            {
                member.Rank = SyndicateMember.SyndicateRank.DeputySteward;
                await member.SaveAsync();
            }

            #endregion

            #region Rose Agent

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.RoseAgent);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.RedRoseDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.RoseAgent;
                await member.SaveAsync();
            }

            #endregion
            #region White Rose Agent

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.LilyAgent);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.WhiteRoseDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.LilyAgent;
                await member.SaveAsync();
            }

            #endregion
            #region Orchid Agent

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.OrchidAgent);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.OrchidDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.OrchidAgent;
                await member.SaveAsync();
            }

            #endregion
            #region Tulip Agent

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.TulipAgent);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.TulipDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.TulipAgent;
                await member.SaveAsync();
            }

            #endregion
            #region Pk Agent

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.PkAgent);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.PkDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.PkAgent;
                await member.SaveAsync();
            }

            #endregion
            #region Guide Agent

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.GuideAgent);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.GuideDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.GuideAgent;
                await member.SaveAsync();
            }

            #endregion
            #region Silver Agent

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.SilverAgent);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.Silvers))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.SilverAgent;
                await member.SaveAsync();
            }

            #endregion
            #region CPs Agent

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.CpAgent);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.ConquerPointsDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.CpAgent;
                await member.SaveAsync();
            }

            #endregion
            #region Arsenal Agent

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.ArsenalAgent);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.ArsenalDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.ArsenalAgent;
                await member.SaveAsync();
            }

            #endregion
            #region Agent

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.Agent);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.UsableDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.Agent;
                await member.SaveAsync();
            }

            #endregion

            #region Rose Follower

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.RoseFollower);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.RedRoseDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.RoseFollower;
                await member.SaveAsync();
            }

            #endregion
            #region White Rose Follower

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.LilyFollower);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.WhiteRoseDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.LilyFollower;
                await member.SaveAsync();
            }

            #endregion
            #region Orchid Follower

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.OrchidFollower);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.OrchidDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.OrchidFollower;
                await member.SaveAsync();
            }

            #endregion
            #region Tulip Follower

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.TulipFollower);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.TulipDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.TulipFollower;
                await member.SaveAsync();
            }

            #endregion
            #region Pk Follower

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.PkFollower);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.PkDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.PkFollower;
                await member.SaveAsync();
            }

            #endregion
            #region Guide Follower

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.GuideFollower);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.GuideDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.GuideFollower;
                await member.SaveAsync();
            }

            #endregion
            #region Silver Follower

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.SilverFollower);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.Silvers))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.SilverFollower;
                await member.SaveAsync();
            }

            #endregion
            #region CPs Follower

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.CpFollower);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.ConquerPointsDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.CpFollower;
                await member.SaveAsync();
            }

            #endregion
            #region Arsenal Follower

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.ArsenalFollower);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.ArsenalDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.ArsenalFollower;
                await member.SaveAsync();
            }

            #endregion
            #region Follower

            amount = 0;
            maxAmount = MaxPositionAmount(SyndicateMember.SyndicateRank.Follower);

            foreach (var member in m_dicMembers
                .Values
                .Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                .OrderByDescending(x => x.UsableDonation))
            {
                if (amount++ >= maxAmount)
                    break;
                member.Rank = SyndicateMember.SyndicateRank.Follower;
                await member.SaveAsync();
            }

            #endregion

            await SaveAsync();
            UpdateBattlePower();
            return true;
        }

        public async Task<bool> CreateAsync(string name, int investment, Character leader)
        {
            if (Kernel.SyndicateManager.GetSyndicate(name) != null)
            {
                await leader.SendAsync(Language.StrSynNameInUse);
                return false;
            }

            m_syndicate = new DbSyndicate
            {
                Amount = 1,
                Announce = DEFAULT_ANNOUNCEMENT_S,
                AnnounceDate = DateTime.Now,
                ConquerPoints = 0,
                CreationDate = DateTime.Now,
                LeaderIdentity = leader.Identity,
                LeaderName = leader.Name,
                Money = investment/2,
                Name = name,
                ReqClass = 0,
                ReqMetempsychosis = 0,
                ReqLevel = 1,
                Level = 1
            };

            if (!await SaveAsync())
                return false;

            m_leader = new SyndicateMember();
            if (!await m_leader.CreateAsync(leader, this, SyndicateMember.SyndicateRank.GuildLeader))
            {
                await DeleteAsync();
                return false;
            }

            m_leader.Silvers = investment / 2;
            await m_leader.SaveAsync();

            m_dicMembers.TryAdd(m_leader.UserIdentity, m_leader);

            for (TotemPoleType totemPole = TotemPoleType.Headgear; totemPole < TotemPoleType.None; totemPole++)
            {
                m_totemPoles.TryAdd(totemPole, new TotemPole(totemPole));
            }

            return true;
        }

        public async Task LoadRelationAsync()
        {
            var dbAllies = await SyndicateAllyRepository.GetAsync(Identity);
            foreach (var ally in dbAllies)
            {
                // validate alliance
                if ((await SyndicateAllyRepository.GetAsync(ally.AllyIdentity)).All(x => x.AllyIdentity != ally.SyndicateIdentity))
                {
                    await BaseRepository.DeleteAsync(ally);
                    continue;
                }

                Syndicate syndicate = Kernel.SyndicateManager.GetSyndicate((int) ally.AllyIdentity);
                if (syndicate == null || syndicate.Deleted)
                {
                    // invalid ally
                    await BaseRepository.DeleteAsync(ally);
                    continue;
                }

                m_dicAllies.TryAdd(syndicate.Identity, syndicate);
            }

            var dbEnemies = await SyndicateEnemyRepository.GetAsync(Identity);
            foreach (var enemy in dbEnemies)
            {
                Syndicate syndicate = Kernel.SyndicateManager.GetSyndicate((int)enemy.EnemyIdentity);
                if (syndicate == null || syndicate.Deleted)
                {
                    // invalid ally
                    await BaseRepository.DeleteAsync(enemy);
                    continue;
                }

                m_dicEnemies.TryAdd(syndicate.Identity, syndicate);
            }
        }

        #endregion

        #region Disband

        public async Task<bool> DisbandAsync(Character user)
        {
            if (user.Identity != Leader.UserIdentity)
                return false;

            if (MemberCount > 1)
                return false;

            if (Money < DISBAND_MONEY)
            {
                return false;
            }

            user.Syndicate = null;

            if (m_dicMembers.TryRemove(user.Identity, out var member))
            {
                await member.DeleteAsync();
            }

            ExitFromEvents();

            await UnsubscribeAllAsync(user.Identity);

            // additional clean up
            await new ServerDbContext().Database.ExecuteSqlRawAsync($"DELETE FROM `cq_synattr` WHERE `syn_id`={Identity}");

            foreach (var ally in m_dicAllies.Values)
            {
                await RemoveAllyAsync(ally.Identity);
            }

            foreach (var enemy in m_dicEnemies.Values)
            {
                await PeaceAsync(user, enemy);
            }

            await user.SendAsync(new MsgSyndicate
            {
                Identity = Identity,
                Mode = MsgSyndicate.SyndicateRequest.Disband
            });

            await user.Screen.SynchroScreenAsync();
            await Kernel.RoleManager.BroadcastMsgAsync(string.Format(Language.StrSynDestroy, Name));
            return await SoftDeleteAsync();
        }

        #endregion

        #region Member Management

        public async Task<bool> AppendMemberAsync(Character target, Character caller, JoinMode mode)
        {
            if ((mode == JoinMode.Invite || mode == JoinMode.Request) && caller == null)
            {
                return false;
            }

            if (target.SyndicateIdentity != 0)
                return false;

            if (target.Level < MEMBER_MIN_LEVEL)
                return false;

            if (MemberCount >= MAX_MEMBER_SIZE)
                return false;

            if (target.Level < LevelRequirement)
                return false;

            if (target.Metempsychosis < MetempsychosisRequirement)
                return false;

            if (target.CurrentEvent != null)
            {
                return false; // cannot join while in event
            }

            switch (target.ProfessionSort)
            {
                case 10:
                    if (!AllowTrojan)
                        return false;
                    break;
                case 20:
                    if (!AllowWarrior)
                        return false;
                    break;
                case 40:
                    if (!AllowArcher)
                        return false;
                    break;
                case 50:
                    if (!AllowNinja)
                        return false;
                    break;
                case 100:
                    if (!AllowTaoist)
                        return false;
                    break;
            }

            if (Money < SYNDICATE_ACTION_COST)
            {
                await caller.SendAsync(string.Format(Language.StrSynNoMoney, SYNDICATE_ACTION_COST));
                return false;
            }

            var newMember = new SyndicateMember();
            if (!await newMember.CreateAsync(target, this, SyndicateMember.SyndicateRank.Member))
                return false;

            if (!m_dicMembers.TryAdd(newMember.UserIdentity, newMember))
            {
                await newMember.DeleteAsync();
                return false;
            }

            target.Syndicate = this;

            await target.SendSyndicateAsync();
            await SendAsync(target);
            await SendRelationAsync(target);
            await target.Screen.SynchroScreenAsync();

            m_syndicate.Amount = (uint) MemberCount;
            await SaveAsync();

            switch (mode)
            {
                case JoinMode.Invite:
                    await SendAsync(string.Format(Language.StrSynInviteGuild, caller.SyndicateMember.RankName, caller.Name, target.Name));
                    break;
                case JoinMode.Request:
                    await SendAsync(string.Format(Language.StrSynJoinGuild, caller.SyndicateMember.RankName, caller.Name, target.Name));
                    break;
                case JoinMode.Recruitment:
                    break;
            }

            if (m_dicMembers.TryGetValue(target.MateIdentity, out var mate))
            {
                var matePos = GetSpousePosition(mate.Rank);
                if (matePos != SyndicateMember.SyndicateRank.None && matePos > mate.Rank && IsSystemDefinedPosition(mate.Rank))
                    await SetSpouseAsync(target.SyndicateMember, matePos);
            }

            return true;
        }

        public async Task<bool> QuitSyndicateAsync(Character target)
        {
            if (target.SyndicateRank == SyndicateMember.SyndicateRank.GuildLeader)
                return false;

            if (!m_dicMembers.TryGetValue(target.Identity, out var member))
                return false;

            if (target.CurrentEvent != null)
                return false;

            if (member.Silvers < EXIT_MONEY)
            {
                await target.SendAsync(string.Format(Language.StrSynExitNotEnoughMoney, EXIT_MONEY));
                return false;
            }

            m_dicMembers.TryRemove(target.Identity, out _);

            target.Syndicate = null;

            await target.SendAsync(new MsgSyndicate
            {
                Identity = Identity,
                Mode = MsgSyndicate.SyndicateRequest.Disband
            });

            m_syndicate.Amount = (uint) MemberCount;
            await SaveAsync();

            await target.Screen.SynchroScreenAsync();

            await BaseRepository.SaveAsync(new DbSyndicateMemberHistory
            {
                UserIdentity = member.UserIdentity,
                JoinDate = member.JoinDate,
                LeaveDate = DateTime.Now,
                SyndicateIdentity = Identity,
                Rank = (ushort)member.Rank,
                Silver = member.Silvers,
                ConquerPoints = 0,
                Guide = 0,
                PkPoints = 0
            });

            RemoveUserFromEvents(target.Identity);

            await member.DeleteAsync();
            await SendAsync(string.Format(Language.StrSynMemberExit, target.Name));

            await UnsubscribeAllAsync(target.Identity);

            if (m_dicMembers.TryGetValue(member.MateIdentity, out var mate))
            {
                if (IsSpousePosition(mate.Rank))
                    await SetSpouseAsync(mate, SyndicateMember.SyndicateRank.Member);
            }

            return true;
        }

        public async Task<bool> KickOutMemberAsync(Character sender, string name)
        {
            if (sender.SyndicateRank < SyndicateMember.SyndicateRank.DeputyLeader)
                return false;

            if (Money < SYNDICATE_ACTION_COST)
            {
                await sender.SendAsync(string.Format(Language.StrSynNoMoney, SYNDICATE_ACTION_COST));
                return false;
            }

            var member = QueryMember(name);
            if (member == null)
                return false;

            if (member.Rank == SyndicateMember.SyndicateRank.GuildLeader)
                return false;

            if (!m_dicMembers.TryRemove(member.UserIdentity, out _))
                return false;

            Character target = member.User;
            if (target != null)
            {
                target.Syndicate = null;
                await target.SendAsync(new MsgSyndicate
                {
                    Identity = Identity,
                    Mode = MsgSyndicate.SyndicateRequest.Disband
                });
                await target.Screen.SynchroScreenAsync();
                await target.SendAsync(string.Format(Language.StrSynYouBeenKicked, sender.Name));
            }

            RemoveUserFromEvents(member.UserIdentity);

            await BaseRepository.SaveAsync(new DbSyndicateMemberHistory
            {
                UserIdentity = member.UserIdentity,
                JoinDate = member.JoinDate,
                LeaveDate = DateTime.Now,
                SyndicateIdentity = Identity,
                Rank = (ushort) member.Rank,
                Silver = member.Silvers,
                ConquerPoints = 0,
                Guide = 0,
                PkPoints = 0
            });

            m_syndicate.Amount = (uint)MemberCount;
            await SaveAsync();
            await member.DeleteAsync();
            await SendAsync(string.Format(Language.StrSynMemberKickout, sender.SyndicateMember.RankName, sender.Name, member.UserName));

            await UnsubscribeAllAsync(member.UserIdentity);

            if (m_dicMembers.TryGetValue(member.MateIdentity, out var mate))
            {
                if (IsSpousePosition(mate.Rank))
                    await SetSpouseAsync(mate, SyndicateMember.SyndicateRank.Member);
            }

            return true;
        }

        #endregion

        #region Promote and Demote

        public Task<bool> PromoteAsync(Character sender, string target, SyndicateMember.SyndicateRank position)
        {
            Character user = Kernel.RoleManager.GetUser(target);
            if (user == null || user.SyndicateIdentity != sender.SyndicateIdentity)
                return Task.FromResult(false);
            return PromoteAsync(sender, user, position);
        }

        public Task<bool> PromoteAsync(Character sender, uint target, SyndicateMember.SyndicateRank position)
        {
            Character user = Kernel.RoleManager.GetUser(target);
            if (user == null || user.SyndicateIdentity != sender.SyndicateIdentity)
                return Task.FromResult(false);
            return PromoteAsync(sender, user, position);
        }

        public async Task<bool> PromoteAsync(Character sender, Character target, SyndicateMember.SyndicateRank position)
        {
            if (target.SyndicateRank == SyndicateMember.SyndicateRank.GuildLeader)
                return false;

            if (target.SyndicateIdentity != Identity)
                return false;

            uint cost = 0;
            switch (position)
            {
                case SyndicateMember.SyndicateRank.GuildLeader:
                {
                    if (sender.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                        return false;

                    break;
                }

                case SyndicateMember.SyndicateRank.DeputyLeader:
                {
                    if (sender.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                        return false;

                    if (DeputyLeaderCount >= MaxDeputyLeader)
                        return false;
                    break;
                }

                case SyndicateMember.SyndicateRank.HonoraryDeputyLeader:
                {
                    if (sender.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                        return false;

                    if (HonoraryDeputyLeaderCount >= MaxHonoraryDeputyLeader)
                        return false;

                    cost = HONORARY_DEPUTY_LEADER_PRICE;
                    break;
                }

                case SyndicateMember.SyndicateRank.HonoraryManager:
                {
                    if (sender.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                        return false;

                    if (HonoraryManagerCount >= MaxHonoraryManager)
                        return false;

                    cost = HONORARY_MANAGER_PRICE;
                    break;
                }

                case SyndicateMember.SyndicateRank.HonorarySupervisor:
                {
                    if (sender.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                        return false;

                    if (HonorarySupervisorCount >= MaxHonorarySupervisor)
                        return false;

                    cost = HONORARY_SUPERVISOR_PRICE;
                    break;
                }

                case SyndicateMember.SyndicateRank.HonorarySteward:
                {
                    if (sender.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                        return false;

                    if (HonoraryStewardCount >= MaxHonorarySteward)
                        return false;

                    cost = HONORARY_STEWARD_PRICE;
                    break;
                }

                default:
                {
                    if (!IsMasterPosition(sender.SyndicateRank))
                        return false;

                    var assistant = GetAssistantPosition(sender.SyndicateRank);
                    if (assistant != position)
                        return false;

                    if (sender.SyndicateMember.AssistantIdentity != 0)
                        return false;

                    if (target.SyndicateMember.MasterIdentity != 0)
                        return false;

                    if (IsUserSetPosition(target.SyndicateRank))
                        return false;
                    break;
                }
            }

            if (Money < SYNDICATE_ACTION_COST)
            {
                await sender.SendAsync(string.Format(Language.StrSynNoMoney, SYNDICATE_ACTION_COST));
                return false;
            }

            if (cost > 0)
            {
                if (ConquerPoints < cost)
                    return false;

                ConquerPoints -= cost;
                await SaveAsync();
            }

            if (position == SyndicateMember.SyndicateRank.GuildLeader) // abdicate
            {
                sender.SyndicateMember.Rank = SyndicateMember.SyndicateRank.Member;
                await sender.SendSyndicateAsync();
                await sender.Screen.SynchroScreenAsync();
                await sender.SyndicateMember.SaveAsync();

                await SendAsync(string.Format(Language.StrSynAbdicate, sender.Name, target.Name));
            }
            else
            {
                await SendAsync(string.Format(Language.StrSynPromoted, sender.SyndicateMember.RankName, sender.Name, target.Name,
                    position));
            }

            target.SyndicateMember.Rank = position;

            if (cost > 0)
                target.SyndicateMember.PositionExpiration = DateTime.Now.AddDays(30);

            await target.SendSyndicateAsync();
            await target.Screen.SynchroScreenAsync();
            await target.SyndicateMember.SaveAsync();

            if (m_dicMembers.TryGetValue(target.MateIdentity, out var mate))
            {
                var matePos = GetSpousePosition(mate.Rank);
                if (matePos != SyndicateMember.SyndicateRank.None && matePos > mate.Rank && IsSystemDefinedPosition(mate.Rank))
                    await SetSpouseAsync(target.SyndicateMember, matePos);
            }

            return true;
        }

        public Task<bool> DemoteAsync(Character sender, string name)
        {
            var member = QueryMember(name);
            if (member == null)
                return Task.FromResult(false);
            return DemoteAsync(sender, member);
        }

        public Task<bool> DemoteAsync(Character sender, uint target)
        {
            var member = QueryMember(target);
            if (member == null)
                return Task.FromResult(false);
            return DemoteAsync(sender, member);
        }

        public async Task<bool> DemoteAsync(Character sender, SyndicateMember member)
        {
            if (sender.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                return false;

            if (member.Rank == SyndicateMember.SyndicateRank.GuildLeader)
                return false;

            if (IsSpousePosition(member.Rank))
                return false;

            if (sender.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
            {
                if (IsAssistantPosition(member.Rank) && sender.Identity != member.MasterIdentity)
                    return false;

                if (IsSystemDefinedPosition(member.Rank))
                {
                    return false;
                }
            }

            if (Money < SYNDICATE_ACTION_COST)
            {
                await sender.SendAsync(string.Format(Language.StrSynNoMoney, SYNDICATE_ACTION_COST));
                return false;
            }

            member.Rank = SyndicateMember.SyndicateRank.Member;

            if (member.PositionExpiration != null)
                member.PositionExpiration = null;

            if (member.User != null)
            {
                await member.User.SendSyndicateAsync();
                await member.User.Screen.SynchroScreenAsync();
            }
            await member.SaveAsync();

            if (m_dicMembers.TryGetValue(member.MateIdentity, out var mate))
            {
                if (IsSpousePosition(mate.Rank))
                    await SetSpouseAsync(mate, SyndicateMember.SyndicateRank.Member);
            }
            return true;
        }

        /// <remarks>USER MUST BE ONLINE!</remarks>
        public static async Task SetSpouseAsync(SyndicateMember member, SyndicateMember.SyndicateRank rank)
        {
            member.Rank = rank;
            if (member.User != null)
            {
                await member.User.SendSyndicateAsync();
                await member.User.Screen.SynchroScreenAsync();
            }
            await member.SaveAsync();
        }

        public Task SendPromotionListAsync(Character target)
        {
            MsgSyndicate msg = new MsgSyndicate
            {
                Mode = MsgSyndicate.SyndicateRequest.PromotionList
            };

            if (target.SyndicateRank == SyndicateMember.SyndicateRank.GuildLeader)
            {
                msg.Strings.Add($"{(int) SyndicateMember.SyndicateRank.GuildLeader} 1 1 {GetSharedBattlePower(SyndicateMember.SyndicateRank.GuildLeader)} 0");
                msg.Strings.Add($"{(int) SyndicateMember.SyndicateRank.DeputyLeader} {DeputyLeaderCount} {MaxDeputyLeader} {GetSharedBattlePower(SyndicateMember.SyndicateRank.DeputyLeader)} 0");
                msg.Strings.Add($"{(int) SyndicateMember.SyndicateRank.HonoraryDeputyLeader} {HonoraryDeputyLeaderCount} {MaxHonoraryDeputyLeader} {GetSharedBattlePower(SyndicateMember.SyndicateRank.HonoraryDeputyLeader)} {HONORARY_DEPUTY_LEADER_PRICE}");
                msg.Strings.Add($"{(int) SyndicateMember.SyndicateRank.HonoraryManager} {HonoraryManagerCount} {MaxHonoraryManager} {GetSharedBattlePower(SyndicateMember.SyndicateRank.HonoraryManager)} {HONORARY_MANAGER_PRICE}");
                msg.Strings.Add($"{(int) SyndicateMember.SyndicateRank.HonorarySupervisor} {HonorarySupervisorCount} {MaxHonorarySupervisor} {GetSharedBattlePower(SyndicateMember.SyndicateRank.HonorarySupervisor)} {HONORARY_SUPERVISOR_PRICE}");
                msg.Strings.Add($"{(int) SyndicateMember.SyndicateRank.HonorarySteward} {HonoraryStewardCount} {MaxHonorarySteward} {GetSharedBattlePower(SyndicateMember.SyndicateRank.HonorarySteward)} {HONORARY_STEWARD_PRICE}");
            }

            if (IsMasterPosition(target.SyndicateRank))
            {
                SyndicateMember.SyndicateRank assistantRank = GetAssistantPosition(target.SyndicateRank);
                int assistantCount = target.SyndicateMember.AssistantIdentity != 0 ? 1 : 0;
                msg.Strings.Add($"{(int) assistantRank} {assistantCount} 1 {GetSharedBattlePower(assistantRank)} 0");
            }

            return target.SendAsync(msg);
        }

        public uint MaxPositionAmount(SyndicateMember.SyndicateRank pos)
        {
            switch (Level)
            {
                #region Level 1

                case 1:
                    {
                        switch (pos)
                        {
                            case SyndicateMember.SyndicateRank.Manager:
                            case SyndicateMember.SyndicateRank.ManagerAide:
                                return 1;
                            case SyndicateMember.SyndicateRank.Supervisor:
                            case SyndicateMember.SyndicateRank.ArsenalSupervisor:
                            case SyndicateMember.SyndicateRank.CpSupervisor:
                            case SyndicateMember.SyndicateRank.GuideSupervisor:
                            case SyndicateMember.SyndicateRank.LilySupervisor:
                            case SyndicateMember.SyndicateRank.OrchidSupervisor:
                            case SyndicateMember.SyndicateRank.PkSupervisor:
                            case SyndicateMember.SyndicateRank.SilverSupervisor:
                            case SyndicateMember.SyndicateRank.TulipSupervisor:
                            case SyndicateMember.SyndicateRank.Steward:
                                return 0;
                            case SyndicateMember.SyndicateRank.Agent:
                            case SyndicateMember.SyndicateRank.ArsenalAgent:
                            case SyndicateMember.SyndicateRank.CpAgent:
                            case SyndicateMember.SyndicateRank.GuideAgent:
                            case SyndicateMember.SyndicateRank.LilyAgent:
                            case SyndicateMember.SyndicateRank.OrchidAgent:
                            case SyndicateMember.SyndicateRank.PkAgent:
                            case SyndicateMember.SyndicateRank.SilverAgent:
                            case SyndicateMember.SyndicateRank.TulipAgent:
                            case SyndicateMember.SyndicateRank.Follower:
                            case SyndicateMember.SyndicateRank.ArsenalFollower:
                            case SyndicateMember.SyndicateRank.CpFollower:
                            case SyndicateMember.SyndicateRank.GuideFollower:
                            case SyndicateMember.SyndicateRank.LilyFollower:
                            case SyndicateMember.SyndicateRank.OrchidFollower:
                            case SyndicateMember.SyndicateRank.PkFollower:
                            case SyndicateMember.SyndicateRank.SilverFollower:
                            case SyndicateMember.SyndicateRank.TulipFollower:
                                return 1;
                            default:
                                return 0;
                        }
                    }

                #endregion

                #region Level 2

                case 2:
                    {
                        switch (pos)
                        {
                            case SyndicateMember.SyndicateRank.Manager:
                            case SyndicateMember.SyndicateRank.ManagerAide:
                                return 1;
                            case SyndicateMember.SyndicateRank.Supervisor:
                            case SyndicateMember.SyndicateRank.ArsenalSupervisor:
                            case SyndicateMember.SyndicateRank.CpSupervisor:
                            case SyndicateMember.SyndicateRank.GuideSupervisor:
                            case SyndicateMember.SyndicateRank.LilySupervisor:
                            case SyndicateMember.SyndicateRank.OrchidSupervisor:
                            case SyndicateMember.SyndicateRank.PkSupervisor:
                            case SyndicateMember.SyndicateRank.SilverSupervisor:
                            case SyndicateMember.SyndicateRank.TulipSupervisor:
                                return 0;
                            case SyndicateMember.SyndicateRank.Steward:
                                return 1;
                            case SyndicateMember.SyndicateRank.Agent:
                            case SyndicateMember.SyndicateRank.ArsenalAgent:
                            case SyndicateMember.SyndicateRank.CpAgent:
                            case SyndicateMember.SyndicateRank.GuideAgent:
                            case SyndicateMember.SyndicateRank.LilyAgent:
                            case SyndicateMember.SyndicateRank.OrchidAgent:
                            case SyndicateMember.SyndicateRank.PkAgent:
                            case SyndicateMember.SyndicateRank.SilverAgent:
                            case SyndicateMember.SyndicateRank.TulipAgent:
                            case SyndicateMember.SyndicateRank.Follower:
                            case SyndicateMember.SyndicateRank.ArsenalFollower:
                            case SyndicateMember.SyndicateRank.CpFollower:
                            case SyndicateMember.SyndicateRank.GuideFollower:
                            case SyndicateMember.SyndicateRank.LilyFollower:
                            case SyndicateMember.SyndicateRank.OrchidFollower:
                            case SyndicateMember.SyndicateRank.PkFollower:
                            case SyndicateMember.SyndicateRank.SilverFollower:
                            case SyndicateMember.SyndicateRank.TulipFollower:
                                return 1;
                            default:
                                return 0;
                        }
                    }

                #endregion

                #region Level 3

                case 3:
                    {
                        switch (pos)
                        {
                            case SyndicateMember.SyndicateRank.Manager:
                            case SyndicateMember.SyndicateRank.ManagerAide:
                                return 2;
                            case SyndicateMember.SyndicateRank.Supervisor:
                            case SyndicateMember.SyndicateRank.ArsenalSupervisor:
                            case SyndicateMember.SyndicateRank.CpSupervisor:
                            case SyndicateMember.SyndicateRank.GuideSupervisor:
                            case SyndicateMember.SyndicateRank.LilySupervisor:
                            case SyndicateMember.SyndicateRank.OrchidSupervisor:
                            case SyndicateMember.SyndicateRank.PkSupervisor:
                            case SyndicateMember.SyndicateRank.SilverSupervisor:
                            case SyndicateMember.SyndicateRank.TulipSupervisor:
                                return 0;
                            case SyndicateMember.SyndicateRank.Steward:
                                return 2;
                            case SyndicateMember.SyndicateRank.Agent:
                            case SyndicateMember.SyndicateRank.ArsenalAgent:
                            case SyndicateMember.SyndicateRank.CpAgent:
                            case SyndicateMember.SyndicateRank.GuideAgent:
                            case SyndicateMember.SyndicateRank.LilyAgent:
                            case SyndicateMember.SyndicateRank.OrchidAgent:
                            case SyndicateMember.SyndicateRank.PkAgent:
                            case SyndicateMember.SyndicateRank.SilverAgent:
                            case SyndicateMember.SyndicateRank.TulipAgent:
                            case SyndicateMember.SyndicateRank.Follower:
                            case SyndicateMember.SyndicateRank.ArsenalFollower:
                            case SyndicateMember.SyndicateRank.CpFollower:
                            case SyndicateMember.SyndicateRank.GuideFollower:
                            case SyndicateMember.SyndicateRank.LilyFollower:
                            case SyndicateMember.SyndicateRank.OrchidFollower:
                            case SyndicateMember.SyndicateRank.PkFollower:
                            case SyndicateMember.SyndicateRank.SilverFollower:
                            case SyndicateMember.SyndicateRank.TulipFollower:
                                return 1;
                            default:
                                return 0;
                        }
                    }

                #endregion

                #region Level 4

                case 4:
                    {
                        switch (pos)
                        {
                            case SyndicateMember.SyndicateRank.Manager:
                            case SyndicateMember.SyndicateRank.ManagerAide:
                                return 2;
                            case SyndicateMember.SyndicateRank.Supervisor:
                            case SyndicateMember.SyndicateRank.ArsenalSupervisor:
                            case SyndicateMember.SyndicateRank.CpSupervisor:
                            case SyndicateMember.SyndicateRank.GuideSupervisor:
                            case SyndicateMember.SyndicateRank.LilySupervisor:
                            case SyndicateMember.SyndicateRank.OrchidSupervisor:
                            case SyndicateMember.SyndicateRank.PkSupervisor:
                            case SyndicateMember.SyndicateRank.SilverSupervisor:
                            case SyndicateMember.SyndicateRank.TulipSupervisor:
                                return 1;
                            case SyndicateMember.SyndicateRank.Steward:
                                return 3;
                            case SyndicateMember.SyndicateRank.Agent:
                            case SyndicateMember.SyndicateRank.ArsenalAgent:
                            case SyndicateMember.SyndicateRank.CpAgent:
                            case SyndicateMember.SyndicateRank.GuideAgent:
                            case SyndicateMember.SyndicateRank.LilyAgent:
                            case SyndicateMember.SyndicateRank.OrchidAgent:
                            case SyndicateMember.SyndicateRank.PkAgent:
                            case SyndicateMember.SyndicateRank.SilverAgent:
                            case SyndicateMember.SyndicateRank.TulipAgent:
                            case SyndicateMember.SyndicateRank.Follower:
                            case SyndicateMember.SyndicateRank.ArsenalFollower:
                            case SyndicateMember.SyndicateRank.CpFollower:
                            case SyndicateMember.SyndicateRank.GuideFollower:
                            case SyndicateMember.SyndicateRank.LilyFollower:
                            case SyndicateMember.SyndicateRank.OrchidFollower:
                            case SyndicateMember.SyndicateRank.PkFollower:
                            case SyndicateMember.SyndicateRank.SilverFollower:
                            case SyndicateMember.SyndicateRank.TulipFollower:
                                return 1;
                            default:
                                return 0;
                        }
                    }

                #endregion

                #region Level 5

                case 5:
                    {
                        switch (pos)
                        {
                            case SyndicateMember.SyndicateRank.Manager:
                            case SyndicateMember.SyndicateRank.ManagerAide:
                                return 4;
                            case SyndicateMember.SyndicateRank.Supervisor:
                            case SyndicateMember.SyndicateRank.ArsenalSupervisor:
                            case SyndicateMember.SyndicateRank.CpSupervisor:
                            case SyndicateMember.SyndicateRank.GuideSupervisor:
                            case SyndicateMember.SyndicateRank.LilySupervisor:
                            case SyndicateMember.SyndicateRank.OrchidSupervisor:
                            case SyndicateMember.SyndicateRank.PkSupervisor:
                            case SyndicateMember.SyndicateRank.SilverSupervisor:
                            case SyndicateMember.SyndicateRank.TulipSupervisor:
                                return 1;
                            case SyndicateMember.SyndicateRank.Steward:
                                return 4;
                            case SyndicateMember.SyndicateRank.Agent:
                            case SyndicateMember.SyndicateRank.ArsenalAgent:
                            case SyndicateMember.SyndicateRank.CpAgent:
                            case SyndicateMember.SyndicateRank.GuideAgent:
                            case SyndicateMember.SyndicateRank.LilyAgent:
                            case SyndicateMember.SyndicateRank.OrchidAgent:
                            case SyndicateMember.SyndicateRank.PkAgent:
                            case SyndicateMember.SyndicateRank.SilverAgent:
                            case SyndicateMember.SyndicateRank.TulipAgent:
                            case SyndicateMember.SyndicateRank.Follower:
                            case SyndicateMember.SyndicateRank.ArsenalFollower:
                            case SyndicateMember.SyndicateRank.CpFollower:
                            case SyndicateMember.SyndicateRank.GuideFollower:
                            case SyndicateMember.SyndicateRank.LilyFollower:
                            case SyndicateMember.SyndicateRank.OrchidFollower:
                            case SyndicateMember.SyndicateRank.PkFollower:
                            case SyndicateMember.SyndicateRank.SilverFollower:
                            case SyndicateMember.SyndicateRank.TulipFollower:
                                return 1;
                            default:
                                return 0;
                        }
                    }

                #endregion

                #region Level 6

                case 6:
                    {
                        switch (pos)
                        {
                            case SyndicateMember.SyndicateRank.Manager:
                            case SyndicateMember.SyndicateRank.ManagerAide:
                                return 4;
                            case SyndicateMember.SyndicateRank.Supervisor:
                            case SyndicateMember.SyndicateRank.ArsenalSupervisor:
                            case SyndicateMember.SyndicateRank.CpSupervisor:
                            case SyndicateMember.SyndicateRank.GuideSupervisor:
                            case SyndicateMember.SyndicateRank.LilySupervisor:
                            case SyndicateMember.SyndicateRank.OrchidSupervisor:
                            case SyndicateMember.SyndicateRank.PkSupervisor:
                            case SyndicateMember.SyndicateRank.SilverSupervisor:
                            case SyndicateMember.SyndicateRank.TulipSupervisor:
                                return 1;
                            case SyndicateMember.SyndicateRank.Steward:
                                return 5;
                            case SyndicateMember.SyndicateRank.Agent:
                            case SyndicateMember.SyndicateRank.ArsenalAgent:
                            case SyndicateMember.SyndicateRank.CpAgent:
                            case SyndicateMember.SyndicateRank.GuideAgent:
                            case SyndicateMember.SyndicateRank.LilyAgent:
                            case SyndicateMember.SyndicateRank.OrchidAgent:
                            case SyndicateMember.SyndicateRank.PkAgent:
                            case SyndicateMember.SyndicateRank.SilverAgent:
                            case SyndicateMember.SyndicateRank.TulipAgent:
                            case SyndicateMember.SyndicateRank.Follower:
                            case SyndicateMember.SyndicateRank.ArsenalFollower:
                            case SyndicateMember.SyndicateRank.CpFollower:
                            case SyndicateMember.SyndicateRank.GuideFollower:
                            case SyndicateMember.SyndicateRank.LilyFollower:
                            case SyndicateMember.SyndicateRank.OrchidFollower:
                            case SyndicateMember.SyndicateRank.PkFollower:
                            case SyndicateMember.SyndicateRank.SilverFollower:
                            case SyndicateMember.SyndicateRank.TulipFollower:
                                return 1;
                            default:
                                return 0;
                        }
                    }

                #endregion

                #region Level 7

                case 7:
                    {
                        switch (pos)
                        {
                            case SyndicateMember.SyndicateRank.Manager:
                            case SyndicateMember.SyndicateRank.ManagerAide:
                                return 6;
                            case SyndicateMember.SyndicateRank.Supervisor:
                            case SyndicateMember.SyndicateRank.ArsenalSupervisor:
                            case SyndicateMember.SyndicateRank.CpSupervisor:
                            case SyndicateMember.SyndicateRank.GuideSupervisor:
                            case SyndicateMember.SyndicateRank.LilySupervisor:
                            case SyndicateMember.SyndicateRank.OrchidSupervisor:
                            case SyndicateMember.SyndicateRank.PkSupervisor:
                            case SyndicateMember.SyndicateRank.SilverSupervisor:
                            case SyndicateMember.SyndicateRank.TulipSupervisor:
                                return 1;
                            case SyndicateMember.SyndicateRank.Steward:
                                return 6;
                            case SyndicateMember.SyndicateRank.Agent:
                            case SyndicateMember.SyndicateRank.ArsenalAgent:
                            case SyndicateMember.SyndicateRank.CpAgent:
                            case SyndicateMember.SyndicateRank.GuideAgent:
                            case SyndicateMember.SyndicateRank.LilyAgent:
                            case SyndicateMember.SyndicateRank.OrchidAgent:
                            case SyndicateMember.SyndicateRank.PkAgent:
                            case SyndicateMember.SyndicateRank.SilverAgent:
                            case SyndicateMember.SyndicateRank.TulipAgent:
                            case SyndicateMember.SyndicateRank.Follower:
                            case SyndicateMember.SyndicateRank.ArsenalFollower:
                            case SyndicateMember.SyndicateRank.CpFollower:
                            case SyndicateMember.SyndicateRank.GuideFollower:
                            case SyndicateMember.SyndicateRank.LilyFollower:
                            case SyndicateMember.SyndicateRank.OrchidFollower:
                            case SyndicateMember.SyndicateRank.PkFollower:
                            case SyndicateMember.SyndicateRank.SilverFollower:
                            case SyndicateMember.SyndicateRank.TulipFollower:
                                return 1;
                            default:
                                return 0;
                        }
                    }

                #endregion

                #region Level 8

                case 8:
                    {
                        switch (pos)
                        {
                            case SyndicateMember.SyndicateRank.Manager:
                            case SyndicateMember.SyndicateRank.ManagerAide:
                                return 6;
                            case SyndicateMember.SyndicateRank.Supervisor:
                            case SyndicateMember.SyndicateRank.ArsenalSupervisor:
                            case SyndicateMember.SyndicateRank.CpSupervisor:
                            case SyndicateMember.SyndicateRank.GuideSupervisor:
                            case SyndicateMember.SyndicateRank.LilySupervisor:
                            case SyndicateMember.SyndicateRank.OrchidSupervisor:
                            case SyndicateMember.SyndicateRank.PkSupervisor:
                            case SyndicateMember.SyndicateRank.SilverSupervisor:
                            case SyndicateMember.SyndicateRank.TulipSupervisor:
                                return 2;
                            case SyndicateMember.SyndicateRank.Steward:
                                return 7;
                            case SyndicateMember.SyndicateRank.Agent:
                            case SyndicateMember.SyndicateRank.ArsenalAgent:
                            case SyndicateMember.SyndicateRank.CpAgent:
                            case SyndicateMember.SyndicateRank.GuideAgent:
                            case SyndicateMember.SyndicateRank.LilyAgent:
                            case SyndicateMember.SyndicateRank.OrchidAgent:
                            case SyndicateMember.SyndicateRank.PkAgent:
                            case SyndicateMember.SyndicateRank.SilverAgent:
                            case SyndicateMember.SyndicateRank.TulipAgent:
                            case SyndicateMember.SyndicateRank.Follower:
                            case SyndicateMember.SyndicateRank.ArsenalFollower:
                            case SyndicateMember.SyndicateRank.CpFollower:
                            case SyndicateMember.SyndicateRank.GuideFollower:
                            case SyndicateMember.SyndicateRank.LilyFollower:
                            case SyndicateMember.SyndicateRank.OrchidFollower:
                            case SyndicateMember.SyndicateRank.PkFollower:
                            case SyndicateMember.SyndicateRank.SilverFollower:
                            case SyndicateMember.SyndicateRank.TulipFollower:
                                return 1;
                            default:
                                return 0;
                        }
                    }

                #endregion

                #region Level 9

                case 9:
                    {
                        switch (pos)
                        {
                            case SyndicateMember.SyndicateRank.Manager:
                            case SyndicateMember.SyndicateRank.ManagerAide:
                                return 8;
                            case SyndicateMember.SyndicateRank.Supervisor:
                            case SyndicateMember.SyndicateRank.ArsenalSupervisor:
                            case SyndicateMember.SyndicateRank.CpSupervisor:
                            case SyndicateMember.SyndicateRank.GuideSupervisor:
                            case SyndicateMember.SyndicateRank.LilySupervisor:
                            case SyndicateMember.SyndicateRank.OrchidSupervisor:
                            case SyndicateMember.SyndicateRank.PkSupervisor:
                            case SyndicateMember.SyndicateRank.SilverSupervisor:
                            case SyndicateMember.SyndicateRank.TulipSupervisor:
                                return 2;
                            case SyndicateMember.SyndicateRank.Steward:
                                return 8;
                            case SyndicateMember.SyndicateRank.Agent:
                            case SyndicateMember.SyndicateRank.ArsenalAgent:
                            case SyndicateMember.SyndicateRank.CpAgent:
                            case SyndicateMember.SyndicateRank.GuideAgent:
                            case SyndicateMember.SyndicateRank.LilyAgent:
                            case SyndicateMember.SyndicateRank.OrchidAgent:
                            case SyndicateMember.SyndicateRank.PkAgent:
                            case SyndicateMember.SyndicateRank.SilverAgent:
                            case SyndicateMember.SyndicateRank.TulipAgent:
                            case SyndicateMember.SyndicateRank.Follower:
                            case SyndicateMember.SyndicateRank.ArsenalFollower:
                            case SyndicateMember.SyndicateRank.CpFollower:
                            case SyndicateMember.SyndicateRank.GuideFollower:
                            case SyndicateMember.SyndicateRank.LilyFollower:
                            case SyndicateMember.SyndicateRank.OrchidFollower:
                            case SyndicateMember.SyndicateRank.PkFollower:
                            case SyndicateMember.SyndicateRank.SilverFollower:
                            case SyndicateMember.SyndicateRank.TulipFollower:
                                return 1;
                            default:
                                return 0;
                        }
                    }

                #endregion

                default:
                    return 0;
            }
        }

        public static bool IsSystemDefinedPosition(SyndicateMember.SyndicateRank pos)
        {
            if (pos == SyndicateMember.SyndicateRank.Manager)
                return true;
            if (pos == SyndicateMember.SyndicateRank.Steward)
                return true;
            if (pos == SyndicateMember.SyndicateRank.DeputySteward)
                return true;
            if (pos >= SyndicateMember.SyndicateRank.Supervisor && pos <= SyndicateMember.SyndicateRank.TulipSupervisor)
                return true;
            if (pos >= SyndicateMember.SyndicateRank.Agent && pos <= SyndicateMember.SyndicateRank.TulipAgent)
                return true;
            if (pos >= SyndicateMember.SyndicateRank.Follower && pos <= SyndicateMember.SyndicateRank.TulipFollower)
                return true;
            return IsSpousePosition(pos);
        }

        public static bool IsUserSetPosition(SyndicateMember.SyndicateRank pos)
        {
            switch (pos)
            {
                case SyndicateMember.SyndicateRank.GuildLeader:
                case SyndicateMember.SyndicateRank.LeaderSpouse:
                case SyndicateMember.SyndicateRank.LeaderSpouseAide:
                case SyndicateMember.SyndicateRank.DeputyLeader:
                case SyndicateMember.SyndicateRank.HonoraryDeputyLeader:
                case SyndicateMember.SyndicateRank.HonoraryManager:
                case SyndicateMember.SyndicateRank.HonorarySteward:
                case SyndicateMember.SyndicateRank.HonorarySupervisor:
                case SyndicateMember.SyndicateRank.DeputyLeaderAide:
                case SyndicateMember.SyndicateRank.ManagerAide:
                case SyndicateMember.SyndicateRank.SupervisorAide:
                case SyndicateMember.SyndicateRank.Aide:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsHonoraryPosition(SyndicateMember.SyndicateRank pos)
        {
            switch (pos)
            {
                case SyndicateMember.SyndicateRank.HonoraryDeputyLeader:
                case SyndicateMember.SyndicateRank.HonoraryManager:
                case SyndicateMember.SyndicateRank.HonorarySteward:
                case SyndicateMember.SyndicateRank.HonorarySupervisor:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsSpousePosition(SyndicateMember.SyndicateRank pos)
        {
            switch (pos)
            {
                case SyndicateMember.SyndicateRank.DeputyLeaderSpouse:
                case SyndicateMember.SyndicateRank.LeaderSpouse:
                case SyndicateMember.SyndicateRank.ManagerSpouse:
                case SyndicateMember.SyndicateRank.StewardSpouse:
                case SyndicateMember.SyndicateRank.SupervisorSpouse:
                    return true;
                default:
                    return false;
            }
        }

        public static SyndicateMember.SyndicateRank GetSpousePosition(SyndicateMember.SyndicateRank rank)
        {
            switch (rank)
            {
                case SyndicateMember.SyndicateRank.GuildLeader: return SyndicateMember.SyndicateRank.LeaderSpouse;
                case SyndicateMember.SyndicateRank.DeputyLeader: return SyndicateMember.SyndicateRank.DeputyLeaderSpouse;
                case SyndicateMember.SyndicateRank.Manager: return SyndicateMember.SyndicateRank.ManagerSpouse;
                case SyndicateMember.SyndicateRank.Steward: return SyndicateMember.SyndicateRank.StewardSpouse;
                case SyndicateMember.SyndicateRank.Supervisor: return SyndicateMember.SyndicateRank.SupervisorSpouse;
                default: return SyndicateMember.SyndicateRank.None;
            }
        }

        public static SyndicateMember.SyndicateRank GetAssistantPosition(SyndicateMember.SyndicateRank rank)
        {
            if (!IsMasterPosition(rank))
                return SyndicateMember.SyndicateRank.None;

            switch (rank)
            {
                case SyndicateMember.SyndicateRank.GuildLeader:
                    return SyndicateMember.SyndicateRank.Aide;
                case SyndicateMember.SyndicateRank.LeaderSpouse:
                    return SyndicateMember.SyndicateRank.LeaderSpouseAide;
                case SyndicateMember.SyndicateRank.DeputyLeader:
                case SyndicateMember.SyndicateRank.HonoraryDeputyLeader:
                    return SyndicateMember.SyndicateRank.DeputyLeaderAide;
                case SyndicateMember.SyndicateRank.Manager:
                case SyndicateMember.SyndicateRank.HonoraryManager:
                    return SyndicateMember.SyndicateRank.ManagerAide;
                case SyndicateMember.SyndicateRank.Supervisor:
                case SyndicateMember.SyndicateRank.HonorarySupervisor:
                case SyndicateMember.SyndicateRank.ArsenalSupervisor:
                case SyndicateMember.SyndicateRank.CpSupervisor:
                case SyndicateMember.SyndicateRank.GuideSupervisor:
                case SyndicateMember.SyndicateRank.LilySupervisor:
                case SyndicateMember.SyndicateRank.OrchidSupervisor:
                case SyndicateMember.SyndicateRank.PkSupervisor:
                case SyndicateMember.SyndicateRank.SilverSupervisor:
                case SyndicateMember.SyndicateRank.TulipSupervisor:
                    return SyndicateMember.SyndicateRank.SupervisorAide;
            }

            return SyndicateMember.SyndicateRank.None;
        }
        
        #endregion

        #region Positions
        
        public static bool IsMasterPosition(SyndicateMember.SyndicateRank rank)
        {
            switch (rank)
            {
                case SyndicateMember.SyndicateRank.GuildLeader:
                case SyndicateMember.SyndicateRank.LeaderSpouse:
                case SyndicateMember.SyndicateRank.DeputyLeader:
                case SyndicateMember.SyndicateRank.HonoraryDeputyLeader:
                case SyndicateMember.SyndicateRank.Manager:
                case SyndicateMember.SyndicateRank.HonoraryManager:
                case SyndicateMember.SyndicateRank.Supervisor:
                case SyndicateMember.SyndicateRank.HonorarySupervisor:
                case SyndicateMember.SyndicateRank.ArsenalSupervisor:
                case SyndicateMember.SyndicateRank.CpSupervisor:
                case SyndicateMember.SyndicateRank.GuideSupervisor:
                case SyndicateMember.SyndicateRank.LilySupervisor:
                case SyndicateMember.SyndicateRank.OrchidSupervisor:
                case SyndicateMember.SyndicateRank.PkSupervisor:
                case SyndicateMember.SyndicateRank.SilverSupervisor:
                case SyndicateMember.SyndicateRank.TulipSupervisor:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsAssistantPosition(SyndicateMember.SyndicateRank rank)
        {
            switch (rank)
            {
                case SyndicateMember.SyndicateRank.Aide:
                case SyndicateMember.SyndicateRank.LeaderSpouseAide:
                case SyndicateMember.SyndicateRank.DeputyLeaderAide:
                case SyndicateMember.SyndicateRank.ManagerAide:
                case SyndicateMember.SyndicateRank.SupervisorAide:
                    return true;
                default:
                    return false;
            }
        }
        
        #endregion

        #region Alliance

        public int AlliesCount => m_dicAllies.Count;
        

        public async Task<bool> CreateAllianceAsync(Character user, Syndicate target)
        {
            if (user.SyndicateIdentity != Identity)
                return false;

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
            {
                await user.SendAsync(Language.StrSynYouNoLeader);
                return false;
            }

            if (IsAlly(target.Identity) || IsEnemy(target.Identity))
                return false;

            if (Money < SYNDICATE_ACTION_COST)
            {
                await user.SendAsync(string.Format(Language.StrSynNoMoney, SYNDICATE_ACTION_COST));
                return false;
            }

            if (AlliesCount >= MAX_ALLY)
            {
                return false;
            }

            await BaseRepository.SaveAsync(new DbSyndicateAllies
            {
                AllyIdentity = Identity,
                AllyName = Name,
                SyndicateIdentity = target.Identity,
                SyndicateName = target.Name,
                EstabilishDate = DateTime.Now
            });

            await BaseRepository.SaveAsync(new DbSyndicateAllies
            {
                SyndicateIdentity = Identity,
                SyndicateName = Name,
                AllyIdentity = target.Identity,
                AllyName = target.Name,
                EstabilishDate = DateTime.Now
            });

            await AddAllyAsync(target);
            await target.AddAllyAsync(this);

            await SendAsync(string.Format(Language.StrSynAllyAdd, user.Name, target.Name));
            await target.SendAsync(string.Format(Language.StrSynAllyAdd, target.Leader.UserName, Name));

            return true;
        }

        public async Task<bool> DisbandAllianceAsync(Character user, Syndicate target)
        {
            if (user.SyndicateIdentity != Identity)
                return false;

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
            {
                await user.SendAsync(Language.StrSynYouNoLeader);
                return false;
            }

            if (!IsAlly(target.Identity))
                return false;

            if (Money < SYNDICATE_ACTION_COST)
            {
                await user.SendAsync(string.Format(Language.StrSynNoMoney, SYNDICATE_ACTION_COST));
                return false;
            }

            await SyndicateAllyRepository.DeleteAsync(Identity, target.Identity);

            await RemoveAllyAsync(target.Identity);
            await target.RemoveAllyAsync(Identity);

            await SendAsync(string.Format(Language.StrSynAllyRemove, user.Name, target.Name));
            await target.SendAsync(string.Format(Language.StrSynAllyRemoved, user.Name, target.Name));
            return true;
        }

        public async Task AddAllyAsync(Syndicate target)
        {
            m_dicAllies.TryAdd(target.Identity, target);
            await SendAsync(new MsgSyndicate
            {
                Identity = target.Identity,
                Mode = MsgSyndicate.SyndicateRequest.Ally
            });
            await SendAsync(new MsgName
            {
                Identity = target.Identity,
                Action = StringAction.SetAlly,
                Strings = new List<string>
                {
                    $"{target.Name} {target.Leader.UserName} {target.Level} {target.MemberCount}"
                }
            });
        }

        public async Task RemoveAllyAsync(uint idAlly)
        {
            m_dicAllies.TryRemove((ushort) idAlly, out _);
            await SendAsync(new MsgSyndicate
            {
                Identity = idAlly,
                Mode = MsgSyndicate.SyndicateRequest.Unally
            });
        }

        public bool IsAlly(uint id)
        {
            return m_dicAllies.ContainsKey((ushort) id);
        }

        public ushort MaxAllies()
        {
            switch (Level)
            {
                case 1: return 5;
                case 2: return 7;
                case 3: return 9;
                case 4: return 12;
                default: return 15;
            }
        }

        #endregion

        #region Enemies

        public int EnemyCount => m_dicEnemies.Count;

        public async Task<bool> AntagonizeAsync(Character user, Syndicate target)
        {
            if (user.SyndicateIdentity != Identity)
                return false;

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
            {
                await user.SendAsync(Language.StrSynYouNoLeader);
                return false;
            }

            if (IsAlly(target.Identity) || IsEnemy(target.Identity))
                return false;

            if (Money < SYNDICATE_ACTION_COST)
            {
                await user.SendAsync(string.Format(Language.StrSynNoMoney, SYNDICATE_ACTION_COST));
                return false;
            }

            if (EnemyCount >= MAX_ENEMY)
            {
                // ? message
                return false;
            }

            if (!await BaseRepository.SaveAsync(new DbSyndicateEnemy
            {
                SyndicateIdentity = Identity,
                SyndicateName = Name,
                EnemyIdentity = target.Identity,
                EnemyName = target.Name,
                EstabilishDate = DateTime.Now
            }))
                return false;

            m_dicEnemies.TryAdd(target.Identity, target);

            await SendAsync(new MsgSyndicate
            {
                Identity = target.Identity,
                Mode = MsgSyndicate.SyndicateRequest.Enemy
            });
            await SendAsync(new MsgName
            {
                Identity = target.Identity,
                Action = StringAction.SetEnemy,
                Strings = new List<string>
                {
                    $"{target.Name} {target.Leader.UserName} {target.Level} {target.MemberCount}"
                }
            });

            await SendAsync(string.Format(Language.StrSynAddEnemy, user.Name, target.Name));
            await target.SendAsync(string.Format(Language.StrSynAddedEnemy, user.Name, Name));
            return true;
        }

        public async Task<bool> PeaceAsync(Character user, Syndicate target)
        {
            if (user.SyndicateIdentity != Identity)
                return false;

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
            {
                await user.SendAsync(Language.StrSynYouNoLeader);
                return false;
            }

            if (!IsEnemy(target.Identity))
                return false;

            if (Money < SYNDICATE_ACTION_COST)
            {
                await user.SendAsync(string.Format(Language.StrSynNoMoney, SYNDICATE_ACTION_COST));
                return false;
            }

            m_dicEnemies.TryRemove(target.Identity, out _);
            await SyndicateEnemyRepository.DeleteAsync(Identity, target.Identity);

            await SendAsync(new MsgSyndicate
            {
                Identity = target.Identity,
                Mode = MsgSyndicate.SyndicateRequest.Unenemy
            });

            await SendAsync(string.Format(Language.StrSynRemoveEnemy, user.Name, target.Name));
            await target.SendAsync(string.Format(Language.StrSynRemovedEnemy, user.Name, Name));

            return true;
        }

        public bool IsEnemy(uint id)
        {
            return m_dicEnemies.ContainsKey((ushort)id);
        }

        public ushort MaxEnemies()
        {
            switch (Level)
            {
                case 1: return 5;
                case 2: return 7;
                case 3: return 9;
                case 4: return 12;
                default: return 15;
            }
        }

        #endregion

        #region Events

        public void ExitFromEvents()
        {
            Kernel.EventThread.GetEvent<TimedGuildWar>()?.UnsubscribeSyndicate(Identity);
            Kernel.EventThread.GetEvent<GuildContest>()?.RemoveFromEvent(Identity);
        }

        public void RemoveUserFromEvents(uint idUser)
        {
            Kernel.EventThread.GetEvent<TimedGuildWar>()?.LeaveSyndicate(idUser, Identity);
            Kernel.EventThread.GetEvent<GuildContest>()?.RemoveUserFromEvent(idUser, Identity);
        }

        #endregion

        #region Requirements

        public bool AllowTrojan => (m_syndicate.ReqClass & (ulong) ProfessionPermission.Trojan) == 0;
        public bool AllowWarrior => (m_syndicate.ReqClass & (ulong) ProfessionPermission.Warrior) == 0;
        public bool AllowArcher => (m_syndicate.ReqClass & (ulong) ProfessionPermission.Archer) == 0;
        public bool AllowTaoist => (m_syndicate.ReqClass & (ulong) ProfessionPermission.Taoist) == 0;
        public bool AllowNinja => (m_syndicate.ReqClass & (ulong) ProfessionPermission.Ninja) == 0;

        public uint ProfessionRequirement
        {
            get => m_syndicate.ReqClass;
            set => m_syndicate.ReqClass = value;
        }

        public byte LevelRequirement
        {
            get => m_syndicate.ReqLevel;
            set => m_syndicate.ReqLevel = value;
        }

        public byte MetempsychosisRequirement
        {
            get => m_syndicate.ReqMetempsychosis;
            set => m_syndicate.ReqMetempsychosis = value;
        }

        #endregion

        #region Totem Pole

        public DateTime? LastOpenTotem
        {
            get => m_syndicate.LastTotem;
            set => m_syndicate.LastTotem = value;
        }

        public bool HasTotemHeadgear
        {
            get => (m_syndicate.TotemPole & (int) TotemPoleFlag.Headgear) != 0;
            set
            {
                if (value)
                    m_syndicate.TotemPole |= (int) TotemPoleFlag.Headgear;
                else
                    m_syndicate.TotemPole &= ~(int)TotemPoleFlag.Headgear;
            }
        }

        public bool HasTotemNecklace
        {
            get => (m_syndicate.TotemPole & (int)TotemPoleFlag.Necklace) != 0;
            set
            {
                if (value)
                    m_syndicate.TotemPole |= (int)TotemPoleFlag.Necklace;
                else
                    m_syndicate.TotemPole &= ~(int)TotemPoleFlag.Necklace;
            }
        }

        public bool HasTotemRing
        {
            get => (m_syndicate.TotemPole & (int)TotemPoleFlag.Ring) != 0;
            set
            {
                if (value)
                    m_syndicate.TotemPole |= (int)TotemPoleFlag.Ring;
                else
                    m_syndicate.TotemPole &= ~(int)TotemPoleFlag.Ring;
            }
        }

        public bool HasTotemWeapon
        {
            get => (m_syndicate.TotemPole & (int) TotemPoleFlag.Weapon) != 0;
            set
            {
                if (value)
                    m_syndicate.TotemPole |= (int) TotemPoleFlag.Weapon;
                else
                    m_syndicate.TotemPole &= ~(int)TotemPoleFlag.Weapon;
            }
        }

        public bool HasTotemArmor
        {
            get => (m_syndicate.TotemPole & (int)TotemPoleFlag.Armor) != 0;
            set
            {
                if (value)
                    m_syndicate.TotemPole |= (int)TotemPoleFlag.Armor;
                else
                    m_syndicate.TotemPole &= ~(int)TotemPoleFlag.Armor;
            }
        }

        public bool HasTotemBoots
        {
            get => (m_syndicate.TotemPole & (int)TotemPoleFlag.Boots) != 0;
            set
            {
                if (value)
                    m_syndicate.TotemPole |= (int)TotemPoleFlag.Boots;
                else
                    m_syndicate.TotemPole &= ~(int)TotemPoleFlag.Boots;
            }
        }

        public bool HasTotemFan
        {
            get => (m_syndicate.TotemPole & (int)TotemPoleFlag.Fan) != 0;
            set
            {
                if (value)
                    m_syndicate.TotemPole |= (int)TotemPoleFlag.Fan;
                else
                    m_syndicate.TotemPole &= ~(int)TotemPoleFlag.Fan;
            }
        }

        public bool HasTotemTower
        {
            get => (m_syndicate.TotemPole & (int)TotemPoleFlag.Tower) != 0;
            set
            {
                if (value)
                    m_syndicate.TotemPole |= (int)TotemPoleFlag.Tower;
                else
                    m_syndicate.TotemPole &= ~(int)TotemPoleFlag.Tower;
            }
        }

        public int TotemSharedBattlePower { get; private set; } = 0;

        private ConcurrentDictionary<TotemPoleType, TotemPole> m_totemPoles = new ConcurrentDictionary<TotemPoleType, TotemPole>();

        public async Task<bool> OpenTotemPoleAsync(TotemPoleType type)
        {
            if (m_totemPoles.TryGetValue(type, out var pole) && !pole.Locked)
                return false;

            if (pole == null)
            {
                pole = new TotemPole(type);
                m_totemPoles.TryAdd(type, pole);
            }

            LastOpenTotem = DateTime.Now;

            switch (type)
            {
                case TotemPoleType.Headgear: HasTotemHeadgear = true; break;
                case TotemPoleType.Necklace: HasTotemNecklace = true; break;
                case TotemPoleType.Ring: HasTotemRing = true; break;
                case TotemPoleType.Weapon: HasTotemWeapon = true; break;
                case TotemPoleType.Armor: HasTotemArmor = true; break;
                case TotemPoleType.Boots: HasTotemBoots = true; break;
                case TotemPoleType.Fan: HasTotemFan = true; break;
                case TotemPoleType.Tower: HasTotemTower = true; break;
            }

            pole.Locked = false;
            return await SaveAsync();
        }

        public async Task<bool> InscribeItemAsync(Character user, Item item)
        {
            if (user.SyndicateIdentity != Identity)
                return false;

            TotemPoleType type = GetTotemPoleType(item.Type);
            if (type == TotemPoleType.None)
                return false;

            if (!m_totemPoles.TryGetValue(type, out var pole) || pole.Locked)
                return false;

            if (user.UserPackage[item.Identity] == null)
                return false;

            if (item.GetQuality() < 8 || item.IsArrowSort())
                return false;

            if (pole.Totems.ContainsKey(item.Identity))
                return false;

            int limit = TotemLimit(user.Level, user.Metempsychosis);
            if (pole.Totems.Values.Count(x => x.PlayerIdentity == user.Identity) >= limit)
            {
                await user.SendAsync(string.Format(Language.StrTotemPoleLimit, limit));
                return false;
            }

            var totem = new Totem(user, item);
            if (!pole.Totems.TryAdd(totem.ItemIdentity, totem))
                return false;

            item.SyndicateIdentity = Identity;
            await item.SaveAsync();

            int battlePower = TotemSharedBattlePower;
            UpdateBattlePower();

            if (battlePower != TotemSharedBattlePower)
                await SynchronizeBattlePowerAsync();

            await RefreshMemberArsenalDonationAsync(user.SyndicateMember);
            await user.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
            await SendTotemPolesAsync(user);

            await Log.GmLogAsync("arsenal_inscribe", $"{user.Identity},{item.Identity},{item.Type},{Identity}");
            return true;
        }

        public async Task<bool> UnsubscribeItemAsync(uint idItem, uint idUser, bool synchro = true, bool isSystem = false)
        {
            Character user = Kernel.RoleManager.GetUser(idUser);
            if (user == null && !isSystem)
                return false;

            m_dicMembers.TryGetValue(idUser, out var member);

            DbItem dbItem = null;
            Item item = null;
            uint idType;
            if (user == null)
            {
                dbItem = await ItemRepository.GetByIdAsync(idItem);
                if (dbItem == null)
                    return false;

                idType = dbItem.Type;
            }
            else
            {
                item = user.UserPackage.FindByIdentityAnywhere(idItem);
                if (item == null)
                    return false;

                idType = item.Type;
            }

            TotemPoleType type = GetTotemPoleType(idType);
            if (type == TotemPoleType.None)
                return false;

            if (!m_totemPoles.TryGetValue(type, out var pole) || pole.Locked)
                return false;

            if (!pole.Totems.TryGetValue(idItem, out var totem) || totem.PlayerIdentity != idUser)
                return false;

            pole.Totems.TryRemove(idItem, out _);

            if (item != null)
            {
                item.SyndicateIdentity = 0;
                await item.SaveAsync();
            }
            else
            {
                dbItem.Syndicate = 0;
                await BaseRepository.SaveAsync(dbItem);
            }

            if (synchro)
            {
                int battlePower = TotemSharedBattlePower;
                UpdateBattlePower();

                if (battlePower != TotemSharedBattlePower)
                    await SynchronizeBattlePowerAsync();

                if (user != null)
                {
                    if (member != null)
                        await RefreshMemberArsenalDonationAsync(member);

                    await user.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
                    await SendTotemPolesAsync(user);
                }
            }

            await Log.GmLogAsync("arsenal_unsubscribe", $"{idUser},{idItem},{idType},{Identity}");
            return true;
        }

        public async Task UnsubscribeAllAsync(uint idUser, bool isSystem = false)
        {
            List<Totem> totems = new List<Totem>();
            for (TotemPoleType type = TotemPoleType.Headgear; type < TotemPoleType.None; type++)
            {
                if (m_totemPoles.TryGetValue(type, out var pole))
                {
                    totems.AddRange(pole.Totems.Values.Where(x => x.PlayerIdentity == idUser));
                }
            }

            int battlePower = TotemSharedBattlePower;

            foreach (var totem in totems)
                await UnsubscribeItemAsync(totem.ItemIdentity, totem.PlayerIdentity, false, isSystem);

            UpdateBattlePower();
            if (battlePower != TotemSharedBattlePower)
                await SynchronizeBattlePowerAsync();
        }

        public async Task RefreshMemberArsenalDonationAsync(SyndicateMember member)
        {
            member.ArsenalDonation = 0;
            foreach (var pole in m_totemPoles.Values.Where(x => !x.Locked))
            {
                member.ArsenalDonation += (uint)pole.Totems.Values.Where(x => x.PlayerIdentity == member.UserIdentity).Sum(x => x.Points);
            }
            await member.SaveAsync();
        }

        public async Task<bool> EnhanceTotemPoleAsync(TotemPoleType type, byte power)
        {
            if (!m_totemPoles.TryGetValue(type, out var pole))
                return false;

            if (pole.Enhancement >= power)
                return false;

            int cost;
            switch (power)
            {
                case 1:
                    cost = 60000000;
                    break;
                case 2:
                    cost = 100000000;
                    break;
                default:
                    return false;
            }

            if (Money < cost)
                return false;

            if (pole.Enhancement > 0)
                await pole.RemoveEnhancementAsync();

            if (!await pole.SetEnhancementAsync(new DbTotemAdd
            {
                OwnerIdentity = Identity,
                BattleAddition = power,
                TimeLimit = DateTime.Now.AddDays(30),
                TotemType = (uint) type
            }))
            {
                return false;
            }

            UpdateBattlePower();

            Money -= cost;

            await Log.GmLogAsync("arsenal_enhance", $"{Identity},{type},{power},{cost}");
            return await SaveAsync();
        }

        public int GetSharedBattlePower(SyndicateMember.SyndicateRank rank)
        {
            // todo maybe the real calculation from the version???
            return TotemSharedBattlePower;
        }

        public Task SendTotemPolesAsync(Character user)
        {
            MsgTotemPoleInfo msg = new MsgTotemPoleInfo
            {
                TotemBattlePower = TotemSharedBattlePower,
                SharedBattlePower = GetSharedBattlePower(user.SyndicateRank),
                TotemDonation = (int) user.SyndicateMember.ArsenalDonation
            };
            for (TotemPoleType type = TotemPoleType.Headgear; type < TotemPoleType.None; type++)
            {
                if (m_totemPoles.TryGetValue(type, out var pole))
                {
                    msg.Items.Add(new MsgTotemPoleInfo.TotemPoleStruct
                    {
                        BattlePower = pole.BattlePower,
                        Enhancement = pole.Enhancement,
                        Donation = pole.Donation,
                        Open = !pole.Locked,
                        Type = type
                    });
                }
                else
                {
                    msg.Items.Add(new MsgTotemPoleInfo.TotemPoleStruct
                    {
                        Type = type
                    });
                }
            }
            return user.SendAsync(msg);
        }

        public Task SendTotemsAsync(Character user, TotemPoleType type, int index)
        {
            if (!m_totemPoles.TryGetValue(type, out var pole))
                return Task.CompletedTask;

            MsgWeaponsInfo msg = new MsgWeaponsInfo
            {
                Data1 = index - 1,
                Donation = pole.GetUserContribution(user.Identity),
                Enhancement = pole.Enhancement,
                EnhancementExpiration = uint.Parse(pole.EnhancementExpiration?.ToString("yyyyMMdd") ?? "0"),
                SharedBattlePower = pole.BattlePower,
                TotalInscribed = pole.Totems.Count,
                TotemType = type
            };

            int position = index;
            foreach (var item in pole.Totems.Values.OrderByDescending(x => x.Points)
                .Skip(index - 1)
                .Take(8))
            {
                msg.Items.Add(new MsgWeaponsInfo.TotemPoleStruct
                {
                    Donation = (int) item.Points,
                    Type = item.ItemType,
                    BattlePower = 0,
                    Position = position++,
                    SocketOne = item.SocketOne,
                    SocketTwo = item.SocketTwo,
                    Addition = item.Addition,
                    ItemIdentity = item.ItemIdentity,
                    PlayerName = item.PlayerName,
                    Quality = item.Quality
                });
            }

            msg.Data2 += msg.Data1 - 1;
            return user.SendAsync(msg);
        }

        private void UpdateBattlePower()
        {
            TotemSharedBattlePower = m_totemPoles.Values
                .Where(x => !x.Locked)
                .OrderByDescending(x => x.SharedBattlePower)
                .Take(5)
                .Sum(x => x.SharedBattlePower);
        }

        private async Task SynchronizeBattlePowerAsync()
        {
            foreach (var member in m_dicMembers.Values.Where(x => x.IsOnline))
            {
                await member.User.BroadcastRoomMsgAsync(
                    new MsgUserAttrib(member.UserIdentity, ClientUpdateType.TotemPoleBattlePower,
                        (ulong) TotemSharedBattlePower), true);
            }
        }

        public static int TotemLimit(int level, int metempsychosis)
        {
            if (metempsychosis == 0)
            {
                if (level < 100)
                    return 7;
                return 14;
            }
            if (metempsychosis == 1)
                return 21;
            return 30;
        }

        public static TotemPoleType GetTotemPoleType(uint type)
        {
            if (Item.IsHelmet(type))
                return TotemPoleType.Headgear;
            if (Item.IsArmor(type) && type / 1000 != 137)
                return TotemPoleType.Armor;
            if (Item.IsWeapon(type) || Item.IsShield(type))
                return TotemPoleType.Weapon;
            if (Item.IsRing(type) || Item.IsBangle(type))
                return TotemPoleType.Ring;
            if (Item.IsShoes(type))
                return TotemPoleType.Boots;
            if (Item.IsNeck(type))
                return TotemPoleType.Necklace;
            if (Item.IsAttackTalisman(type))
                return TotemPoleType.Fan;
            if (Item.IsDefenseTalisman(type))
                return TotemPoleType.Tower;
            return TotemPoleType.None;
        }

        public int UnlockTotemPolePrice()
        {
            switch (m_totemPoles.Count(x => !x.Value.Locked))
            {
                case 0:
                case 1: return 5000000;
                case 2:
                case 3:
                case 4: return 10000000;
                case 5:
                case 6: return 15000000;
                case 7: return 20000000;
            }
            return 0;
        }

        #endregion

        #region Query

        public SyndicateMember QueryMember(uint id)
        {
            return m_dicMembers.TryGetValue(id, out var member) ? member : null;
        }

        public SyndicateMember QueryMember(string member)
        {
            return m_dicMembers.Values.FirstOrDefault(x => x.UserName == member);
        }

        public List<SyndicateMember> QueryRank(MsgFactionRankInfo.RankRequestType type)
        {
            switch (type)
            {
                case MsgFactionRankInfo.RankRequestType.Silvers:
                    return m_dicMembers.Values.Where(x => x.Silvers > 0).OrderByDescending(x => x.Silvers).ToList();
                case MsgFactionRankInfo.RankRequestType.ConquerPoints:
                    return m_dicMembers.Values.Where(x => x.ConquerPointsDonation > 0).OrderByDescending(x => x.ConquerPointsDonation).ToList();
                case MsgFactionRankInfo.RankRequestType.Guide:
                    return m_dicMembers.Values.Where(x => x.GuideDonation > 0).OrderByDescending(x => x.GuideDonation).ToList();
                case MsgFactionRankInfo.RankRequestType.PK:
                    return m_dicMembers.Values.Where(x => x.PkDonation > 0).OrderByDescending(x => x.PkDonation).ToList();
                case MsgFactionRankInfo.RankRequestType.Arsenal:
                    return m_dicMembers.Values.Where(x => x.ArsenalDonation > 0).OrderByDescending(x => x.ArsenalDonation).ToList();
                case MsgFactionRankInfo.RankRequestType.RedRose:
                    return m_dicMembers.Values.Where(x => x.RedRoseDonation > 0).OrderByDescending(x => x.RedRoseDonation).ToList();
                case MsgFactionRankInfo.RankRequestType.WhiteRose:
                    return m_dicMembers.Values.Where(x => x.WhiteRoseDonation > 0).OrderByDescending(x => x.WhiteRoseDonation).ToList();
                case MsgFactionRankInfo.RankRequestType.Orchid:
                    return m_dicMembers.Values.Where(x => x.OrchidDonation > 0).OrderByDescending(x => x.OrchidDonation).ToList();
                case MsgFactionRankInfo.RankRequestType.Tulip:
                    return m_dicMembers.Values.Where(x => x.TulipDonation > 0).OrderByDescending(x => x.TulipDonation).ToList();
                case MsgFactionRankInfo.RankRequestType.Total:
                    return m_dicMembers.Values.Where(x => x.TotalDonation > 0).OrderByDescending(x => x.TotalDonation).ToList();
                case MsgFactionRankInfo.RankRequestType.Usable:
                    return m_dicMembers.Values.Where(x => x.TotalDonation > 0).OrderByDescending(x => x.UsableDonation).ToList();
            }

            return new List<SyndicateMember>();
        }

        #endregion

        #region Socket

        public async Task SendMembersAsync(int page, Character target)
        {
            const int MAX_PER_PAGE_I = 12;
            int startAt = page; // just in case I need to change the value in runtime
            int current = 0;

            MsgSynMemberList msg = new MsgSynMemberList
            {
                Index = page
            };

            foreach (var member in m_dicMembers.Values
                .OrderByDescending(x => x.IsOnline)
                .ThenByDescending(x => x.Rank)
                .ThenByDescending(x => x.Level))
            {
                if (current - startAt >= MAX_PER_PAGE_I)
                    break;
                if (current++ < startAt)
                    continue;

                msg.Members.Add(new MsgSynMemberList.MemberStruct
                {
                    Identity = member.UserIdentity,
                    Name = member.UserName,
                    Rank = member.Rank,
                    Nobility = member.NobilityRank,
                    PositionExpire = uint.Parse(member.PositionExpiration?.ToString("yyyyMMdd") ?? "0"),
                    IsOnline = member.IsOnline,
                    LookFace = member.LookFace,
                    TotalDonation = member.TotalDonation,
                    Level = member.Level
                });
            }

            await target.SendAsync(msg);
        }

        private static SyndicateMember.SyndicateRank[] ShowMinContriRank =
        {
            SyndicateMember.SyndicateRank.Manager,
            SyndicateMember.SyndicateRank.Supervisor,
            SyndicateMember.SyndicateRank.SilverSupervisor,
            SyndicateMember.SyndicateRank.CpSupervisor,
            SyndicateMember.SyndicateRank.PkSupervisor,
            SyndicateMember.SyndicateRank.GuideSupervisor,
            SyndicateMember.SyndicateRank.ArsenalSupervisor,
            SyndicateMember.SyndicateRank.RoseSupervisor,
            SyndicateMember.SyndicateRank.LilySupervisor,
            SyndicateMember.SyndicateRank.OrchidSupervisor,
            SyndicateMember.SyndicateRank.TulipSupervisor,
            SyndicateMember.SyndicateRank.Steward,
            SyndicateMember.SyndicateRank.DeputySteward,
            SyndicateMember.SyndicateRank.TulipAgent,
            SyndicateMember.SyndicateRank.OrchidAgent,
            SyndicateMember.SyndicateRank.CpAgent,
            SyndicateMember.SyndicateRank.ArsenalAgent,
            SyndicateMember.SyndicateRank.SilverAgent,
            SyndicateMember.SyndicateRank.GuideAgent,
            SyndicateMember.SyndicateRank.PkAgent,
            SyndicateMember.SyndicateRank.RoseAgent,
            SyndicateMember.SyndicateRank.LilyAgent,
            SyndicateMember.SyndicateRank.Agent,
            SyndicateMember.SyndicateRank.TulipFollower,
            SyndicateMember.SyndicateRank.OrchidFollower,
            SyndicateMember.SyndicateRank.CpFollower,
            SyndicateMember.SyndicateRank.ArsenalFollower,
            SyndicateMember.SyndicateRank.SilverFollower,
            SyndicateMember.SyndicateRank.GuideFollower,
            SyndicateMember.SyndicateRank.PkFollower,
            SyndicateMember.SyndicateRank.RoseFollower,
            SyndicateMember.SyndicateRank.LilyFollower,
            SyndicateMember.SyndicateRank.Follower,
            SyndicateMember.SyndicateRank.SeniorMember
        };

        public Task SendMinContributionAsync(Character target)
        {
            if (target.SyndicateIdentity != Identity)
                return Task.CompletedTask;

            MsgDutyMinContri msg = new MsgDutyMinContri();
            foreach (var pos in ShowMinContriRank)
            {
                uint min = 0;
                int current = m_dicMembers.Values.Count(x => x.Rank == pos);
                int maxPerPos = (int) MaxPositionAmount(pos);

                switch (pos)
                {
                    case SyndicateMember.SyndicateRank.Manager:
                    case SyndicateMember.SyndicateRank.Steward:
                    case SyndicateMember.SyndicateRank.Supervisor:
                    case SyndicateMember.SyndicateRank.Agent:
                    case SyndicateMember.SyndicateRank.Follower:
                    {
                        if (current >= maxPerPos)
                        {
                            min = (uint) m_dicMembers.Values.Where(x => x.Rank == pos).Select(x => x.UsableDonation)
                                .DefaultIfEmpty().Min(x => x);
                        }
                        else
                        {
                            min = (uint) m_dicMembers.Values.Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                                .Select(x => x.UsableDonation).DefaultIfEmpty().Max(x => x);
                        }

                        break;
                    }

                    case SyndicateMember.SyndicateRank.RoseSupervisor:
                    case SyndicateMember.SyndicateRank.RoseAgent:
                    case SyndicateMember.SyndicateRank.RoseFollower:
                    {
                        if (current >= maxPerPos)
                        {
                            min = m_dicMembers.Values.Where(x => x.Rank == pos).Select(x => x.RedRoseDonation)
                                .DefaultIfEmpty().Min(x => x);
                        }
                        else
                        {
                            min = m_dicMembers.Values.Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                                .Select(x => x.RedRoseDonation).DefaultIfEmpty().Max(x => x);
                        }

                        break;
                    }

                    case SyndicateMember.SyndicateRank.LilySupervisor:
                    case SyndicateMember.SyndicateRank.LilyAgent:
                    case SyndicateMember.SyndicateRank.LilyFollower:
                    {
                        if (current >= maxPerPos)
                        {
                            min = m_dicMembers.Values.Where(x => x.Rank == pos).Select(x => x.WhiteRoseDonation)
                                .DefaultIfEmpty().Min(x => x);
                        }
                        else
                        {
                            min = m_dicMembers.Values.Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                                .Select(x => x.WhiteRoseDonation).DefaultIfEmpty().Max(x => x);
                        }

                        break;
                    }

                    case SyndicateMember.SyndicateRank.OrchidAgent:
                    case SyndicateMember.SyndicateRank.OrchidFollower:
                    case SyndicateMember.SyndicateRank.OrchidSupervisor:
                    {
                        if (current >= maxPerPos)
                        {
                            min = m_dicMembers.Values.Where(x => x.Rank == pos).Select(x => x.OrchidDonation)
                                .DefaultIfEmpty().Min(x => x);
                        }
                        else
                        {
                            min = m_dicMembers.Values.Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                                .Select(x => x.OrchidDonation).DefaultIfEmpty().Max(x => x);
                        }

                        break;
                    }

                    case SyndicateMember.SyndicateRank.TulipSupervisor:
                    case SyndicateMember.SyndicateRank.TulipAgent:
                    case SyndicateMember.SyndicateRank.TulipFollower:
                    {
                        if (current >= maxPerPos)
                        {
                            min = m_dicMembers.Values.Where(x => x.Rank == pos).Select(x => x.TulipDonation)
                                .DefaultIfEmpty().Min(x => x);
                        }
                        else
                        {
                            min = m_dicMembers.Values.Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                                .Select(x => x.TulipDonation).DefaultIfEmpty().Max(x => x);
                        }

                        break;
                    }

                    case SyndicateMember.SyndicateRank.PkSupervisor:
                    case SyndicateMember.SyndicateRank.PkAgent:
                    case SyndicateMember.SyndicateRank.PkFollower:
                    {
                        if (current >= maxPerPos)
                        {
                            min = (uint) m_dicMembers.Values.Where(x => x.Rank == pos).Select(x => x.PkDonation)
                                .DefaultIfEmpty().Min(x => x);
                        }
                        else
                        {
                            min = (uint) m_dicMembers.Values.Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                                .Select(x => x.PkDonation).DefaultIfEmpty().Max(x => x);
                        }

                        break;
                    }

                    case SyndicateMember.SyndicateRank.GuideSupervisor:
                    case SyndicateMember.SyndicateRank.GuideAgent:
                    case SyndicateMember.SyndicateRank.GuideFollower:
                    {
                        if (current >= maxPerPos)
                        {
                            min = m_dicMembers.Values.Where(x => x.Rank == pos).Select(x => x.GuideDonation)
                                .DefaultIfEmpty().Min(x => x);
                        }
                        else
                        {
                            min = m_dicMembers.Values.Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                                .Select(x => x.GuideDonation).DefaultIfEmpty().Max(x => x);
                        }

                        break;
                    }

                    case SyndicateMember.SyndicateRank.SilverSupervisor:
                    case SyndicateMember.SyndicateRank.SilverFollower:
                    case SyndicateMember.SyndicateRank.SilverAgent:
                    {
                        if (current >= maxPerPos)
                        {
                            min = (uint) m_dicMembers.Values.Where(x => x.Rank == pos).Select(x => x.Silvers)
                                .DefaultIfEmpty().Min(x => x);
                        }
                        else
                        {
                            min = (uint) m_dicMembers.Values.Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                                .Select(x => x.Silvers).DefaultIfEmpty().Max(x => x);
                        }

                        break;
                    }

                    case SyndicateMember.SyndicateRank.CpSupervisor:
                    case SyndicateMember.SyndicateRank.CpAgent:
                    case SyndicateMember.SyndicateRank.CpFollower:
                    {
                        if (current >= maxPerPos)
                        {
                            min = m_dicMembers.Values.Where(x => x.Rank == pos).Select(x => x.ConquerPointsDonation)
                                .DefaultIfEmpty().Min(x => x);
                        }
                        else
                        {
                            min = m_dicMembers.Values.Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                                .Select(x => x.ConquerPointsDonation).DefaultIfEmpty().Max(x => x);
                        }

                        break;
                    }

                    case SyndicateMember.SyndicateRank.ArsenalSupervisor:
                    case SyndicateMember.SyndicateRank.ArsenalAgent:
                    case SyndicateMember.SyndicateRank.ArsenalFollower:
                    {
                        if (current >= maxPerPos)
                        {
                            min = m_dicMembers.Values.Where(x => x.Rank == pos).Select(x => x.ArsenalDonation)
                                .DefaultIfEmpty().Min(x => x);
                        }
                        else
                        {
                            min = m_dicMembers.Values.Where(x => x.Rank == SyndicateMember.SyndicateRank.Member)
                                .Select(x => x.ArsenalDonation).DefaultIfEmpty().Max(x => x);
                        }

                        break;
                    }

                    case SyndicateMember.SyndicateRank.DeputySteward:
                    {
                        min = 175000;
                        break;
                    }

                    case SyndicateMember.SyndicateRank.SeniorMember:
                    {
                        min = 25000;
                        break;
                    }
                }

                msg.Members.Add(new MsgDutyMinContri.MinContriStruct
                {
                    Position = pos,
                    Donation = min
                });
            }

            return target.SendAsync(msg);
        }

        public async Task SendRelationAsync(Character target)
        {
            foreach (var ally in m_dicAllies.Values)
            {
                await target.SendAsync(new MsgName
                {
                    Identity = ally.Identity,
                    Action = StringAction.SetAlly,
                    Strings = new List<string>
                    {
                        $"{ally.Name} {ally.Leader.UserName} {ally.Level} {ally.MemberCount}"
                    }
                });
            }

            foreach (var enemy in m_dicEnemies.Values)
            {
                await target.SendAsync(new MsgName
                {
                    Identity = enemy.Identity,
                    Action = StringAction.SetEnemy,
                    Strings = new List<string>
                    {
                        $"{enemy.Name} {enemy.Leader.UserName} {enemy.Level} {enemy.MemberCount}"
                    }
                });
            }
        }

        public async Task BroadcastNameAsync()
        {
            await Kernel.RoleManager.BroadcastMsgAsync(new MsgName
            {
                Action = StringAction.Guild,
                Identity = Identity,
                Strings = new List<string>
                {
                    Name
                }
            });
        }

        public async Task SendAsync(Character user)
        {
            await user.SendAsync(new MsgName
            {
                Action = StringAction.Guild,
                Identity = Identity,
                Strings = new List<string>
                {
                    Name
                }
            });
        }

        public async Task SendAsync(string message, uint idIgnore = 0u, Color? color = null)
        {
            await SendAsync(new MsgTalk(0, MsgTalk.TalkChannel.Guild, color ?? Color.White, message), idIgnore);
        }

        public async Task SendAsync(IPacket msg, uint exclude = 0u)
        {
            foreach (var player in m_dicMembers.Values)
            {
                if (exclude == player.UserIdentity || !player.IsOnline)
                    continue;
                await player.User.SendAsync(msg);
            }
        }

        #endregion

        #region Database

        public async Task<bool> SaveAsync()
        {
            return await BaseRepository.SaveAsync(m_syndicate);
        }

        public async Task<bool> SoftDeleteAsync()
        {
            m_syndicate.Amount = 0;
            m_syndicate.DelFlag = DateTime.Now;
            return await SaveAsync();
        }

        public async Task<bool> DeleteAsync()
        {
            return await BaseRepository.DeleteAsync(m_syndicate);
        }

        #endregion

        public enum JoinMode
        {
            Invite,
            Request,
            Recruitment
        }

        [Flags]
        public enum TotemPoleFlag
        {
            Headgear = 0x1,
            Armor = 0x2,
            Weapon = 0x4,
            Ring = 0x8,
            Boots = 0x10,
            Necklace = 0x20,
            Fan = 0x40,
            Tower = 0x80,
            None = 0x100
        }

        public enum TotemPoleType
        {
            Headgear,
            Armor,
            Weapon,
            Ring,
            Boots,
            Necklace,
            Fan,
            Tower,
            None
        }

        [Flags]
        public enum ProfessionPermission
        {
            Trojan = 1,
            Warrior = 2,
            Taoist = 4,
            Archer = 8,
            Ninja = 16,
            Monk = 32,

            All = Trojan | Warrior | Taoist | Archer | Ninja | Monk
        }
    }
}