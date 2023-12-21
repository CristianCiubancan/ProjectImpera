// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MapsRepository.cs
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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Game.Database.Models;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Comet.Game.Database.Repositories
{
    public static class MapsRepository
    {
        public static async Task<DbMap> GetAsync(uint idMap)
        {
            await using var db = new ServerDbContext();
            // return await db.Maps.FirstOrDefaultAsync(x => x.Identity == idMap);
            return await db.Maps.FirstOrDefaultAsync(x => x.Identity == idMap);
        }

        public static async Task<List<DbMap>> GetAsync()
        {
            await using var db = new ServerDbContext();
            // return db.Maps.Where(x => x.ServerIndex == -1 || x.ServerIndex == Kernel.Configuration.ServerIdentity)
            return db.Maps
                .ToList();
        }

        public static async Task<List<DbDynamap>> GetDynaAsync()
        {
            await using var db = new ServerDbContext();
            // return db.DynaMaps.Where(x => x.ServerIndex == -1 || x.ServerIndex == Kernel.Configuration.ServerIdentity)
            return db.DynaMaps
                .ToList();
        }
    }
}