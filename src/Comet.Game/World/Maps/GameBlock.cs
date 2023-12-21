// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Game Block.cs
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
using System.Linq;
using System.Threading;
using Comet.Game.States;
using Comet.Game.States.BaseEntities;

#endregion

namespace Comet.Game.World.Maps
{
    /// <summary>
    ///     A block is a set of the map which will hold a collection with all entities in an area. This will help us
    ///     iterating over a limited number of roles when trying to process AI and movement. Instead of iterating a list with
    ///     thousand roles in the entire map, we'll just iterate the blocks around us.
    /// </summary>
    public class GameBlock
    {
        /// <summary>
        ///     The width/height of a view block.
        /// </summary>
        public const int BLOCK_SIZE = 18;

        private int m_userCount = 0;

        /// <summary>
        ///     Collection of roles currently inside of this block.
        /// </summary>
        public ConcurrentDictionary<uint, Role> RoleSet = new ConcurrentDictionary<uint, Role>();
        
        public bool Add(Role role)
        {
            if (role is Character)
            {
                Interlocked.Increment(ref m_userCount);
                //foreach (var mob in RoleSet.Values.Where(x => x is Monster).Cast<Monster>())
                //{
                //    mob.PlayerOnSight++;
                //}
            }
            return RoleSet.TryAdd(role.Identity, role);
        }

        public bool Remove(Role role)
        {
            bool remove = RoleSet.TryRemove(role.Identity, out _);
            if (role is Character && remove)
            {
                Interlocked.Decrement(ref m_userCount);
                //foreach (var mob in RoleSet.Values.Where(x => x is Monster).Cast<Monster>())
                //{
                //    mob.PlayerOnSight--;
                //}
            }
            return remove;
        }

        public bool Remove(uint role)
        {
            bool remove = RoleSet.TryRemove(role, out var target);
            if (target is Character && remove)
            {
                Interlocked.Decrement(ref m_userCount);
                //foreach (var mob in RoleSet.Values.Where(x => x is Monster).Cast<Monster>())
                //{
                //    mob.PlayerOnSight--;
                //}
            }
            return remove;
        }
    }
}