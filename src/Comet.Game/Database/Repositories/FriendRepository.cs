// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - FriendRepository.cs
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

#endregion

namespace Comet.Game.Database.Repositories
{
    public static class FriendRepository
    {
        public static async Task<List<DbFriend>> GetAsync(uint idUser)
        {
            await using var db = new ServerDbContext();
            return db.Friends.Where(x => x.UserIdentity == idUser).ToList();
        }

        public static async Task<DbFriend> GetAsync(uint user, uint target)
        {
            await using var db = new ServerDbContext();
            return db.Friends.FirstOrDefault(x => x.UserIdentity == user && x.TargetIdentity == target);
        }
    }
}