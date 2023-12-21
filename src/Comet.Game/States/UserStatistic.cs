// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - User Statistic.cs
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
using System.Linq;
using System.Threading.Tasks;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Database.Repositories;

#endregion

namespace Comet.Game.States
{
    public sealed class UserStatistic
    {
        private readonly ConcurrentDictionary<ulong, DbStatistic> m_dicStc = new ConcurrentDictionary<ulong, DbStatistic>();

        private readonly Character m_pOwner;

        public UserStatistic(Character user)
        {
            m_pOwner = user;
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                var list = await StatisticRepository.GetAsync(m_pOwner.Identity);
                if (list != null)
                {
                    foreach (var st in list)
                    {
                        m_dicStc.TryAdd(GetKey(st.EventType, st.DataType), st);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AddOrUpdateAsync(uint idEvent, uint idType, uint data, bool bUpdate)
        {
            ulong key = GetKey(idEvent, idType);
            if (m_dicStc.TryGetValue(key, out var stc))
            {
                stc.Data = data;
                if (bUpdate)
                    stc.Timestamp = DateTime.Now;
            }
            else
            {
                stc = new DbStatistic
                {
                    Data = data,
                    DataType = idType,
                    EventType = idEvent,
                    PlayerIdentity = m_pOwner.Identity,
                    Timestamp = DateTime.Now
                };
                m_dicStc.TryAdd(key, stc);
            }

            return await BaseRepository.SaveAsync(stc);
        }

        public async Task<bool> SetTimestampAsync(uint idEvent, uint idType, DateTime? data)
        {
            DbStatistic stc = GetStc(idEvent, idType);
            if (stc == null)
            {
                await AddOrUpdateAsync(idEvent, idType, 0, true);
                stc = GetStc(idEvent, idType);

                if (stc == null)
                    return false;
            }

            stc.Timestamp = data;
            return await BaseRepository.SaveAsync(stc);
        }

        public uint GetValue(uint idEvent, uint idType = 0)
        {
            return m_dicStc.FirstOrDefault(x => x.Key == GetKey(idEvent, idType)).Value?.Data ?? 0u;
        }

        public DbStatistic GetStc(uint idEvent, uint idType = 0)
        {
            return m_dicStc.FirstOrDefault(x => x.Key == GetKey(idEvent, idType)).Value;
        }

        public bool HasEvent(uint idEvent, uint idType)
        {
            return m_dicStc.ContainsKey(GetKey(idEvent, idType));
        }

        private ulong GetKey(uint idEvent, uint idType)
        {
            return idEvent + ((ulong) idType << 32);
        }

        public async Task<bool> SaveAllAsync()
        {
            return await BaseRepository.SaveAsync(m_dicStc.Values.ToList());
        }
    }
}