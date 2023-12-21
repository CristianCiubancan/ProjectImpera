// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Peerage Manager.cs
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
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets;
using Comet.Game.States;

namespace Comet.Game.World.Managers
{
    public sealed class PeerageManager
    {
        private ConcurrentDictionary<uint, DbPeerage> PeerageSet = new ConcurrentDictionary<uint, DbPeerage>();

        public async Task InitializeAsync()
        {
            List<DbPeerage> dbPeerages = await PeerageRepository.GetAsync();
            foreach (var peerage in dbPeerages)
            {
                PeerageSet.TryAdd(peerage.UserIdentity, peerage);
            }
        }

        public async Task DonateAsync(Character user, ulong amount)
        {
            int oldPosition = GetPosition(user.Identity);
            NobilityRank oldRank = GetRanking(user.Identity);

            if (!PeerageSet.TryGetValue(user.Identity, out var peerage))
            {
                peerage = new DbPeerage
                {
                    UserIdentity = user.Identity,
                    Name = user.Name,
                    Donation = user.NobilityDonation + amount,
                    FirstDonation = DateTime.Now
                };
                await SaveAsync(peerage);

                PeerageSet.TryAdd(user.Identity, peerage);
            }
            else
            {
                peerage.Donation += amount;
                await SaveAsync(peerage);
            }

            user.NobilityDonation = peerage.Donation;

            NobilityRank rank = GetRanking(user.Identity);
            int position = GetPosition(user.Identity);

            await user.SendNobilityInfoAsync();

            if (position != oldPosition && position < 50)
            {
                foreach (var peer in PeerageSet.Values.OrderByDescending(z => z.Donation)
                    .ThenBy(y => y.FirstDonation))
                {
                    Character targetUser = Kernel.RoleManager.GetUser(peer.UserIdentity);
                    if (targetUser != null)
                        await targetUser.SendNobilityInfoAsync(true);
                }
            }

            if (rank != oldRank)
            {
                string message = "";
                switch (rank)
                {
                    case NobilityRank.King:
                        if (user.Gender == 1) message = string.Format(Language.StrPeeragePromptKing, user.Name, Kernel.Configuration.ServerName);
                        else message = string.Format(Language.StrPeeragePromptQueen, user.Name, Kernel.Configuration.ServerName);
                        break;
                    case NobilityRank.Prince:
                        if (user.Gender == 1) message = string.Format(Language.StrPeeragePromptPrince, user.Name, Kernel.Configuration.ServerName);
                        else message = string.Format(Language.StrPeeragePromptPrincess, user.Name, Kernel.Configuration.ServerName);
                        break;
                    case NobilityRank.Duke:
                        if (user.Gender == 1) message = string.Format(Language.StrPeeragePromptDuke, user.Name, Kernel.Configuration.ServerName);
                        else message = string.Format(Language.StrPeeragePromptDuchess, user.Name, Kernel.Configuration.ServerName);
                        break;
                    case NobilityRank.Earl:
                        if (user.Gender == 1) message = string.Format(Language.StrPeeragePromptEarl, user.Name);
                        else message = string.Format(Language.StrPeeragePromptCountess, user.Name);
                        break;
                    case NobilityRank.Baron:
                        if (user.Gender == 1) message = string.Format(Language.StrPeeragePromptBaron, user.Name);
                        else message = string.Format(Language.StrPeeragePromptBaroness, user.Name);
                        break;
                    case NobilityRank.Knight:
                        if (user.Gender == 1) message = string.Format(Language.StrPeeragePromptLady, user.Name);
                        else message = string.Format(Language.StrPeeragePromptLady, user.Name);
                        break;
                }

                if (user.Team != null)
                    await user.Team.SyncFamilyBattlePowerAsync();

                if (user.ApprenticeCount > 0)
                    await user.SynchroApprenticesSharedBattlePowerAsync();

                await Kernel.RoleManager.BroadcastMsgAsync(message, MsgTalk.TalkChannel.Center, Color.Red);
            }
        }

        public NobilityRank GetRanking(uint idUser)
        {
            int position = GetPosition(idUser);
            if (position >= 0 && position < 3)
                return NobilityRank.King;
            if (position >= 3 && position < 15)
                return NobilityRank.Prince;
            if (position >= 15 && position < 50)
                return NobilityRank.Duke;

            DbPeerage peerageUser = GetUser(idUser);
            ulong donation = 0;
            if (peerageUser != null)
            {
                donation = peerageUser.Donation;
            }
            else
            {
                Character user = Kernel.RoleManager.GetUser(idUser);
                if (user != null)
                {
                    donation = user.NobilityDonation;
                }
            }

            if (donation >= 200000000)
                return NobilityRank.Earl;
            if (donation >= 100000000)
                return NobilityRank.Baron;
            if (donation >= 30000000)
                return NobilityRank.Knight;
            return NobilityRank.Serf;
        }

        public int GetPosition(uint idUser)
        {
            bool found = false;
            int idx = -1;

            foreach (var peerage in PeerageSet.Values.OrderByDescending(x => x.Donation).ThenBy(x => x.FirstDonation))
            {
                idx++;
                if (peerage.UserIdentity == idUser)
                {
                    found = true;
                    break;
                }

                if (idx >= 50)
                    break;
            }

            return found ? idx : -1;
        }

        public async Task SendRankingAsync(Character target, int page)
        {
            if (target == null)
                return;

            const int MAX_PER_PAGE_I = 10;
            const int MAX_PAGES = 5;

            int currentPagesNum = Math.Max(1, Math.Min(PeerageSet.Count / MAX_PER_PAGE_I + 1, MAX_PAGES));
            if (page >= currentPagesNum)
                return;

            int current = 0;
            int min = page * MAX_PER_PAGE_I;
            int max = (page * MAX_PER_PAGE_I) + MAX_PER_PAGE_I;
            
            List<MsgPeerage.NobilityStruct> rank = new List<MsgPeerage.NobilityStruct>();
            foreach (var peerage in PeerageSet.Values.OrderByDescending(x => x.Donation).ThenBy(x => x.FirstDonation))
            {
                if (current >= MAX_PAGES * MAX_PER_PAGE_I)
                    break;

                if (current < min)
                {
                    current++;
                    continue;
                }

                if (current >= max)
                    break;

                Character peerageUser = Kernel.RoleManager.GetUser(peerage.UserIdentity);
                uint lookface = peerageUser?.Mesh ?? 0;
                rank.Add(new MsgPeerage.NobilityStruct
                {
                    Identity = peerage.UserIdentity,
                    Name = peerage.Name,
                    Donation = peerage.Donation,
                    LookFace = lookface,
                    Position = current,
                    Rank = GetRanking(peerage.UserIdentity)
                });

                current++;
            }

            MsgPeerage msg = new MsgPeerage(NobilityAction.List, (ushort) Math.Min(MAX_PER_PAGE_I, rank.Count), (ushort)currentPagesNum);
            msg.Rank.AddRange(rank);
            await target.SendAsync(msg);
        }

        public DbPeerage GetUser(uint idUser)
        {
            return PeerageSet.TryGetValue(idUser, out var peerage) ? peerage : null;
        }

        public ulong GetNextRankSilver(NobilityRank rank, ulong donation)
        {
            switch (rank)
            {
                case NobilityRank.Knight: return 30000000 - donation;
                case NobilityRank.Baron: return 100000000 - donation;
                case NobilityRank.Earl: return 200000000 - donation;
                case NobilityRank.Duke: return GetDonation(50) - donation;
                case NobilityRank.Prince: return GetDonation(15) - donation;
                case NobilityRank.King: return GetDonation(3) - donation;
                default: return 0;
            }
        }

        public ulong GetDonation(int position)
        {
            int ranking = 1;
            ulong donation = 0;
            foreach (var peerage in PeerageSet.Values.OrderByDescending(x => x.Donation).ThenBy(x => x.FirstDonation))
            {
                donation = peerage.Donation;
                if (ranking++ == position)
                    break;
            }
            return Math.Max(3000000, donation);
        }

        public async Task SaveAsync()
        {
            foreach (var peerage in PeerageSet.Values)
                await SaveAsync(peerage);
        }

        public async Task SaveAsync(DbPeerage peerage)
        {
            await using ServerDbContext context = new ServerDbContext();
            if (peerage.Identity == 0)
                context.Peerage.Add(peerage);
            else context.Peerage.Update(peerage);
            await context.SaveChangesAsync();
        }
    }
}