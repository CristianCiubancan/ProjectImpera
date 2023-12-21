// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Syndicate Manager.cs
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
using Comet.Game.Database.Repositories;
using Comet.Game.States.Syndicates;

namespace Comet.Game.World.Managers
{
    public sealed class SyndicateManager
    {
        private ConcurrentDictionary<ushort, Syndicate> m_dicSyndicates = new ConcurrentDictionary<ushort, Syndicate>();

        public async Task<bool> InitializeAsync()
        {
            var dbSyndicates = await SyndicateRepository.GetAsync();
            foreach (var dbSyn in dbSyndicates)
            {
                Syndicate syn = new Syndicate();
                if (!await syn.CreateAsync(dbSyn))
                    continue;
                m_dicSyndicates.TryAdd(syn.Identity, syn);
            }

            foreach (var syndicate in m_dicSyndicates.Values)
            {
                await syndicate.LoadRelationAsync();
            }

            return true;
        }

        public bool AddSyndicate(Syndicate syn)
        {
            return m_dicSyndicates.TryAdd(syn.Identity, syn);
        }

        public Syndicate GetSyndicate(int idSyndicate)
        {
            return m_dicSyndicates.TryGetValue((ushort) idSyndicate, out var syn) ? syn : null;
        }

        public Syndicate GetSyndicate(string name)
        {
            return m_dicSyndicates.Values.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Find the syndicate a user is in.
        /// </summary>
        public Syndicate FindByUser(uint idUser)
        {
            return m_dicSyndicates.Values.FirstOrDefault(x => x.QueryMember(idUser) != null);
        }
    }
}