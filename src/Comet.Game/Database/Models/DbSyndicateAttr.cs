// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbSyndicateAttr.cs
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
    [Table("cq_synattr")]
    public class DbSyndicateAttr
    {
        [Key] [Column("id")] public virtual uint Id { get; set; }
        [Column("user_id")] public virtual uint UserIdentity { get; set; }
        [Column("syn_id")] public virtual uint SynId { get; set; }
        [Column("rank")] public virtual ushort Rank { get; set; }
        [Column("proffer")] public virtual long Proffer { get; set; }
        [Column("proffer_his")] public virtual ulong ProfferTotal { get; set; }
        [Column("proffer_emoney")] public virtual uint Emoney { get; set; }
        [Column("proffer_emoney_his")] public virtual uint EmoneyTotal { get; set; }
        [Column("proffer_pk")] public virtual int Pk { get; set; }
        [Column("proffer_pk_his")] public virtual int PkTotal { get; set; }
        [Column("proffer_edu")] public virtual uint Guide { get; set; }
        [Column("proffer_edu_his")] public virtual uint GuideTotal { get; set; }
        [Column("proffer_totem")] public virtual uint Arsenal { get; set; }
        [Column("flower")] public uint Flower { get; set; }
        [Column("flower_w")] public uint WhiteFlower { get; set; }
        [Column("flower_lily")] public uint Orchid { get; set; }
        [Column("flower_tulip")] public uint Tulip { get; set; }
        [Column("duty_time_limit")] public virtual DateTime? Expiration { get; set; }
        [Column("assistant_id")] public uint AssistantIdentity { get; set; }
        [Column("master_id")] public uint MasterId { get; set; }
        [Column("proffer_merit")] public uint Merit { get; set; }
        [Column("join_date")] public virtual DateTime JoinDate { get; set; }
        [Column("profession")] public uint Profession { get; set; }
        [Column("last_logout")] public DateTime? LastLogout { get; set; }
    }
}