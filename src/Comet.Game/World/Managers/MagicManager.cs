// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Magic Manager.cs
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
using System.Threading.Tasks;
using Comet.Game.Database.Models;
using Comet.Game.Database.Repositories;

#endregion

namespace Comet.Game.World.Managers
{
    public sealed class MagicManager
    {
        private ConcurrentDictionary<uint, DbMagictype> m_magicType  = new ConcurrentDictionary<uint, DbMagictype>();

        public async Task InitializeAsync()
        {
            foreach (var magicType in await MagictypeRepository.GetAsync())
            {
                m_magicType.TryAdd(magicType.Id, magicType);
            }
        }

        public byte GetMaxLevel(uint idType)
        {
            return (byte) (m_magicType.Values.Where(x => x.Type == idType).OrderByDescending(x => x.Level).FirstOrDefault()?.Level ?? 0);
        }

        public DbMagictype GetMagictype(uint idType, ushort level)
        {
            return m_magicType.Values.FirstOrDefault(x => x.Type == idType && x.Level == level);
        }
    }
}