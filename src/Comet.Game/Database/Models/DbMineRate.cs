// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbMineRate.cs
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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Comet.Game.Database.Models
{
    [Table("cq_mine_rate")]
    public class DbMineRate
    {
        [Key][Column("id")] public virtual uint Identity { get; set; }
        [Column("map_id")] public virtual uint MapIdentity { get; set; }
        [Column("req_lev")] public virtual byte RequiredLevel { get; set; }
        [Column("req_pro")] public virtual byte RequiredProfession { get; set; }
        [Column("req_money")] public virtual uint RequiredMoney { get; set; }
        [Column("req_emoney")] public virtual uint RequiredEmoney { get; set; }
        [Column("req_itemtype")] public virtual uint RequiredItemtype { get; set; }
        [Column("req_itemname")] public virtual string RequiredItemName { get; set; }
        [Column("req_task_id")] public virtual uint RequiredTaskIdentity { get; set; }
        [Column("req_task_complete")] public virtual bool RequiredTaskCompletion { get; set; }
        [Column("chance_x")] public virtual uint ChanceX { get; set; }
        [Column("chance_y")] public virtual uint ChanceY { get; set; }
        [Column("itemtype_begin")] public virtual uint ItemtypeBegin { get; set; }
        [Column("itemtype_end")] public virtual uint ItemtypeEnd { get; set; }
        [Column("timeout")] public virtual uint TimeOut { get; set; }
        [Column("level")] public virtual byte Level { get; set; }

        public static async Task<List<DbMineRate>> GetAsync()
        {
            await using ServerDbContext ctx = new ServerDbContext();
            return await ctx.MineRates
                .OrderBy(x => x.MapIdentity)
                .ToListAsync();
        }
    }
}