// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbTask.cs
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
    [Table("cq_task")]
    public class DbTask
    {
        [Key] [Column("id")] public virtual uint Id { get; set; }
        [Column("id_next")] public virtual uint IdNext { get; set; }
        [Column("id_nextfail")] public virtual uint IdNextfail { get; set; }
        [Column("itemname1")] public virtual string Itemname1 { get; set; }
        [Column("itemname2")] public virtual string Itemname2 { get; set; }
        [Column("money")] public virtual uint Money { get; set; }
        [Column("profession")] public virtual uint Profession { get; set; }
        [Column("sex")] public virtual uint Sex { get; set; }
        [Column("min_pk")] public virtual int MinPk { get; set; }
        [Column("max_pk")] public virtual int MaxPk { get; set; }
        [Column("team")] public virtual uint Team { get; set; }
        [Column("metempsychosis")] public virtual uint Metempsychosis { get; set; }
        [Column("query")] public virtual ushort Query { get; set; }
        [Column("marriage")] public virtual short Marriage { get; set; }
        [Column("client_active")] public virtual ushort ClientActive { get; set; }
    }
}