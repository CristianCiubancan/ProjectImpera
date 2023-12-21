// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbTrade.cs
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
    [Table("cq_trade")]
    public class DbTrade
    {
        [Key] [Column("id")] public virtual uint Identity { get; set; }
        [Column("user_id")] public virtual uint UserIdentity { get; set; }
        [Column("target_id")] public virtual uint TargetIdentity { get; set; }
        [Column("user_money")] public virtual uint UserMoney { get; set; }
        [Column("target_money")] public virtual uint TargetMoney { get; set; }
        [Column("user_emoney")] public virtual uint UserEmoney { get; set; }
        [Column("target_emoney")] public virtual uint TargetEmoney { get; set; }
        [Column("map_id")] public virtual uint MapIdentity { get; set; }
        [Column("user_x")] public virtual ushort UserX { get; set; }
        [Column("user_y")] public virtual ushort UserY { get; set; }
        [Column("target_x")] public virtual ushort TargetX { get; set; }
        [Column("target_y")] public virtual ushort TargetY { get; set; }
        [Column("timestamp")] public virtual DateTime Timestamp { get; set; }
        [Column("user_ip_addr")] public virtual string UserIpAddress { get; set; }
        [Column("user_mac_addr")] public virtual string UserMacAddress { get; set; }
        [Column("target_ip_addr")] public virtual string TargetIpAddress { get; set; }
        [Column("target_mac_addr")] public virtual string TargetMacAddress { get; set; }
    }
}