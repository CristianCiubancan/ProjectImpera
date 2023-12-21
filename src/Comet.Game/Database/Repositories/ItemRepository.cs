// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - ItemRepository.cs
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
    public static class ItemRepository
    {
        public static async Task<List<DbItem>> GetAsync(uint idUser)
        {
            await using var db = new ServerDbContext();
            return db.Items.Where(x => x.PlayerId == idUser).ToList();
        }

        public static async Task<DbItem> GetByIdAsync(uint idItem)
        {
            await using var db = new ServerDbContext();
            return await db.Items.FirstOrDefaultAsync(x => x.Id == idItem);
        }

        public static async Task<List<DbItem>> GetBySyndicateAsync(uint idSyndicate)
        {
            await using var db = new ServerDbContext();
            return db.Items.Where(x => x.Syndicate == idSyndicate).ToList();
        }
    }
}