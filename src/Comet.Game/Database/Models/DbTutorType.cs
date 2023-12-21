// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbTutorType.cs
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
    [Table("cq_tutor_type")]
    public class DbTutorType
    {
        [Key]
        [Column("id")]
        public virtual uint Id { get; set; }
        /// <summary>
        /// Minimum user level for this rule. Below the lowest one can't be a mentor.
        /// </summary>
        [Column("User_lev_min")] public virtual byte UserMinLevel { get; set; }
        /// <summary>
        /// Maximum user level for this rule. After the highest one, get the higher.
        /// </summary>
        [Column("User_lev_max")] public virtual byte UserMaxLevel { get; set; }
        /// <summary>
        /// Maximum number of students a user can have.
        /// </summary>
        [Column("Student_num")] public virtual byte StudentNum { get; set; }
        /// <summary>
        /// Percentage of battle power to share.
        /// </summary>
        [Column("Battle_lev_share")] public virtual byte BattleLevelShare { get; set; }

        public static async Task<List<DbTutorType>> GetAsync()
        {
            await using ServerDbContext ctx = new ServerDbContext();
            return await ctx.TutorTypes.ToListAsync();
        }
    }
}