// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbItemAddition.cs
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

#endregion

namespace Comet.Game.Database.Models
{
    [Table("cq_itemaddition")]
    public class DbItemAddition
    {
        [Key] [Column("id")] public virtual uint Identity { get; set; }

        [Column("typeid")] public virtual uint TypeId { get; set; }

        [Column("level")] public virtual byte Level { get; set; }

        [Column("life")] public virtual ushort Life { get; set; }

        [Column("attack_max")] public virtual ushort AttackMax { get; set; }

        [Column("attack_min")] public virtual ushort AttackMin { get; set; }

        [Column("defense")] public virtual ushort Defense { get; set; }

        [Column("magic_atk")] public virtual ushort MagicAtk { get; set; }

        [Column("magic_def")] public virtual ushort MagicDef { get; set; }

        [Column("dexterity")] public virtual ushort Dexterity { get; set; }

        [Column("dodge")] public virtual ushort Dodge { get; set; }
    }
}