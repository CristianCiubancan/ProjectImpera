// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Generator.cs
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
using Comet.Game.Database.Models;
using Comet.Game.States;
using Comet.Game.World.Maps;
using Comet.Shared;

#endregion

namespace Comet.Game.World
{
    public sealed class Generator
    {
        private const int _MAX_PER_GEN = 15;
        private const int _MIN_TIME_BETWEEN_GEN = 10;
        private static uint m_idGenerator = 2000000;

        private readonly DbGenerator m_dbGen;
        private readonly DbMonstertype m_dbMonster;
        private readonly Point m_pCenter;
        private readonly GameMap m_pMap;
        private TimeOut m_pTimer = new TimeOut(_MIN_TIME_BETWEEN_GEN);
        private Monster m_pDemo;

        public int Generated => m_dicMonsters.Count;

        private ConcurrentDictionary<uint, Monster> m_dicMonsters = new ConcurrentDictionary<uint, Monster>();

        public bool CanBeProcessed = false;

        public Generator(DbGenerator dbGen)
        {
            m_dbGen = dbGen;
            m_pTimer = new TimeOut(Math.Max(m_dbGen.RestSecs, _MIN_TIME_BETWEEN_GEN));

            m_pMap = Kernel.MapManager.GetMap(m_dbGen.Mapid);
            if (m_pMap == null)
            {
                Log.WriteLogAsync(LogLevel.Error, $"Could not load map ({m_dbGen.Mapid}) for generator ({m_dbGen.Id})")
                    .ConfigureAwait(false);
                return;
            }

            m_dbMonster = Kernel.RoleManager.GetMonstertype(m_dbGen.Npctype);
            if (m_dbMonster == null)
            {
                Log.WriteLogAsync(LogLevel.Error, $"Could not load monster ({m_dbGen.Npctype}) for generator ({m_dbGen.Id})")
                    .ConfigureAwait(false);
                return;
            }

            m_pCenter = new Point(m_dbGen.BoundX + m_dbGen.BoundCx / 2, m_dbGen.BoundY + m_dbGen.BoundCy / 2);
            CanBeProcessed = true;
        }

        public Generator(uint idMap, uint idMonster, ushort usX, ushort usY, ushort usCx, ushort usCy)
        {
            m_dbGen = new DbGenerator
            {
                Mapid = idMap,
                BoundX = usX,
                BoundY = usY,
                BoundCx = usCx,
                BoundCy = usCy,
                Npctype = idMonster,
                MaxNpc = 0,
                MaxPerGen = 0,
                Id = m_idGenerator++
            };

            m_pMap = Kernel.MapManager.GetMap(m_dbGen.Mapid);
            if (m_pMap == null)
            {
                Log.WriteLogAsync(LogLevel.Error, $"Could not load map ({m_dbGen.Mapid}) for generator ({m_dbGen.Id})")
                    .ConfigureAwait(false);
                return;
            }

            m_dbMonster = Kernel.RoleManager.GetMonstertype(m_dbGen.Npctype);
            if (m_dbMonster == null)
            {
                Log.WriteLogAsync(LogLevel.Error, $"Could not load monster ({m_dbGen.Npctype}) for generator ({m_dbGen.Id})")
                    .ConfigureAwait(false);
                return;
            }

            m_pCenter = new Point(m_dbGen.BoundX + m_dbGen.BoundCx / 2, m_dbGen.BoundY + m_dbGen.BoundCy / 2);
        }

        public uint Identity => m_dbGen.Id;

        public uint RoleType => m_dbGen.Npctype;

        public int RestSeconds => Math.Max(m_dbGen.RestSecs, _MIN_TIME_BETWEEN_GEN);

        public uint MapIdentity => m_dbGen.Mapid;

        public string MonsterName => m_dbMonster.Name;

        public bool IsActive => m_pMap.PlayerCount > 0 || m_dicMonsters.Values.Any(x => !x.IsAlive);

        public async Task<Point> FindGenPosAsync()
        {
            Point result = new Point
            {
                X = m_dbGen.BoundX + await Kernel.Services.Randomness.NextAsync(0, m_dbGen.BoundCx),
                Y = m_dbGen.BoundY + await Kernel.Services.Randomness.NextAsync(0, m_dbGen.BoundCy)
            };

            if (!m_pMap.IsValidPoint(result.X, result.Y) || !m_pMap.IsStandEnable(result.X, result.Y) || m_pMap.IsSuperPosition(result.X, result.Y))
            {
                return default;
            }

            return result;
        }

        public async Task<Monster> GenerateMonsterAsync()
        {
            Monster mob = new Monster(m_dbMonster, (uint) IdentityGenerator.Monster.GetNextIdentity, this);

            Point pos = await FindGenPosAsync();
            if (pos == default)
            {
                IdentityGenerator.Monster.ReturnIdentity(mob.Identity);
                return null;
            }

            if (!await mob.InitializeAsync(m_pMap.Identity, (ushort) pos.X, (ushort) pos.Y))
            {
                IdentityGenerator.Monster.ReturnIdentity(mob.Identity);
                return null;
            }
            return mob;
        }

        public Task GenerateAsync()
        {
            if (!IsActive)
                return Task.CompletedTask;

            int generate = Math.Min(m_dbGen.MaxPerGen - Generated, _MAX_PER_GEN);
            if (generate > 0)
            {
                while (generate-- > 0)
                {
                    Kernel.Services.Processor.Queue(m_pMap.Partition, async () =>
                    {
                        Monster monster = await GenerateMonsterAsync();
                        if (monster == null || !m_dicMonsters.TryAdd(monster.Identity, monster))
                            return;
                        await monster.EnterMapAsync();
                    });                    
                }
            }
            return Task.CompletedTask;
        }

        public bool Add(Monster monster)
        {
            return m_dicMonsters.TryAdd(monster.Identity, monster);
        }

        public void Remove(uint role)
        {
            m_dicMonsters.TryRemove(role, out _);
        }

        public async Task ClearGeneratorAsync()
        {
            foreach (var monster in m_dicMonsters.Values)
            {
                await monster.BeKillAsync(null);
                await monster.LeaveMapAsync();
            }
        }

        public Monster[] GetRoles()
        {
            return m_dicMonsters.Values.ToArray();
        }

        public Point GetCenter()
        {
            return m_pCenter;
        }

        public bool IsTooFar(ushort x, ushort y, int nRange)
        {
            return !(x >= m_dbGen.BoundX - nRange
                     && x < m_dbGen.BoundX + m_dbGen.BoundCx + nRange
                     && y >= m_dbGen.BoundY - nRange
                     && y < m_dbGen.BoundY + m_dbGen.BoundCy + nRange);
        }

        public bool IsInRegion(int x, int y)
        {
            return x >= m_dbGen.BoundX && x < m_dbGen.BoundX + m_dbGen.BoundCx
                                       && y >= m_dbGen.BoundY && y < m_dbGen.BoundY + m_dbGen.BoundCy;
        }

        public int GetWidth()
        {
            return m_dbGen.BoundCx;
        }

        public int GetHeight()
        {
            return m_dbGen.BoundCy;
        }

        public int GetPosX()
        {
            return m_dbGen.BoundX;
        }

        public int GetPosY()
        {
            return m_dbGen.BoundY;
        }
    }
}