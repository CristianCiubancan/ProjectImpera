// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbNewbieInfo.cs
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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Comet.Game.Database.Models
{
    [Table("cq_newbie_info")]
    public class DbNewbieInfo
    {
        [Key] [Column("id")] public uint Identity { get; set; }
        [Column("profession")] public uint Profession { get; set; }
        [Column("lweapon")] public uint LeftHand { get; set; }
        [Column("rweapon")] public uint RightHand { get; set; }
        [Column("head")] public uint Headgear { get; set; }
        [Column("boot")] public uint Shoes { get; set; }
        [Column("neck")] public uint Necklace { get; set; }
        [Column("ring")] public uint Ring { get; set; }
        [Column("armor")] public uint Armor { get; set; }
        [Column("item0")] public uint Item0 { get; set; }
        [Column("number0")] public uint Number0 { get; set; }
        [Column("item1")] public uint Item1 { get; set; }
        [Column("number1")] public uint Number1 { get; set; }
        [Column("item2")] public uint Item2 { get; set; }
        [Column("number2")] public uint Number2 { get; set; }
        [Column("item3")] public uint Item3 { get; set; }
        [Column("number3")] public uint Number3 { get; set; }
        [Column("item4")] public uint Item4 { get; set; }
        [Column("number4")] public uint Number4 { get; set; }
        [Column("item5")] public uint Item5 { get; set; }
        [Column("number5")] public uint Number5 { get; set; }
        [Column("item6")] public uint Item6 { get; set; }
        [Column("number6")] public uint Number6 { get; set; }
        [Column("item7")] public uint Item7 { get; set; }
        [Column("number7")] public uint Number7 { get; set; }
        [Column("item8")] public uint Item8 { get; set; }
        [Column("number8")] public uint Number8 { get; set; }
        [Column("item9")] public uint Item9 { get; set; }
        [Column("number9")] public uint Number9 { get; set; }
        [Column("magic0")] public uint Magic0 { get; set; }
        [Column("magic1")] public uint Magic1 { get; set; }
        [Column("magic2")] public uint Magic2 { get; set; }
        [Column("magic3")] public uint Magic3 { get; set; }

        public static async Task<DbNewbieInfo> GetAsync(uint prof)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.NewbieInfo.FirstOrDefaultAsync(x => x.Profession == prof);
        }
    }
}