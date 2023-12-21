// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbTrapType.cs
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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

#endregion

namespace Comet.Game.Database.Models
{
    [Table("cq_traptype")]
    public class DbTrapType
    {
        [Key]
        [Column("id")]  public virtual uint Id { get; set; }
        [Column("sort")] public virtual uint Sort { get; set; }
        [Column("look")] public virtual uint Look { get; set; }
        [Column("action_id")] public virtual uint ActionId { get; set; }
        [Column("level")] public virtual byte Level { get; set; }
        [Column("attack_max")] public virtual int AttackMax { get; set; }
        [Column("attack_min")] public virtual int AttackMin { get; set; }
        [Column("dexterity")] public virtual int Dexterity { get; set; }
        [Column("attack_speed")] public virtual int AttackSpeed { get; set; }
        [Column("active_times")] public virtual int ActiveTimes { get; set; }
        [Column("magic_type")] public virtual ushort MagicType { get; set; }
        [Column("magic_hitrate")] public virtual int MagicHitrate { get; set; }
        [Column("size")] public virtual int Size { get; set; }
        [Column("atk_mode")] public virtual int AtkMode { get; set; }

        public static async Task<DbTrapType> GetAsync(uint id)
        {
            await using ServerDbContext ctx = new ServerDbContext();
            return await ctx.TrapTypes.FindAsync(id);
        }
    }
}