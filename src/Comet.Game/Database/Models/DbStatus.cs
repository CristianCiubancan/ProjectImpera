// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbStatus.cs
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
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Comet.Game.Database.Models
{
    [Table("cq_status")]
    public class DbStatus
    {
        [Key] [Column("id")] public virtual uint Id { get; set; }
        [Column("owner_id")] public virtual uint OwnerId { get; set; }
        [Column("status")] public virtual uint Status { get; set; }
        [Column("power")] public virtual int Power { get; set; }
        [Column("sort")] public virtual uint Sort { get; set; }
        [Column("leave_times")] public virtual uint LeaveTimes { get; set; }
        [Column("remain_time")] public virtual uint RemainTime { get; set; }
        [Column("end_time")] public virtual DateTime EndTime { get; set; }
        [Column("interval_time")] public virtual uint IntervalTime { get; set; }

        public static async Task<List<DbStatus>> GetAsync(uint idUser)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Status.Where(x => x.OwnerId == idUser).ToListAsync();
        }
    }
}