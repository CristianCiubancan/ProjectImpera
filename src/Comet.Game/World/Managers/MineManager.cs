// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Mine Manager.cs
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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.Database.Models;
using Comet.Game.States;

#endregion

namespace Comet.Game.World.Managers
{
    public sealed class MineManager
    {
        private ConcurrentDictionary<uint, MineDictionary> m_mineDictionary = new ConcurrentDictionary<uint, MineDictionary>();

        public async Task<bool> InitializeAsync()
        {
            var list = await DbMineRate.GetAsync();
            foreach (var db in list)
            {
                MineDictionary dict;
                if (m_mineDictionary.ContainsKey(db.MapIdentity))
                    dict = m_mineDictionary[db.MapIdentity];
                else m_mineDictionary.TryAdd(db.MapIdentity, dict = new MineDictionary());

                dict.Add(new MineObject(db));
            }

            return true;
        }

        public Task<uint> GetDropAsync(Character user)
        {
            if (!m_mineDictionary.ContainsKey(user.MapIdentity))
                return Task.FromResult<uint>(0);
            return m_mineDictionary[user.MapIdentity].GetMineResultAsync(user);
        }

        private class MineDictionary : List<MineObject>
        {
            public async Task<uint> GetMineResultAsync(Character user)
            {
                if (user.UserPackage.IsPackFull())
                    return 0;

                uint result = 0;
                foreach (MineObject mineObject in this)
                {
                    if (!mineObject.IsEnabled)
                        continue;
                    if (mineObject.Object.RequiredLevel != 0 && mineObject.Object.RequiredLevel > user.Level)
                        continue;
                    if (mineObject.Object.RequiredProfession != 0 &&
                        mineObject.Object.RequiredProfession != user.ProfessionSort)
                        continue;
                    if (mineObject.Object.RequiredMoney != 0 && mineObject.Object.RequiredMoney > user.Silvers)
                        continue;
                    if (mineObject.Object.RequiredEmoney != 0 && mineObject.Object.RequiredEmoney > user.ConquerPoints)
                        continue;
                    if (mineObject.Object.RequiredItemtype != 0 &&
                        user.UserPackage.GetItemByType(mineObject.Object.RequiredItemtype) == null)
                        continue;
                    if (!string.IsNullOrEmpty(mineObject.Object.RequiredItemName) &&
                        user.UserPackage[mineObject.Object.RequiredItemName] == null)
                        continue;
                    if (mineObject.Object.RequiredTaskIdentity != 0)
                    {
                        if (user.TaskDetail.QueryTaskData(mineObject.Object.RequiredTaskIdentity) == null)
                            continue;
                        if (mineObject.Object.RequiredTaskCompletion && user.TaskDetail
                            .QueryTaskData(mineObject.Object.RequiredTaskIdentity).CompleteFlag == 0)
                            continue;
                    }

                    if (await Kernel.ChanceCalcAsync(mineObject.Chance))
                    {
                        result = await mineObject.GetItemTypeAsync();
                        break;
                    }
                }

                return result;
            }
        }

        private class MineObject
        {
            private DbMineRate m_rate;
            private TimeOutMS m_timeOut = new TimeOutMS();

            public MineObject(DbMineRate rate)
            {
                m_rate = rate;
            }

            public double Chance
            {
                get
                {
                    if (!IsEnabled)
                        return 0;
                    return m_rate.ChanceX / (double) m_rate.ChanceY * 100;
                }
            }

            public DbMineRate Object => m_rate;

            public bool IsEnabled => m_rate.TimeOut == 0 || m_timeOut.IsTimeOut();

            public async Task<uint> GetItemTypeAsync()
            {
                if (m_rate.ItemtypeBegin == m_rate.ItemtypeEnd || m_rate.ItemtypeEnd < m_rate.ItemtypeBegin)
                    return m_rate.ItemtypeBegin;
                List<uint> itemTypes = new List<uint>();
                for (uint init = m_rate.ItemtypeBegin; init <= m_rate.ItemtypeEnd; init++) itemTypes.Add(init);
                Update();
                return itemTypes[await Kernel.NextAsync(0, itemTypes.Count) % itemTypes.Count];
            }

            private void Update()
            {
                m_timeOut.Update();
            }
        }
    }
}