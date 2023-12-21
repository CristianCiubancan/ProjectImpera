// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbItemOwnerHistory.cs
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
    [Table("cq_item_owner_history")]
    public class DbItemOwnerHistory
    {
        [Key] [Column("id")] public virtual uint Identity { get; set; }
        [Column("item_id")] public virtual uint ItemIdentity { get; set; }
        [Column("old_owner_id")] public virtual uint OldOwnerIdentity { get; set; }
        [Column("new_owner_id")] public virtual uint NewOwnerIdentity { get; set; }
        [Column("change_time")] public virtual DateTime Time { get; set; }
        [Column("operation")] public virtual byte Operation { get; set; }
    }
}