// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbGenerator.cs
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
    [Table("cq_generator")]
    public class DbGenerator
    {
        [Key]
        [Column("id")] public virtual uint Id { get; set; }
        [Column("mapid")] public virtual uint Mapid { get; set; }
        [Column("bound_x")] public virtual ushort BoundX { get; set; }
        [Column("bound_y")] public virtual ushort BoundY { get; set; }
        [Column("bound_cx")] public virtual ushort BoundCx { get; set; }
        [Column("bound_cy")] public virtual ushort BoundCy { get; set; }
        [Column("maxnpc")] public virtual int MaxNpc { get; set; }
        [Column("rest_secs")] public virtual int RestSecs { get; set; }
        [Column("max_per_gen")] public virtual int MaxPerGen { get; set; }
        [Column("npctype")] public virtual uint Npctype { get; set; }
        [Column("timer_begin")] public virtual int TimerBegin { get; set; }
        [Column("timer_end")] public virtual int TimerEnd { get; set; }
        [Column("born_x")] public virtual int BornX { get; set; }
        [Column("born_y")] public virtual int BornY { get; set; }
    }
}