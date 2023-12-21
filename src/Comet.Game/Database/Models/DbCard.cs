// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbCard.cs
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
    [Table("cq_card")]
    public class DbCard
    {
        [Key] [Column("id")] public virtual uint Identity { get; set; }
        [Column("ref_id")] public virtual uint ReferenceId { get; set; }
        [Column("account_id")] public virtual uint AccountId { get; set; }
        [Column("itemtype")] public virtual uint ItemType { get; set; }
        [Column("money")] public virtual uint Money { get; set; }
        [Column("emoney")] public virtual uint ConquerPoints { get; set; }
        [Column("emoney_mono")] public virtual uint ConquerPointsMono { get; set; }
        [Column("flag")] public virtual uint Flag { get; set; }
        [Column("timestamp")] public virtual DateTime? Timestamp { get; set; }

        public static async Task<List<DbCard>> GetAsync(uint accountId)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Cards.Where(x => x.AccountId == accountId && x.Flag == 0 && x.Timestamp == null).ToListAsync();
        }

        public static async Task<int> CountAsync(uint account)
        {
            await using var db = new ServerDbContext();
            return await db.Cards.CountAsync(x => x.AccountId== account && x.Flag == 0 && x.Timestamp == null);
        }
    }
}