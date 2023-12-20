using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Comet.Game.States;

namespace Comet.Game.Managers
{
    public class MapManager
    {
        // A thread-safe dictionary to store users mapped to their map IDs
        private readonly ConcurrentDictionary<uint, ConcurrentDictionary<uint, Character>> maps;

        public MapManager()
        {
            maps = new ConcurrentDictionary<uint, ConcurrentDictionary<uint, Character>>();
        }

        public void AddUser(uint mapId, Character user)
        {
            Console.WriteLine($"Adding user {user.Name} to map {mapId}");
            var usersInMap = maps.GetOrAdd(mapId, new ConcurrentDictionary<uint, Character>());
            usersInMap.TryAdd(user.UID, user);
        }

        public void RemoveUser(uint mapId, uint userId)
        {
            if (maps.TryGetValue(mapId, out var usersInMap))
            {
                usersInMap.TryRemove(userId, out _);
            }
        }

        public IEnumerable<Character> GetUsersInMap(uint mapId)
        {
            if (maps.TryGetValue(mapId, out var usersInMap))
            {
                return usersInMap.Values;
            }

            return new List<Character>();
        }
    }
}