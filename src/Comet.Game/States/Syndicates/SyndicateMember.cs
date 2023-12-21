// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - SyndicateMember.cs
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
using System.Threading.Tasks;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets;
using Comet.Shared;

namespace Comet.Game.States.Syndicates
{
    public sealed class SyndicateMember
    {
        private DbSyndicateAttr m_attr;

        public uint UserIdentity => m_attr.UserIdentity;
        public uint LookFace { get; private set; }
        public string UserName { get; private set; }
        public uint MateIdentity { get; private set; }
        public NobilityRank NobilityRank => Kernel.PeerageManager.GetRanking(UserIdentity);

        public uint SyndicateIdentity => m_attr.SynId;

        public int Silvers
        {
            get => (int) m_attr.Proffer;
            set => m_attr.Proffer = value;
        }

        public ulong SilversTotal
        {
            get => m_attr.ProfferTotal;
            set => m_attr.ProfferTotal = value;
        }

        public string SyndicateName { get; private set; }

        public byte Level { get; private set; }

        public SyndicateRank Rank
        {
            get => (SyndicateRank) m_attr.Rank;
            set => m_attr.Rank = (ushort) value;
        }

        public string RankName
        {
            get
            {
                switch (Rank)
                {
                    case SyndicateRank.GuildLeader:
                        return "Guild Leader";
                    case SyndicateRank.LeaderSpouse:
                        return "Leader Spouse";
                    case SyndicateRank.LeaderSpouseAide:
                        return "Leader Spouse Aide";
                    case SyndicateRank.DeputyLeader:
                        return "Deputy Leader";
                    case SyndicateRank.DeputyLeaderAide:
                        return "Deputy Leader Aide";
                    case SyndicateRank.DeputyLeaderSpouse:
                        return "Deputy Leader Spouse";
                    case SyndicateRank.HonoraryDeputyLeader:
                        return "Honorary Deputy Leader";
                    case SyndicateRank.Manager:
                        return "Manager";
                    case SyndicateRank.ManagerAide:
                        return "Manager Aide";
                    case SyndicateRank.ManagerSpouse:
                        return "Manager Spouse";
                    case SyndicateRank.HonoraryManager:
                        return "Honorary Manager";
                    case SyndicateRank.Supervisor:
                        return "Supervisor";
                    case SyndicateRank.SupervisorAide:
                        return "Supervisor Aide";
                    case SyndicateRank.SupervisorSpouse:
                        return "Supervisor Spouse";
                    case SyndicateRank.TulipSupervisor:
                        return "Tulip Supervisor";
                    case SyndicateRank.ArsenalSupervisor:
                        return "Arsenal Supervisor";
                    case SyndicateRank.CpSupervisor:
                        return "CP Supervisor";
                    case SyndicateRank.GuideSupervisor:
                        return "Guide Supervisor";
                    case SyndicateRank.LilySupervisor:
                        return "Lily Supervisor";
                    case SyndicateRank.OrchidSupervisor:
                        return "Orchid Supervisor";
                    case SyndicateRank.SilverSupervisor:
                        return "Silver Supervisor";
                    case SyndicateRank.RoseSupervisor:
                        return "Rose Supervisor";
                    case SyndicateRank.PkSupervisor:
                        return "PK Supervisor";
                    case SyndicateRank.HonorarySupervisor:
                        return "Honorary Supervisor";
                    case SyndicateRank.Steward:
                        return "Steward";
                    case SyndicateRank.StewardSpouse:
                        return "Steward Spouse";
                    case SyndicateRank.DeputySteward:
                        return "Deputy Steward";
                    case SyndicateRank.HonorarySteward:
                        return "Honorary Steward";
                    case SyndicateRank.Aide:
                        return "Aide";
                    case SyndicateRank.TulipAgent:
                        return "Tulip Agent";
                    case SyndicateRank.OrchidAgent:
                        return "Orchid Agent";
                    case SyndicateRank.CpAgent:
                        return "CP Agent";
                    case SyndicateRank.ArsenalAgent:
                        return "Arsenal Agent";
                    case SyndicateRank.SilverAgent:
                        return "Silver Agent";
                    case SyndicateRank.GuideAgent:
                        return "Guide Agent";
                    case SyndicateRank.PkAgent:
                        return "PK Agent";
                    case SyndicateRank.RoseAgent:
                        return "Rose Agent";
                    case SyndicateRank.LilyAgent:
                        return "Lily Agent";
                    case SyndicateRank.Agent:
                        return "Agent Follower";
                    case SyndicateRank.TulipFollower:
                        return "Tulip Follower";
                    case SyndicateRank.OrchidFollower:
                        return "Orchid Follower";
                    case SyndicateRank.CpFollower:
                        return "CP Follower";
                    case SyndicateRank.ArsenalFollower:
                        return "Arsenal Follower";
                    case SyndicateRank.SilverFollower:
                        return "Silver Follower";
                    case SyndicateRank.GuideFollower:
                        return "Guide Follower";
                    case SyndicateRank.PkFollower:
                        return "PK Follower";
                    case SyndicateRank.RoseFollower:
                        return "Rose Follower";
                    case SyndicateRank.LilyFollower:
                        return "Lily Follower";
                    case SyndicateRank.Follower:
                        return "Follower";
                    case SyndicateRank.SeniorMember:
                        return "Member";
                    case SyndicateRank.Member:
                        return "Member";
                    default:
                        return "Error";
                }
            }
        }

        public DateTime JoinDate => m_attr.JoinDate;

        public Character User => Kernel.RoleManager.GetUser(UserIdentity);
        public bool IsOnline => User != null;

        public uint ConquerPointsDonation
        {
            get => m_attr.Emoney;
            set => m_attr.Emoney = value;
        }

        public uint ConquerPointsTotalDonation
        {
            get => m_attr.EmoneyTotal;
            set => m_attr.EmoneyTotal = value;
        }

        public uint GuideDonation
        {
            get => m_attr.Guide;
            set => m_attr.Guide = value;
        }

        public uint GuideTotalDonation
        {
            get => m_attr.GuideTotal;
            set => m_attr.GuideTotal = value;
        }

        public int PkDonation
        {
            get => m_attr.Pk;
            set => m_attr.Pk = value;
        }

        public int PkTotalDonation
        {
            get => m_attr.PkTotal;
            set => m_attr.PkTotal = value;
        }

        public uint ArsenalDonation
        {
            get => m_attr.Arsenal;
            set => m_attr.Arsenal = value;
        }

        public uint RedRoseDonation
        {
            get => m_attr.Flower;
            set => m_attr.Flower = value;
        }

        public uint WhiteRoseDonation
        {
            get => m_attr.WhiteFlower;
            set => m_attr.WhiteFlower = value;
        }

        public uint OrchidDonation
        {
            get => m_attr.Orchid;
            set => m_attr.Orchid = value;
        }

        public uint TulipDonation
        {
            get => m_attr.Tulip;
            set => m_attr.Tulip = value;
        }

        public uint Merit
        {
            get => m_attr.Merit;
            set => m_attr.Merit = value;
        }

        public DateTime? LastLogout
        {
            get => m_attr.LastLogout;
            set => m_attr.LastLogout = value;
        }

        public DateTime? PositionExpiration
        {
            get => m_attr.Expiration;
            set => m_attr.Expiration = value;
        }

        public uint MasterIdentity
        {
            get => m_attr.MasterId;
            set => m_attr.MasterId = value;
        }

        public uint AssistantIdentity
        {
            get => m_attr.AssistantIdentity;
            set => m_attr.AssistantIdentity = value;
        }

        public int TotalDonation => (int) ((int) (Silvers / 10000) + ConquerPointsDonation * 20 + GuideDonation + PkDonation +
                                           ArsenalDonation + RedRoseDonation + WhiteRoseDonation + OrchidDonation +
                                           TulipDonation);

        public int UsableDonation => (int)(Silvers / 10000 + ConquerPointsDonation * 20 + GuideDonation + PkDonation +
                                           ArsenalDonation + RedRoseDonation + WhiteRoseDonation + OrchidDonation +
                                           TulipDonation);

        public async Task<bool> CreateAsync(DbSyndicateAttr attr, Syndicate syn)
        {
            if (attr == null || syn == null || m_attr != null)
                return false;

            m_attr = attr;

            DbCharacter user = await CharactersRepository.FindByIdentityAsync(attr.UserIdentity);
            if (user == null)
                return false;

            UserName = user.Name;
            LookFace = user.Mesh;
            MateIdentity = user.Mate;
            SyndicateName = syn.Name;
            Level = user.Level;
            return true;
        }

        public async Task<bool> CreateAsync(Character user, Syndicate syn, SyndicateRank rank)
        {
            if (user == null || syn == null || m_attr != null)
                return false;

            m_attr = new DbSyndicateAttr
            {
                UserIdentity = user.Identity,
                SynId = syn.Identity,
                Arsenal = 0,
                Emoney = 0,
                EmoneyTotal = 0,
                Merit = 0,
                Guide = 0,
                GuideTotal = 0,
                JoinDate = DateTime.Now,
                Pk = 0,
                PkTotal = 0,
                Proffer = 0,
                ProfferTotal = 0,
                Rank = (ushort) rank
            };

            if (!await BaseRepository.SaveAsync(m_attr))
                return false;

            UserName = user.Name;
            LookFace = user.Mesh;
            SyndicateName = syn.Name;
            Level = user.Level;
            MateIdentity = user.MateIdentity;

            await Log.GmLogAsync("syndicate", $"User [{user.Identity}, {user.Name}] has joined [{syn.Identity}, {syn.Name}]");
            return true;
        }

        public enum SyndicateRank : ushort
        {
            GuildLeader = 1000,

            DeputyLeader = 990,
            HonoraryDeputyLeader = 980,
            LeaderSpouse = 920,

            Manager = 890,
            HonoraryManager = 880,

            TulipSupervisor = 859,
            OrchidSupervisor = 858,
            CpSupervisor = 857,
            ArsenalSupervisor = 856,
            SilverSupervisor = 855,
            GuideSupervisor = 854,
            PkSupervisor = 853,
            RoseSupervisor = 852,
            LilySupervisor = 851,
            Supervisor = 850,
            HonorarySupervisor = 840,

            Steward = 690,
            HonorarySteward = 680,
            DeputySteward = 650,
            DeputyLeaderSpouse = 620,
            DeputyLeaderAide = 611,
            LeaderSpouseAide = 610,
            Aide = 602,

            TulipAgent = 599,
            OrchidAgent = 598,
            CpAgent = 597,
            ArsenalAgent = 596,
            SilverAgent = 595,
            GuideAgent = 594,
            PkAgent = 593,
            RoseAgent = 592,
            LilyAgent = 591,
            Agent = 590,
            SupervisorSpouse = 521,
            ManagerSpouse = 520,
            SupervisorAide = 511,
            ManagerAide = 510,

            TulipFollower = 499,
            OrchidFollower = 498,
            CpFollower = 497,
            ArsenalFollower = 496,
            SilverFollower = 495,
            GuideFollower = 494,
            PkFollower = 493,
            RoseFollower = 492,
            LilyFollower = 491,
            Follower = 490,
            StewardSpouse = 420,

            SeniorMember = 210,
            Member = 200,

            None = 0
        }

        public async Task<bool> SaveAsync()
        {
            return await BaseRepository.SaveAsync(m_attr);
        }

        public async Task<bool> DeleteAsync()
        {
            return await BaseRepository.DeleteAsync(m_attr);
        }
    }
}