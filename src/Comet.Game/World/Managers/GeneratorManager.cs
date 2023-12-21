// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - GeneratorManager.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Comet.Game.Database.Repositories;
using Comet.Game.World.Maps;
using Comet.Shared;

#endregion

namespace Comet.Game.World.Managers
{
    public sealed class GeneratorManager
    {
        private ConcurrentDictionary<int, List<Generator>> m_generators = new ConcurrentDictionary<int, List<Generator>>();

        private long m_Timeout;

        public GeneratorManager()
        {
        }

        public string ElapsedMilliseconds => m_Timeout.ToString();
        //{
        //    get
        //    {
        //        StringBuilder result = new StringBuilder();
        //        for (int i = 0; i < m_Timeouts.Length; i++)
        //        {
        //            result.AppendFormat("\t\t\t\tThread[0x{0:X4}]: {1}ms{2}", i, m_Timeouts[i], i >= m_Timeouts.Length - 1 ? "" : Environment.NewLine);
        //        }
        //        return result.ToString();
        //    }
        //}

        public async Task<bool> InitializeAsync()
        {
            try
            {
                foreach (var dbGen in await GeneratorRepository.GetAsync())
                {
                    Generator gen = new Generator(dbGen);
                    if (gen.CanBeProcessed)
                    {
                        await AddGeneratorAsync(gen);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task OnTimerAsync()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            foreach (var partition in m_generators.Keys)
            {
                foreach (var gen in m_generators[partition])
                {
                    await gen.GenerateAsync();
                }
            }
            sw.Stop();
            m_Timeout = sw.ElapsedMilliseconds;
            // await Task.Delay(1000);
        }

        public async Task<bool> AddGeneratorAsync(Generator generator)
        {
            try
            {
                if (!generator.CanBeProcessed)
                    return false;

                GameMap map = Kernel.MapManager.GetMap(generator.MapIdentity);
                if (m_generators.ContainsKey(map.Partition))
                    m_generators[map.Partition].Add(generator);
                else
                {
                    m_generators.TryAdd(map.Partition, new List<Generator>());
                    m_generators[map.Partition].Add(generator);
                }
            }
            catch (Exception e)
            {
                await Log.WriteLogAsync(LogLevel.Exception, e.ToString());
                return false;
            }
            return true;
        }

        public Generator GetGenerator(uint idGen)
        {
            return m_generators.Keys.SelectMany(partition => m_generators[partition]).FirstOrDefault(gen => gen.Identity == idGen);
        }

        public List<Generator> GetGenerators(uint idMap, string monsterName)
        {
            return (from partition in m_generators.Keys from gen in m_generators[partition] where gen.MapIdentity == idMap && gen.MonsterName.Equals(monsterName) select gen).ToList();
        }

        public List<Generator> GetByMonsterType(uint idType)
        {
            return (from partition in m_generators.Keys from gen in m_generators[partition] where gen.RoleType == idType select gen).ToList();
        }
    }
}