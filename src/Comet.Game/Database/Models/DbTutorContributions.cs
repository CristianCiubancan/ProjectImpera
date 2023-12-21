// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbTutorContributions.cs
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
    [Table("cq_tutor_contributions")]
    public class DbTutorContributions
    {
        [Key]
        [Column("id")]
        public virtual uint Identity { get; set; }
        [Column("tutor_id")] public virtual uint TutorIdentity { get; set; }
        [Column("Student_id")] public virtual uint StudentIdentity { get; set; }
        [Column("God_time")] public virtual ushort GodTime { get; set; }
        [Column("Exp")] public virtual uint Experience { get; set; }
        [Column("Addlevel")] public virtual uint PlusStone { get; set; }

        public static async Task<List<DbTutorContributions>> GetStudentsAsync(uint idGuide)
        {
            await using ServerDbContext ctx = new ServerDbContext();
            return await ctx.TutorContributions
                .Where(x => x.TutorIdentity == idGuide)
                .ToListAsync();
        }

        public static async Task<DbTutorContributions> GetGuideAsync(uint idStudent)
        {
            await using ServerDbContext ctx = new ServerDbContext();
            return await ctx.TutorContributions
                .FirstOrDefaultAsync(x => x.StudentIdentity == idStudent);
        }
    }
}