// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Map Manager.cs
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
using System.IO;
using System.Threading.Tasks;
using Comet.Game.Database.Models;
using Comet.Game.Database.Repositories;
using Comet.Game.World.Maps;
using Comet.Shared;

#endregion

namespace Comet.Game.World.Managers
{
    public sealed class MapManager
    {
        private readonly ConcurrentDictionary<uint, GameMapData> m_mapData =
            new ConcurrentDictionary<uint, GameMapData>();

        public ConcurrentDictionary<uint, GameMap> GameMaps { get; } = new ConcurrentDictionary<uint, GameMap>();

        public async Task LoadDataAsync()
        {
            var stream = File.OpenRead(string.Format(".{0}ini{0}GameMap.dat", Path.DirectorySeparatorChar));
            BinaryReader reader = new BinaryReader(stream);

            int mapDataCount = reader.ReadInt32();
            await Log.WriteLogAsync(LogLevel.Debug, $"Loading {mapDataCount} maps...");

            for (int i = 0; i < mapDataCount; i++)
            {
                uint idMap = reader.ReadUInt32();
                int length = reader.ReadInt32();
                string name = new string(reader.ReadChars(length));
                uint puzzle = reader.ReadUInt32();

                GameMapData mapData = new GameMapData(idMap);
                if (mapData.Load(name.Replace("\\", Path.DirectorySeparatorChar.ToString())))
                {
#if DEBUG
                    Console.WriteLine($"Map [{idMap},{name}] loaded...");
#endif
                    m_mapData.TryAdd(idMap, mapData);
                }
            }

            reader.Close();
            stream.Close();
            reader.Dispose();
            await stream.DisposeAsync();
        }

        public async Task LoadMapsAsync()
        {
            List<DbMap> maps = await MapsRepository.GetAsync();
            foreach (var dbmap in maps)
            {
                GameMap map = new GameMap(dbmap);
                if (await map.InitializeAsync())
                {
                    GameMaps.TryAdd(map.Identity, map);
                    await Log.GmLogAsync("map_channel", $"{map.Identity}\t{map.Name}\t\t\tPartition: {map.Partition}");
                }
            }

            List<DbDynamap> dynaMaps = await MapsRepository.GetDynaAsync();
            foreach (var dbmap in dynaMaps)
            {
                GameMap map = new GameMap(dbmap);
                if (await map.InitializeAsync())
                {
                    GameMaps.TryAdd(map.Identity, map);
                    await Log.GmLogAsync("map_channel", $"{map.Identity}\t{map.Name}\t\t\tPartition: {map.Partition}");
                }
            }

            foreach (var map in GameMaps.Values)
            {
                await map.LoadTrapsAsync();
            }
        }

        public GameMap GetMap(uint idMap)
        {
            return GameMaps.TryGetValue(idMap, out var value) ? value : null;
        }

        public GameMapData GetMapData(uint idDoc)
        {
            return m_mapData.TryGetValue(idDoc, out var map) ? map : null;
        }

        public bool AddMap(GameMap map)
        {
            return GameMaps.TryAdd(map.Identity, map);
        }

        public bool RemoveMap(uint idMap)
        {
            return GameMaps.TryRemove(idMap, out _);
        }
    }
}