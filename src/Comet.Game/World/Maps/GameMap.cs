// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Game Map.cs
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
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core.Mathematics;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets;
using Comet.Game.States;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Items;
using Comet.Game.States.NPCs;
using Comet.Network.Packets;
using Comet.Shared;

#endregion

namespace Comet.Game.World.Maps
{
    public sealed class GameMap
    {
        public const uint DEFAULT_LIGHT_RGB = 0xFFFFFF;

        public static readonly sbyte[] WalkXCoords = {0, -1, -1, -1, 0, 1, 1, 1, 0};
        public static readonly sbyte[] WalkYCoords = {1, 1, 0, -1, -1, -1, 0, 1, 0};
        public static readonly sbyte[] RideXCoords = { 0, -2, -2, -2, 0, 2, 2, 2, 1, 0, -2, 0, 1, 0, 2, 0, 0, -2, 0, -1, 0, 2, 0, 1, 0 };
        public static readonly sbyte[] RideYCoords = { 2, 2, 0, -2, -2, -2, 0, 2, 2, 0, -1, 0, -2, 0, 1, 0, 0, 1, 0, -2, 0, -1, 0, 2, 0 };

        private readonly DbMap m_dbMap;
        private readonly DbDynamap m_dbDynamap;
        private GameMapData m_mapData;

        private GameBlock[,] m_blocks;

        private ConcurrentDictionary<uint, Character> m_users = new ConcurrentDictionary<uint, Character>();
        private ConcurrentDictionary<uint, Role> m_roles = new ConcurrentDictionary<uint, Role>();

        private List<Passway> m_passway = new List<Passway>();
        private List<DbRegion> m_regions = new List<DbRegion>();

        public Weather Weather;

        public GameMap(DbMap map)
        {
            m_dbMap = map;
        }

        public GameMap(DbDynamap map)
        {
            m_dbDynamap = map;
        }

        public int Partition { get; private set; }

        public uint Identity => m_dbMap?.Identity ?? m_dbDynamap?.Identity ?? 0;
        public string Name => m_dbMap?.Name ?? m_dbDynamap?.Name ?? "Invalid";
        public uint OwnerIdentity
        {
            get => m_dbMap?.OwnerIdentity ?? m_dbDynamap?.OwnerIdentity ?? 0;
            set
            {
                if (m_dbMap != null)
                    m_dbMap.OwnerIdentity = value;
                else if (m_dbDynamap != null)
                    m_dbDynamap.OwnerIdentity = value;
            }
        }

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
        public ulong Type => m_dbMap?.Type ?? m_dbDynamap?.Type ?? 0;

        public ushort PortalX
        {
            get => (ushort) (m_dbMap?.PortalX ?? m_dbDynamap?.PortalX ?? 0);
            set
            {
                if (m_dbMap != null)
                    m_dbMap.PortalX = value;
                else if (m_dbDynamap != null)
                    m_dbDynamap.PortalX = value;
            }
        }

        public ushort PortalY
        {
            get => (ushort) (m_dbMap?.PortalY ?? m_dbDynamap?.PortalY ?? 0);
            set
            {
                if (m_dbMap != null)
                    m_dbMap.PortalY = value;
                else if (m_dbDynamap != null)
                    m_dbDynamap.PortalY = value;
            }
        }

        public byte ResLev
        {
            get => m_dbMap?.ResourceLevel ?? m_dbDynamap?.ResourceLevel ?? 0;
            set
            {
                if (m_dbMap != null)
                    m_dbMap.ResourceLevel = value;
                else if (m_dbDynamap != null)
                    m_dbDynamap.ResourceLevel = value;
            }
        }

        public int Width => m_mapData?.Width ?? 0;
        public int Height => m_mapData?.Height ?? 0;

        public uint Light
        {
            get => m_dbMap?.Color ?? m_dbDynamap?.Color ?? 0;
            set
            {
                if (m_dbMap != null)
                    m_dbMap.Color = value;
                else if (m_dbDynamap != null)
                    m_dbDynamap.Color = value;
            }
        }

        public int BlocksX => (int) Math.Ceiling(Width / (double) GameBlock.BLOCK_SIZE);
        public int BlocksY => (int) Math.Ceiling(Height / (double) GameBlock.BLOCK_SIZE);

        public ulong Flag { get; set; }
        public int PlayerCount => m_users.Count;
        
        public async Task<bool> InitializeAsync()
        {
            if (m_dbMap == null && m_dbDynamap == null) return false;

            m_mapData = Kernel.MapManager.GetMapData(MapDoc);
            if (m_mapData == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Could not load map {Identity}({MapDoc}): map data not found");
                return false;
            }

            Weather = new Weather(this);

            m_blocks = new GameBlock[BlocksX, BlocksY];
            for (int y = 0; y < BlocksY; y++)
            {
                for (int x = 0; x < BlocksX; x++)
                {
                    m_blocks[x, y] = new GameBlock();
                }
            }

            List<DbPassway> passways = await PasswayRepository.GetAsync(Identity);
            foreach (var dbPassway in passways)
            {
                DbPortal portal = await PortalRepository.GetAsync(dbPassway.TargetMapId, dbPassway.TargetPortal);
                if (portal == null)
                {
                    await Log.WriteLogAsync(LogLevel.Error, $"Could not find portal for passway [{dbPassway.Identity}]");
                    continue;
                }

                m_passway.Add(new Passway
                {
                    Index = (int)dbPassway.MapIndex,
                    TargetMap = dbPassway.TargetMapId,
                    TargetX = (ushort)portal.PortalX,
                    TargetY = (ushort)portal.PortalY
                });
            }

            m_regions = await DbRegion.GetAsync(Identity);

            Partition = (int) Kernel.Services.Processor.SelectPartition();
            return true;
        }

        public async Task LoadTrapsAsync()
        {
            foreach (var dbTrap in (await DbTrap.GetAsync()).Where(x => x.MapId == Identity))
            {
                MapTrap trap = new MapTrap(dbTrap);
                if (!await trap.InitializeAsync())
                {
                    await Log.WriteLogAsync(LogLevel.Error, $"Could not start system map trap for {Identity} > Trap {dbTrap.Id}");
                    continue;
                }
            }
        }

        #region Query Role

        public Role QueryRole(uint target)
        {
            return m_roles.TryGetValue(target, out var value) ? value : null;
        }

        public T QueryRole<T>(uint target) where T : Role
        {
            return m_roles.TryGetValue(target, out var value) && value is T role ? role : null;
        }

        public T QueryRole<T>(Func<T, bool> pred) where T : Role
        {
            return m_roles.Values.Where(x => x is T).Cast<T>().FirstOrDefault(pred);
        }

        public Role QueryAroundRole(Role sender, uint target)
        {
            int currentBlockX = GetBlockX(sender.MapX);
            int currentBlockY = GetBlockY(sender.MapY);
            return Query9Blocks(currentBlockX, currentBlockY).FirstOrDefault(x => x.Identity == target);
        }

        public DynamicNpc QueryStatuary(Role sender, uint lookface, uint task)
        {
            return Query9BlocksByPos(sender.MapX, sender.MapY)
                .Where(x => x is DynamicNpc)
                .Cast<DynamicNpc>()
                .FirstOrDefault(x => x.Task0 == task && (x.Mesh - x.Mesh % 10) == (lookface - lookface % 10));
        }

        public List<Character> QueryPlayers(Func<Character, bool> pred)
        {
            return m_users.Values.Where(pred).ToList();
        }

        #endregion

        #region Role Management

        public async Task<bool> AddAsync(Role role)
        {
            if (m_roles.TryAdd(role.Identity, role))
            {
                EnterBlock(role, role.MapX, role.MapY);

                if (role is Character character)
                {
                    m_users.TryAdd(character.Identity, character);
                    await character.Screen.UpdateAsync();
                }
                else
                {
                    Kernel.RoleManager.AddRole(role);
                    foreach (var user in m_users.Values.Where(x =>
                        ScreenCalculations.GetDistance(x.MapX, x.MapY, role.MapX, role.MapY) <= Screen.VIEW_SIZE))
                    {
                        await user.Screen.SpawnAsync(role);
                    }
                }

                return true;
            }

            return false;
        }

        public async Task<bool> RemoveAsync(uint idRole)
        {
            if (m_roles.TryRemove(idRole, out var role))
            {
                m_users.TryRemove(idRole, out _);
                LeaveBlock(role);

                if (!(role is Character))
                    Kernel.RoleManager.RemoveRole(idRole);

                foreach (var user in m_users.Values)
                {
                    if (ScreenCalculations.GetDistance(role.MapX, role.MapY, user.MapX, user.MapY) > Screen.VIEW_SIZE)
                        continue;

                    await user.Screen.RemoveAsync(idRole, true);
                }
            }

            return false;
        }

        #endregion

        #region Broadcasting

        public async Task SendMapInfoAsync(Character user)
        {
            MsgAction action = new MsgAction
            {
                Action = MsgAction.ActionType.MapArgb,
                Identity = 1,
                Command = Light,
                Argument = 0
            };
            await user.SendAsync(action);
            await user.SendAsync(new MsgMapInfo(Identity, MapDoc, Type));

            if (Weather.GetType() != Weather.WeatherType.WeatherNone)
                await Weather.SendWeatherAsync(user);
            else
                await Weather.SendNoWeatherAsync(user);
        }

        public async Task BroadcastMsgAsync(IPacket msg, uint exclude = 0)
        {
            foreach (var user in m_users.Values)
            {
                if (user.Identity == exclude)
                    continue;

                await user.SendAsync(msg);
            }
        }

        public async Task BroadcastMsgAsync(string message, MsgTalk.TalkChannel channel = MsgTalk.TalkChannel.TopLeft, Color? color = null)
        {
            foreach (var user in m_users.Values)
            {
                await user.SendAsync(message, channel, color);
            }
        }

        public async Task BroadcastRoomMsgAsync(int x, int y, IPacket msg, uint exclude = 0)
        {
            foreach (var user in m_users.Values)
            {
                if (user.Identity == exclude ||
                    ScreenCalculations.GetDistance(x, y, user.MapX, user.MapY) > Screen.BROADCAST_SIZE)
                    continue;

                await user.SendAsync(msg);
            }
        }

        #endregion

        #region Blocks

        public void EnterBlock(Role role, int newX, int newY, int oldX = 0, int oldY = 0)
        {
            int currentBlockX = GetBlockX(newX);
            int currentBlockY = GetBlockY(newY);

            int oldBlockX = GetBlockX(oldX);
            int oldBlockY = GetBlockY(oldY);

            if (currentBlockX != oldBlockX || currentBlockY != oldBlockY)
            {
                if (GetBlock(oldBlockX, oldBlockY)?.RoleSet.ContainsKey(role.Identity) == true)
                    LeaveBlock(role);

                GetBlock(currentBlockX, currentBlockY)?.Add(role);
            }
        }

        public void LeaveBlock(Role role)
        {
            GetBlock(GetBlockX(role.MapX), GetBlockY(role.MapY))?.Remove(role);
        }


        public GameBlock GetBlock(int x, int y)
        {
            if (x < 0 || y < 0 || x >= BlocksX || y >= BlocksY)
                return null;
            return m_blocks[x, y];
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

        #endregion

        #region Map Checks

        /// <summary>
        ///     Checks if the map is a pk field. Wont add pk points.
        /// </summary>
        public bool IsPkField()
        {
            return (Type & (uint) MapTypeFlags.PkField) != 0;
        }

        /// <summary>
        ///     Disable teleporting by skills or scrolls.
        /// </summary>
        public bool IsChgMapDisable()
        {
            return (Type & (uint) MapTypeFlags.ChangeMapDisable) != 0;
        }

        /// <summary>
        ///     Disable recording the map position into the database.
        /// </summary>
        public bool IsRecordDisable()
        {
            return (Type & (uint) MapTypeFlags.RecordDisable) != 0;
        }

        /// <summary>
        ///     Disable team creation into the map.
        /// </summary>
        public bool IsTeamDisable()
        {
            return (Type & (uint) MapTypeFlags.TeamDisable) != 0;
        }

        /// <summary>
        ///     Disable use of pk on the map.
        /// </summary>
        public bool IsPkDisable()
        {
            return (Type & (uint) MapTypeFlags.PkDisable) != 0;
        }

        /// <summary>
        ///     Disable teleporting by actions.
        /// </summary>
        public bool IsTeleportDisable()
        {
            return (Type & (uint) MapTypeFlags.TeleportDisable) != 0;
        }

        /// <summary>
        ///     Checks if the map is a syndicate map
        /// </summary>
        /// <returns></returns>
        public bool IsSynMap()
        {
            return (Type & (uint) MapTypeFlags.GuildMap) != 0;
        }

        /// <summary>
        ///     Checks if the map is a prision
        /// </summary>
        public bool IsPrisionMap()
        {
            return (Type & (uint) MapTypeFlags.PrisonMap) != 0;
        }

        /// <summary>
        ///     If the map enable the fly skill.
        /// </summary>
        public bool IsWingDisable()
        {
            return (Type & (uint) MapTypeFlags.WingDisable) != 0;
        }

        /// <summary>
        ///     Check if the map is in war.
        /// </summary>
        public bool IsWarTime()
        {
            return (Flag & 1) != 0;
        }

        /// <summary>
        ///     Check if the map is the training ground. [1039]
        /// </summary>
        public bool IsTrainingMap()
        {
            return Identity == 1039;
        }

        /// <summary>
        ///     Check if its the family (clan) map.
        /// </summary>
        public bool IsFamilyMap()
        {
            return (Type & (uint) MapTypeFlags.Family) != 0;
        }

        /// <summary>
        ///     If the map enables booth to be built.
        /// </summary>
        public bool IsBoothEnable()
        {
            return (Type & (uint) MapTypeFlags.BoothEnable) != 0;
        }

        public bool IsDeadIsland()
        {
            return (Type & (uint) MapTypeFlags.DeadIsland) != 0;
        }

        public bool IsPkGameMap()
        {
            return (Type & (uint) MapTypeFlags.PkGame) != 0;
        }

        public bool IsMineField()
        {
            return (Type & (uint) MapTypeFlags.MineField) != 0;
        }

        public bool IsSkillMap()
        {
            return (Type & (ulong) MapTypeFlags.SkillMap) != 0;
        }

        public bool IsLineSkillMap()
        {
            return (Type & (ulong) MapTypeFlags.LineSkillOnly) != 0;
        }

        public bool IsDynamicMap()
        {
            return Identity > 999999;
        }

        #endregion

        #region Position Check

        /// <summary>
        /// Determinate if a coordinate has already been occupied by another object.
        /// </summary>
        /// <returns>False if the block is empty.</returns>
        public bool IsSuperPosition(int x, int y)
        {
            return GetBlock(GetBlockX(x), GetBlockY(y))?.RoleSet.Values.Any(a => a.MapX == x && a.MapY == y && a.IsAlive) != false;
        }

        /// <summary>
        /// Determinate if a coordinate is valid inside of a map.
        /// </summary>
        /// <returns></returns>
        public bool IsValidPoint(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool IsStandEnable(int x, int y)
        {
            return m_mapData[x, y].IsAccessible();
        }

        public bool IsMoveEnable(int x, int y, FacingDirection dir)
        {
            int newX = x + WalkXCoords[(int) dir];
            int newY = y + WalkYCoords[(int) dir];
            return IsMoveEnable(newX, newY);
        }

        public bool IsMoveEnable(int x, int y)
        {
            return IsValidPoint(x, y) && IsStandEnable(x, y);
        }

        public bool IsLayItemEnable(int x, int y)
        {
            return this[x,y].IsAccessible() && m_roles.All(role => role.Value != null && !(role.Value is MapItem) && !(role.Value is BaseNpc) || role.Value.MapX != x || role.Value.MapY != y);
        }

        public bool FindDropItemCell(int range, ref Point sender)
        {
            if (IsLayItemEnable(sender.X, sender.Y))
                return true;

            int size = range * 2 + 1;
            int bufSize = size ^ 2;

            for (int i = 0; i < 8; i++)
            {
                int newX = sender.X + WalkXCoords[i];
                int newY = sender.Y + WalkYCoords[i];
                if (IsLayItemEnable(newX, newY))
                {
                    sender.X = newX;
                    sender.Y = newY;
                    return true;
                }
            }

            Point pos = sender;
            var setItem = Query9BlocksByPos(sender.X, sender.Y).Where(x => x is MapItem && x.GetDistance(pos.X, pos.Y) <= range).Cast<MapItem>().ToList();
            int nMinRange = range + 1;
            bool ret = false;
            var posFree = new Point();
            for (int i = Math.Max(sender.X - range, 0); i <= sender.X + range && i < Width; i++)
            {
                for (int j = Math.Max(sender.Y - range, 0); j <= sender.Y + range && j < Height; j++)
                {
                    int idx = Pos2Index(i - (sender.X - range), j - (sender.Y - range), size, size);

                    if (idx >= 0 && idx < bufSize)
                        if (setItem.FirstOrDefault(x =>
                                Pos2Index(x.MapX - i + range, x.MapY - j + range, range, range) == idx) != null)
                            continue;

                    if (IsLayItemEnable(sender.X, sender.Y))
                    {
                        double nDistance = ScreenCalculations.GetDistance(i, j, sender.X, sender.Y);
                        if (nDistance < nMinRange)
                        {
                            nMinRange = (int)nDistance;
                            posFree.X = i;
                            posFree.Y = j;
                            ret = true;
                        }
                    }
                }
            }

            if (ret)
            {
                sender = posFree;
                return true;
            }
            return false;
        }

        #endregion

        #region Indexes

        public int Pos2Index(int x, int y, int cx, int cy)
        {
            return x + y * cx;
        }

        public int Index2X(int idx, int cx, int cy)
        {
            return idx % cy;
        }

        public int Index2Y(int idx, int cx, int cy)
        {
            return idx / cy;
        }

        #endregion

        #region Portals and Passages

        public bool GetRebornMap(ref uint idMap, ref Point target)
        {
            idMap = m_dbMap?.RebornMap ?? m_dbDynamap.RebornMap;
            GameMap targetMap = Kernel.MapManager.GetMap(idMap);
            if (targetMap == null)
            {
                Log.WriteLogAsync(LogLevel.Error, $"Could not get reborn map [{Identity}]!").ConfigureAwait(false);
                return false;
            }

            if (m_dbMap.LinkX == 0 || m_dbMap.LinkY == 0)
            {
                target = new Point(targetMap.PortalX, targetMap.PortalY);
            }
            else
            {
                target = new Point(m_dbMap.LinkX, m_dbMap.LinkY);
            }

            return true;
        }

        public bool GetPassageMap(ref uint idMap, ref Point target, ref Point source)
        {
            if (!IsValidPoint(source.X, source.Y))
                return false;

            int idxPassage = m_mapData.GetPassage(source.X, source.Y);
            if (idxPassage < 0)
                return false;

            if (IsDynamicMap())
            {
                idMap = m_dbDynamap.LinkMap;
                target.X = m_dbDynamap.LinkX;
                target.Y = m_dbDynamap.LinkY;
                return true;
            }

            Passway passway = m_passway.FirstOrDefault(x => x.Index == idxPassage);
            idMap = passway.TargetMap;
            target = new Point(passway.TargetX, passway.TargetY);
            return true;
        }

        #endregion

        #region Regions

        public bool QueryRegion(RegionTypes regionType, ushort x, ushort y)
        {
            return m_regions.Where(re => (x > re.BoundX && x < re.BoundX + re.BoundCX) && (y > re.BoundY && y < re.BoundY + re.BoundCY)).Any(region => region.Type == (int)regionType);
        }

        #endregion

        #region Status

        public async Task SetStatusAsync(ulong flag, bool add)
        {
            ulong oldFlag = Flag;
            if (add)
                Flag |= flag;
            else
                Flag &= ~flag;

            if (Flag != oldFlag)
                await BroadcastMsgAsync(new MsgMapInfo(Identity, MapDoc, Flag));
        }

        public void ResetBattle()
        {
            foreach (var player in m_users.Values)
            {
                player.BattleSystem.ResetBattle();
            }
        }

        #endregion

        #region Ai Timer

        public async Task<int> OnTimerAsync()
        {
            await Weather.OnTimerAsync();

            if (m_users.Count == 0)
                return 0;

            int result = 0;
            foreach (var ai in m_roles.Values.Where(x => x is IRoleAi))
            {
                var roleAi = (IRoleAi)ai;
                if (roleAi.IsActive)
                {
                    await ai.OnTimerAsync();
                    result++;
                }
            }
            return result;
        }

        #endregion

        #region Tiles

        public Tile this[int x, int y] => m_mapData[x, y];

        #endregion

        #region Static

        public static int GetBlockX(int x)
        {
            return x / GameBlock.BLOCK_SIZE;
        }

        public static int GetBlockY(int y)
        {
            return y / GameBlock.BLOCK_SIZE;
        }

        #endregion

        #region Database

        public async Task<bool> SaveAsync()
        {
            if (m_dbMap == null && m_dbDynamap == null)
                return false;

            if (m_dbMap != null)
                return await BaseRepository.SaveAsync(m_dbMap);
            return await BaseRepository.SaveAsync(m_dbDynamap);
        }

        public async Task<bool> DeleteAsync()
        {
            if (m_dbMap == null && m_dbDynamap == null)
                return false;

            if (m_dbMap != null)
                return await BaseRepository.DeleteAsync(m_dbMap);
            return await BaseRepository.DeleteAsync(m_dbDynamap);
        }

        #endregion
    }

    public struct Passway
    {
        public int Index;
        public uint TargetMap;
        public ushort TargetX;
        public ushort TargetY;
    }

    [Flags]
    public enum MapTypeFlags : ulong
    {
        Normal = 0,
        PkField = 0x1, //0x1 1
        ChangeMapDisable = 0x2, //0x2 2
        RecordDisable = 0x4, //0x4 4 
        PkDisable = 0x8, //0x8 8
        BoothEnable = 0x10, //0x10 16
        TeamDisable = 0x20, //0x20 32
        TeleportDisable = 0x40, // 0x40 64
        GuildMap = 0x80, // 0x80 128
        PrisonMap = 0x100, // 0x100 256
        WingDisable = 0x200, // 0x200 512
        Family = 0x400, // 0x400 1024
        MineField = 0x800, // 0x800 2048
        PkGame = 0x1000, // 0x1000 4098
        NeverWound = 0x2000, // 0x2000 8196
        DeadIsland = 0x4000, // 0x4000 16392
        SkillMap = 1UL << 62,
        LineSkillOnly = 1UL << 63
    }

    public enum RegionTypes
    {
        None = 0,
        City = 1,
        Weather = 2,
        Statuary = 3,
        Desc = 4,
        Gobaldesc = 5,
        Dance = 6, // data0: idLeaderRegion, data1: idMusic, 
        PkProtected = 7,
        FlagProtection = 24,
        FlagBase = 25,
        JiangHuBonusArea = 30,
    }
}