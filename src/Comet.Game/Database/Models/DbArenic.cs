// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbArenic.cs
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
    [Table("cq_arenic")]
    public class DbArenic
    {
        [Key] [Column("id")] public uint Identity { get; set; }
        [Column("type")] public byte Type { get; set; }
        [Column("date")] public DateTime Date { get; set; }
        [Column("user_id")] public uint UserId { get; set; }
        [Column("athlete_point")] public uint AthletePoint { get; set; }
        [Column("cur_honor")] public uint CurrentHonor { get; set; }
        [Column("history_honor")] public uint HistoryHonor { get; set; }
        [Column("day_wins")] public uint DayWins { get; set; }
        [Column("day_loses")] public uint DayLoses { get; set; }
        [Column("asyn")] public ushort Asyn { get; set; }

        public virtual DbCharacter User { get; set; }

        public static async Task<List<DbArenic>> GetAsync(DateTime date)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Arenics
                .Where(x => x.Date == date.Date)
                .ToListAsync();
        }

        public static async Task<List<DbArenic>> GetRankAsync(int from, int limit = 10)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Arenics
                .Where(x => x.Date == DateTime.Now.Date)
                .OrderByDescending(x => x.AthletePoint)
                .ThenByDescending(x => x.DayWins)
                .ThenBy(x => x.DayLoses)
                .Skip(from)
                .Take(limit)
                .Include(x => x.User)
                .ToListAsync();
        }

        public static async Task<int> GetRankCountAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Arenics
                .Where(x => x.Date == DateTime.Now.Date)
                .OrderByDescending(x => x.AthletePoint)
                .ThenByDescending(x => x.DayWins)
                .ThenBy(x => x.DayLoses)
                .CountAsync();
        }

        public static async Task<List<DbArenic>> GetSeasonRankAsync(DateTime date)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Arenics
                .Where(x => x.Date == date.Date)
                .OrderByDescending(x => x.AthletePoint)
                .ThenByDescending(x => x.DayWins)
                .ThenBy(x => x.DayLoses)
                .Take(10)
                .Include(x => x.User)
                .ToListAsync();
        }

        public static async Task<int> GetSeasonRankCountAsync(DateTime date)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Arenics
                .Where(x => x.Date == date.Date)
                .OrderByDescending(x => x.AthletePoint)
                .ThenByDescending(x => x.DayWins)
                .ThenBy(x => x.DayLoses)
                .CountAsync();
        }
    }
}