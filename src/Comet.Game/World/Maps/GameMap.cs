using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Game.Database.Models;
using Comet.Game.Packets;
using Comet.Game.States;
using Comet.Game.States.BaseEntities;

namespace Comet.Game.World.Maps
{
    public sealed class GameMap
    {
        public uint Identity => m_dbMap?.Identity ?? m_dbDynamap?.Identity ?? 0;
        public string Name => m_dbMap?.Name ?? m_dbDynamap?.Name ?? "Invalid";
        private GameMapData m_mapData;
        private DbMap m_dbMap;
        private DbDynamap m_dbDynamap;
        public int Width => m_mapData?.Width ?? 0;
        public int Height => m_mapData?.Height ?? 0;
        public int BlocksX => (int)Math.Ceiling(Width / (double)GameBlock.BLOCK_SIZE);
        public int BlocksY => (int)Math.Ceiling(Height / (double)GameBlock.BLOCK_SIZE);
        public static readonly sbyte[] WalkXCoords = { 0, -1, -1, -1, 0, 1, 1, 1, 0 };
        public static readonly sbyte[] WalkYCoords = { 1, 1, 0, -1, -1, -1, 0, 1, 0 };
        private GameBlock[,] m_blocks;
        private ConcurrentDictionary<uint, Character> m_users = new ConcurrentDictionary<uint, Character>();
        public uint MapDoc
        {
            get => m_dbMap?.MapDoc ?? m_dbDynamap?.MapDoc ?? 0;
            set
            {
                if (m_dbMap != null)
                    m_dbMap.MapDoc = value;
                else if (m_dbDynamap != null)
                    m_dbDynamap.MapDoc = value;
            }
        }
        public async Task<bool> InitializeAsync()
        {
            if (m_dbMap == null && m_dbDynamap == null) return false;

            m_mapData = Kernel.MapManager.GetMapData(MapDoc);
            if (m_mapData == null)
            {
                // await Log.WriteLogAsync(LogLevel.Warning, $"Could not load map {Identity}({MapDoc}): map data not found");
                return false;
            }

            // Weather = new Weather(this);

            m_blocks = new GameBlock[BlocksX, BlocksY];
            for (int y = 0; y < BlocksY; y++)
            {
                for (int x = 0; x < BlocksX; x++)
                {
                    m_blocks[x, y] = new GameBlock();
                }
            }

            // List<DbPassway> passways = await PasswayRepository.GetAsync(Identity);
            // foreach (var dbPassway in passways)
            // {
            //     DbPortal portal = await PortalRepository.GetAsync(dbPassway.TargetMapId, dbPassway.TargetPortal);
            //     if (portal == null)
            //     {
            //         await Log.WriteLogAsync(LogLevel.Error, $"Could not find portal for passway [{dbPassway.Identity}]");
            //         continue;
            //     }

            //     m_passway.Add(new Passway
            //     {
            //         Index = (int)dbPassway.MapIndex,
            //         TargetMap = dbPassway.TargetMapId,
            //         TargetX = (ushort)portal.PortalX,
            //         TargetY = (ushort)portal.PortalY
            //     });
            // }

            // m_regions = await DbRegion.GetAsync(Identity);

            // Partition = (int)Kernel.Services.Processor.SelectPartition();
            return true;
        }
        public async Task<bool> AddAsync(Character user)
        {
            if (m_users.TryAdd(user.UID, user))
            {
                await user.SendSpawnToAsync(user);
                return true;
            }

            return false;
        }

        public GameMap(DbMap map)
        {
            m_dbMap = map;
        }

        public GameMap(DbDynamap map)
        {
            m_dbDynamap = map;
        }

        public async Task<bool> RemoveAsync(Character user)
        {
            Console.WriteLine("TO BE INPLEMENTED: Removing user from map");
            if (m_users.TryRemove(user.UID, out var value))
            {
                // TODO: update the screen
                return true;
            }

            return false;
        }

        public async Task SendMapInfoAsync(Character user)
        {
            Console.WriteLine("TO BE INPLEMENTED: Sending map info to user");
            // MsgAction action = new MsgAction
            // {
            //     Action = MsgAction.ActionType.MapArgb,
            //     Identity = 1,
            //     Command = Light,
            //     Argument = 0
            // };
            // await user.SendAsync(action);
            // await user.SendAsync(new MsgMapInfo(Identity, MapDoc, Type));

            // if (Weather.GetType() != Weather.WeatherType.WeatherNone)
            //     await Weather.SendWeatherAsync(user);
            // else
            //     await Weather.SendNoWeatherAsync(user);
        }

        public List<Role> Query9BlocksByPos(int x, int y)
        {
            return Query9Blocks(GetBlockX(x), GetBlockY(y));
        }

        public List<Role> Query9Blocks(int x, int y)
        {
            List<Role> result = new List<Role>();

            //Console.WriteLine(@"============== Query Block Begin =================");
            for (int aroundBlock = 0; aroundBlock < WalkXCoords.Length; aroundBlock++)
            {
                int viewBlockX = x + WalkXCoords[aroundBlock];
                int viewBlockY = y + WalkYCoords[aroundBlock];

                //Console.WriteLine($@"Block: {viewBlockX},{viewBlockY} [from: {viewBlockX*18},{viewBlockY*18}] [to: {viewBlockX*18+18},{viewBlockY*18+18}]");

                if (viewBlockX < 0 || viewBlockY < 0 || viewBlockX >= BlocksX || viewBlockY >= BlocksY)
                    continue;

                result.AddRange(GetBlock(viewBlockX, viewBlockY).RoleSet.Values);
            }

            //Console.WriteLine(@"============== Query Block End =================");
            return result;
        }

        public static int GetBlockX(int x)
        {
            return x / GameBlock.BLOCK_SIZE;
        }

        public static int GetBlockY(int y)
        {
            return y / GameBlock.BLOCK_SIZE;
        }

        public GameBlock GetBlock(int x, int y)
        {
            if (x < 0 || y < 0 || x >= BlocksX || y >= BlocksY)
                return null;
            return m_blocks[x, y];
        }
    }
}