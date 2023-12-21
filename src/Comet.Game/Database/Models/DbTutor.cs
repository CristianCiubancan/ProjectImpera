// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbTutor.cs
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
    [Table("cq_tutor")]
    public class DbTutor
    {
        [Key]
        [Column("id")]
        public virtual uint Identity { get; set; }
        [Column("tutor_id")] public virtual uint GuideId { get; set; }
        [Column("Student_id")] public virtual uint StudentId { get; set; }
        [Column("Betrayal_flag")] public virtual int BetrayalFlag { get; set; }
        [Column("Date")] public virtual DateTime? Date { get; set; }

        public virtual DbCharacter Guide { get; set; }

        public virtual DbCharacter Student { get; set; }

        public static async Task<DbTutor> GetAsync(uint idStudent)
        {
            await using ServerDbContext ctx = new ServerDbContext();
            return await ctx.Tutor
                .Include(x => x.Guide)
                .Include(x => x.Student)
                .FirstOrDefaultAsync(x => x.StudentId == idStudent);
        }

        public static async Task<List<DbTutor>> GetStudentsAsync(uint idTutor)
        {
            await using ServerDbContext ctx = new ServerDbContext();
            return await ctx.Tutor
                .Include(x => x.Guide)
                .Include(x => x.Student)
                .Where(x => x.GuideId == idTutor)
                .ToListAsync();
        }
    }
}