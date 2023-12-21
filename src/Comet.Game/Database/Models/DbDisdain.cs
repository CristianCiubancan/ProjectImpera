// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbDisdain.cs
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
    [Table("cq_disdain")]
    public class DbDisdain
    {
        [Key] [Column("id")] public uint Identity { get; set; }
        [Column("delta_lev")] public int DeltaLev { get; set; }
        [Column("exp_factor")] public int ExpFactor { get; set; }
        [Column("max_atk")] public int MaxAtk { get; set; }
        [Column("max_xp_atk")] public int MaxXpAtk { get; set; }
        [Column("mst_atk")] public int MstAtk { get; set; }
        [Column("type")] public int Type { get; set; }
        [Column("usr_atk_mst")] public int UsrAtkMst { get; set; }
        [Column("usr_atk_usr_max")] public int UsrAtkUsrMax { get; set; }
        [Column("usr_atk_usr_min")] public int UsrAtkUsrMin { get; set; }
        [Column("usr_atk_usr_overadj")] public int UsrAtkUsrOveradj { get; set; }
        [Column("usr_atk_usrx_max")] public int UsrAtkUsrxMax { get; set; }
        [Column("usr_atk_usrx_min")] public int UsrAtkUsrxMin { get; set; }
        [Column("usr_atk_usrx_overadj")] public int UsrAtkUsrxOveradj { get; set; }
        [Column("usrx_atk_usr_max")] public int UsrxAtkUsrMax { get; set; }
        [Column("usrx_atk_usr_min")] public int UsrxAtkUsrMin { get; set; }
        [Column("usrx_atk_usr_overadj")] public int UsrxAtkUsrOveradj { get; set; }
        [Column("usrx_atk_usrx_max")] public int UsrxAtkUsrxMax { get; set; }
        [Column("usrx_atk_usrx_min")] public int UsrxAtkUsrxMin { get; set; }
        [Column("usrx_atk_usrx_overadj")] public int UsrxAtkUsrxOveradj { get; set; }
        [Column("xp_exp_factor")] public int XpExpFactor { get; set; }

        public static async Task<List<DbDisdain>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Disdains.ToListAsync();
        }
    }
}