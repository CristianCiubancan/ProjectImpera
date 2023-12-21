// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbTaskDetail.cs
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
    [Table("task_detail")]
    public class DbTaskDetail
    {
        [Key] [Column("id")] public virtual uint Identity { get; set; }
        [Column("user_id")] public virtual uint UserIdentity { get; set; }
        [Column("task_id")] public virtual uint TaskIdentity { get; set; }
        [Column("complete_flag")] public virtual ushort CompleteFlag { get; set; }
        [Column("notify_flag")] public virtual byte NotifyFlag { get; set; }
        [Column("data1")] public virtual int Data1 { get; set; }
        [Column("data2")] public virtual int Data2 { get; set; }
        [Column("data3")] public virtual int Data3 { get; set; }
        [Column("data4")] public virtual int Data4 { get; set; }
        [Column("data5")] public virtual int Data5 { get; set; }
        [Column("data6")] public virtual int Data6 { get; set; }
        [Column("data7")] public virtual int Data7 { get; set; }
        [Column("task_overtime")] public virtual uint TaskOvertime { get; set; }
        [Column("type")] public virtual uint Type { get; set; }
        [Column("max_accumulate_times")] public virtual uint MaxAccumulateTimes { get; set; }
    }
}