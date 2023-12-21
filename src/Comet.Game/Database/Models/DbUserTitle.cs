// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbUserTitle.cs
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

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Comet.Game.Database.Models
{
    [Table("cq_user_title")]
    public class DbUserTitle
    {
        [Key] [Column("id")] public uint Identity { get; set; }

        [Column("player_id")] public uint PlayerId { get; set; }
        [Column("type")] public uint Type { get; set; }
        [Column("title_id")] public uint TitleId { get; set; }
        [Column("status")] public uint Status { get; set; }
        [Column("del_time")] public DateTime DelTime { get; set; }

        public static async Task<List<DbUserTitle>> GetAsync(uint idPlayer)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.UserTitle
                .Where(x => x.PlayerId == idPlayer && x.DelTime > DateTime.Now)
                .ToListAsync();
        }
    }
}