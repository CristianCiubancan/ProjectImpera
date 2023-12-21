// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbFamily.cs
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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Comet.Game.Database.Models
{
    [Table("family")]
    public class DbFamily
    {
        [Key]
        [Column("id")] public uint Identity { get; set; }
        [Column("family_name")] public string Name { get; set; }
        [Column("rank")] public byte Rank { get; set; }
        [Column("lead_id")] public uint LeaderIdentity { get; set; }
        [Column("annouce")] public string Announcement { get; set; }
        [Column("money")] public ulong Money { get; set; }
        [Column("repute")] public uint Repute { get; set; }
        [Column("amount")] public byte Amount { get; set; }
        [Column("enemy_family_0_id")] public uint EnemyFamily0 { get; set; }
        [Column("enemy_family_1_id")] public uint EnemyFamily1 { get; set; }
        [Column("enemy_family_2_id")] public uint EnemyFamily2 { get; set; }
        [Column("enemy_family_3_id")] public uint EnemyFamily3 { get; set; }
        [Column("enemy_family_4_id")] public uint EnemyFamily4 { get; set; }
        [Column("ally_family_0_id")] public uint AllyFamily0 { get; set; }
        [Column("ally_family_1_id")] public uint AllyFamily1 { get; set; }
        [Column("ally_family_2_id")] public uint AllyFamily2 { get; set; }
        [Column("ally_family_3_id")] public uint AllyFamily3 { get; set; }
        [Column("ally_family_4_id")] public uint AllyFamily4 { get; set; }
        [Column("create_date")] public DateTime CreationDate { get; set; }
        [Column("create_name")] public string CreateName { get; set; }
        [Column("challenge_map")] public uint ChallengeMap { get; set; }
        [Column("family_map")] public uint FamilyMap { get; set; }
        [Column("star_tower")] public byte StarTower { get; set; }
        [Column("challenge")] public uint Challenge { get; set; }
        [Column("occupy")] public uint Occupy { get; set; }
        [Column("del_flag")] public DateTime? DeleteDate { get; set; }

        public static async Task<List<DbFamily>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Families.ToListAsync();
        }
    }
}