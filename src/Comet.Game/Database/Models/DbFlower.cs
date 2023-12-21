// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbFlower.cs
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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Comet.Game.Database.Models
{
    [Table("flower")]
    public class DbFlower
    {
        [Key] [Column("id")] public uint Identity { get; set; }
        [Column("player_id")] public uint UserId { get; set; }
        [Column("flower_r")] public uint RedRose { get; set; }
        [Column("flower_w")] public uint WhiteRose { get; set; }
        [Column("flower_lily")] public uint Orchids { get; set; }
        [Column("flower_tulip")] public uint Tulips { get; set; }

        public virtual DbCharacter User { get; set; }

        public static async Task<List<DbFlower>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Flowers
                .Include(x => x.User)
                .ToListAsync();
        }
    }
}