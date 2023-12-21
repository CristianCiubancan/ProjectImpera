// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbBusiness.cs
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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Comet.Game.Database.Models
{
    [Table("cq_business")]
    public class DbBusiness
    {
        [Key] [Column("id")] public virtual uint Identity { get; set; }
        [Column("userid")] public virtual uint UserId { get; set; }
        [Column("business")] public virtual uint BusinessId { get; set; }
        [Column("date")] public virtual DateTime Date { get; set; }

        public virtual DbCharacter User { get; set; }
        public virtual DbCharacter Business { get; set; }

        public static async Task<List<DbBusiness>> GetAsync(uint sender)
        {
            await using ServerDbContext ctx = new ServerDbContext();
            return await ctx.Business.Where(x => x.UserId == sender || x.BusinessId == sender)
                .Include(x => x.User)
                .Include(x => x.Business)
                .ToListAsync();
        }
    }
}