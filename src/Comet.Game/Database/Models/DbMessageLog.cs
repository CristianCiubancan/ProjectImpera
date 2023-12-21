// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbMessageLog.cs
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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace Comet.Game.Database.Models
{
    [Table("cq_message_log")]
    public class DbMessageLog
    {
        [Key] [Column("id")] public virtual uint Identity { get; set; }
        [Column("sender_id")] public virtual uint SenderIdentity { get; set; }
        [Column("sender_name")] public virtual string SenderName { get; set; }
        [Column("target_id")] public virtual uint TargetIdentity { get; set; }
        [Column("target_name")] public virtual string TargetName { get; set; }
        [Column("channel")] public virtual ushort Channel { get; set; }
        [Column("message")] public virtual string Message { get; set; }
        [Column("date")] public virtual DateTime Time { get; set; }
    }
}