// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - CharactersRepository.cs
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
    /// <summary>
    ///     Repository for defining data access layer (DAL) logic for the character table. Allows
    ///     the server to fetch character information for player login. Characters are fetched
    ///     on demand, and character names are indexed for fast lookup during character creation.
    /// </summary>
    public static class CharactersRepository
    {
        /// <summary>
        ///     Fetches a character record from the database using the character's name as a
        ///     unique key for selecting a single record. Character name is indexed for fast
        ///     lookup when logging in.
        /// </summary>
        /// <param name="name">Character's name</param>
        /// <returns>Returns character details from the database.</returns>
        public static async Task<DbCharacter> FindAsync(string name)
        {
            await using var db = new ServerDbContext();
            return await db.Characters
                .Where(x => x.Name == name)
                .SingleOrDefaultAsync();
        }

        /// <summary>
        ///     Fetches a character record from the database using the character's associated
        ///     AccountID as a unique key for selecting a single record.
        /// </summary>
        /// <param name="accountID">Primary key for fetching character info</param>
        /// <returns>Returns character details from the database.</returns>
        public static async Task<DbCharacter> FindAsync(uint accountID)
        {
            await using var db = new ServerDbContext();
            return await db.Characters
                .Where(x => x.AccountIdentity == accountID)
                .SingleOrDefaultAsync();
        }

        public static async Task<DbCharacter> FindByIdentityAsync(uint id)
        {
            await using var db = new ServerDbContext();
            return await db.Characters
                .Where(x => x.Identity == id)
                .SingleOrDefaultAsync();
        }

        /// <summary>Checks if a character exists in the database by name.</summary>
        /// <param name="name">Character's name</param>
        /// <returns>Returns true if the character exists.</returns>
        public static async Task<bool> ExistsAsync(string name)
        {
            await using var db = new ServerDbContext();
            return await db.Characters
                .Where(x => x.Name == name)
                .AnyAsync();
        }

        /// <summary>
        ///     Creates a new character using a character model. If the character primary key
        ///     already exists, then character creation will fail.
        /// </summary>
        /// <param name="character">Character model to be inserted to the database</param>
        public static async Task CreateAsync(DbCharacter character)
        {
            await using var db = new ServerDbContext();
            db.Characters.Add(character);
            await db.SaveChangesAsync();
        }
    }
}