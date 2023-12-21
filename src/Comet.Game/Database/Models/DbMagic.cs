// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbMagic.cs
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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comet.Game.Database.Models
{
    [Table("cq_magic")]
    public class DbMagic
    {
        [Key]
        [Column("id")] public virtual uint Id { get; set; }
        [Column("ownerid")] public virtual uint OwnerId { get; set; }
        [Column("type")] public virtual ushort Type { get; set; }
        [Column("level")] public virtual ushort Level { get; set; }
        [Column("exp")] public virtual uint Experience { get; set; }
        [Column("unlearn")] public virtual byte Unlearn { get; set; }
        [Column("old_level")] public virtual ushort OldLevel { get; set; }
    }
}