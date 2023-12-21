using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.World.Maps;

namespace Comet.Game.Managers
{
    public class MapManager
    {
        // A thread-safe dictionary to store users mapped to their map IDs
        private readonly ConcurrentDictionary<uint, GameMap> GameMaps;
        private readonly ConcurrentDictionary<uint, GameMapData> m_mapData =
            new ConcurrentDictionary<uint, GameMapData>();
        public MapManager()
        {
            GameMaps = new ConcurrentDictionary<uint, GameMap>();
        }

        public async Task LoadDataAsync()
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ini", "GameMap.dat");
            var stream = File.OpenRead(filePath);
            BinaryReader reader = new(stream);

            int mapDataCount = reader.ReadInt32();
            Console.WriteLine($"Loading {mapDataCount} map data entries...");
            for (int i = 0; i < mapDataCount; i++)
            {
                uint idMap = reader.ReadUInt32();
                int length = reader.ReadInt32();
                string name = new(reader.ReadChars(length));
                uint puzzle = reader.ReadUInt32();

                // Construct the full path for the map file
                string mapFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                                  "ini",
                                                  name.Replace("\\", Path.DirectorySeparatorChar.ToString()));

                GameMapData mapData = new(idMap);
                if (mapData.Load(mapFilePath)) // Use the full path here
                {
                    Console.WriteLine($"Loaded map {name} with ID {idMap}");
                    m_mapData.TryAdd(idMap, mapData);
                }
            }

            reader.Close();
            stream.Close();
            reader.Dispose();
            await stream.DisposeAsync();
        }

        public GameMap GetMap(uint idMap)
        {
            return GameMaps.TryGetValue(idMap, out var value) ? value : null;
        }
    }
}