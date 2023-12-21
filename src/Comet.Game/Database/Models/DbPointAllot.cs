// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbPointAllot.cs
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
    [Table("cq_point_allot")]
    public class DbPointAllot
    {
        [Key] [Column("id")] public virtual uint Identity { get; set; }

        [Column("profession")] public virtual byte Profession { get; set; }
        [Column("level")] public virtual byte Level { get; set; }
        [Column("force")] public virtual ushort Strength { get; set; }
        [Column("speed")] public virtual ushort Agility { get; set; }
        [Column("health")] public virtual ushort Vitality { get; set; }
        [Column("soul")] public virtual ushort Spirit { get; set; }
    }
}