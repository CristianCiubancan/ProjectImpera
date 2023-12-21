// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbMonsterMagic.cs
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
    [Table("cq_monster_magic")]
    public class DbMonsterMagic
    {
        [Key] [Column("id")] public virtual uint Identity { get; set; }

        [Column("monster_id")] public virtual uint OwnerIdentity { get; set; }
        [Column("magic_id")] public virtual ushort MagicIdentity { get; set; }
        [Column("magic_level")] public virtual byte MagicLevel { get; set; }
        [Column("chance")] public virtual uint Chance { get; set; }

        public static async Task<List<DbMonsterMagic>> GetAsync()
        {
            await using ServerDbContext ctx = new ServerDbContext();
            return await ctx.MonsterMagics.ToListAsync();
        }
    }
}