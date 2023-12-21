// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbGameLoginRecord.cs
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

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comet.Game.Database.Models
{
    [Table("cq_login_rcd")]
    public class DbGameLoginRecord
    {
        [Key][Column("id")]public virtual uint Identity { get; protected set; }
        [Column("account_id")] public virtual uint AccountIdentity { get; set; }
        [Column("user_id")] public virtual uint UserIdentity { get; set; }
        [Column("session_secs")] public virtual uint OnlineTime { get; set; }
        [Column("login_time")] public virtual DateTime LoginTime { get; set; }
        [Column("logout_time")] public virtual DateTime LogoutTime { get; set; }
        [Column("server_version")] public virtual string ServerVersion { get; set; }
        [Column("mac_addr")] public virtual string MacAddress { get; set; }
        [Column("ip_addr")] public virtual string IpAddress { get; set; }
    }
}