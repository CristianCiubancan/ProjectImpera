// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbFamilyAttr.cs
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
    [Table("family_attr")]
    public class DbFamilyAttr
    {
        [Key] [Column("id")] public uint Identity { get; set; }
        [Column("user_id")] public uint UserIdentity { get; set; }
        [Column("family_id")] public uint FamilyIdentity { get; set; }
        [Column("rank")] public byte Rank { get; set; }
        [Column("proffer")] public uint Proffer { get; set; }
        [Column("join_date")] public DateTime JoinDate { get; set; }
        [Column("auto_exercise")] public byte AutoExercise { get; set; }
        [Column("exp_date")] public uint ExpDate { get; set; }

        public static async Task<List<DbFamilyAttr>> GetAsync(uint idFamily)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.FamilyAttrs.Where(x => x.FamilyIdentity == idFamily).ToListAsync();
        }
    }
}