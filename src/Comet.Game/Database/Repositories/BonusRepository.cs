// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - BonusRepository.cs
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

using System.Linq;
using System.Threading.Tasks;
using Comet.Game.Database.Models;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Comet.Game.Database.Repositories
{
    public static class BonusRepository
    {
        public static async Task<DbBonus> GetAsync(uint account)
        {
            await using var db = new ServerDbContext();
            return await db.Bonus.FirstOrDefaultAsync(x => x.AccountIdentity == account && x.Flag == 0 && x.Time == null);
        }

        public static async Task<int> CountAsync(uint account)
        {
            await using var db = new ServerDbContext();
            return await db.Bonus.CountAsync(x => x.AccountIdentity == account && x.Flag == 0 && x.Time == null);
        }
    }
}