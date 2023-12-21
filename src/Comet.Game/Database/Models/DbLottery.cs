// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbLottery.cs
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
    [Table("cq_lottery")]
    public class DbLottery
    {
        [Key] [Column("id")] public virtual uint Identity { get; set; }

        [Column("type")] public virtual byte Type { get; set; }
        [Column("rank")] public virtual byte Rank { get; set; }
        [Column("chance")] public virtual byte Chance { get; set; }
        [Column("prize_name")] public virtual string Itemname { get; set; }
        [Column("prize_item")] public virtual uint ItemIdentity { get; set; }
        [Column("color")] public virtual byte Color { get; set; }
        [Column("hole_num")] public virtual byte SocketNum { get; set; }
        [Column("addition_lev")] public virtual byte Plus { get; set; }

        public static async Task<List<DbLottery>> GetAsync()
        {
            await using ServerDbContext ctx = new ServerDbContext();
            return await ctx.Lottery.ToListAsync();
        }
    }
}