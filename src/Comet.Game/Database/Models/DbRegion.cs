// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbRegion.cs
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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Comet.Game.Database.Models
{
    [Table("cq_region")]
    public class DbRegion
    {
        [Key] [Column("id")] public virtual uint Identity { get; set; }

        [Column("mapid")] public virtual uint MapIdentity { get; set; }
        [Column("type")] public virtual uint Type { get; set; }
        [Column("bound_x")] public virtual uint BoundX { get; set; }
        [Column("bound_y")] public virtual uint BoundY { get; set; }
        [Column("bound_cx")] public virtual uint BoundCX { get; set; }
        [Column("bound_cy")] public virtual uint BoundCY { get; set; }
        [Column("datastr")] public virtual string DataString { get; set; }
        [Column("data0")] public virtual uint Data0 { get; set; }
        [Column("data1")] public virtual uint Data1 { get; set; }
        [Column("data2")] public virtual uint Data2 { get; set; }
        [Column("data3")] public virtual uint Data3 { get; set; }
        [Column("actionid")] public virtual uint ActionId { get; set; } // 2015-01-13

        public static async Task<List<DbRegion>> GetAsync(uint idMap)
        {
            await using ServerDbContext ctx = new ServerDbContext();
            return await ctx.Regions.Where(x => x.MapIdentity == idMap).ToListAsync();
        }
    }
}