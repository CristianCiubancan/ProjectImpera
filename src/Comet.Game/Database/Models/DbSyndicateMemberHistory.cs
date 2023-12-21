// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbSyndicateMemberHistory.cs
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
    [Table("cq_syn_history")]
    public class DbSyndicateMemberHistory
    {
        [Key] [Column("id")] public virtual uint Identity { get; set; }
        [Column("user_id")] public virtual uint UserIdentity { get; set; }
        [Column("syn_id")] public virtual uint SyndicateIdentity { get; set; }
        [Column("join_date")] public virtual DateTime JoinDate { get; set; }
        [Column("leave_date")] public virtual DateTime LeaveDate { get; set; }
        [Column("silver")] public virtual long Silver { get; set; }
        [Column("emoney")] public virtual uint ConquerPoints { get; set; }
        [Column("pk")] public virtual uint PkPoints { get; set; }
        [Column("guide")] public virtual uint Guide { get; set; }
        [Column("rank")] public virtual ushort Rank { get; set; }
    }
}