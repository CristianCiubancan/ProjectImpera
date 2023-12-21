// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbQuiz.cs
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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Comet.Game.Database.Models
{
    [Table("cq_quiz")]
    public class DbQuiz
    {
        [Key]
        [Column("id")]
        public uint Identity { get; set; }
        [Column("type")] public byte Type { get; set; }
        [Column("level")] public byte Level { get; set; }
        [Column("term")] public byte Term { get; set; }
        [Column("question")] public string Question { get; set; }
        [Column("answer1")] public string Answer1 { get; set; }
        [Column("answer2")] public string Answer2 { get; set; }
        [Column("answer3")] public string Answer3 { get; set; }
        [Column("answer4")] public string Answer4 { get; set; }
        [Column("result")] public byte Result { get; set; }

        public static async Task<List<DbQuiz>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Quiz.ToListAsync();
        }
    }
}