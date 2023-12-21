using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Comet.Game.Database.Models
{
    [Table("cq_pk_item")]
    public class DbDetainedItem
    {
        [Key]
        [Column("id")]
        public virtual uint Identity { get; set; }
        [Column("item")]
        public virtual uint ItemIdentity { get; set; }
        [Column("target")]
        public virtual uint TargetIdentity { get; set; }
        [Column("target_name")]
        public virtual string TargetName { get; set; }
        [Column("hunter")]
        public virtual uint HunterIdentity { get; set; }
        [Column("hunter_name")]
        public virtual string HunterName { get; set; }
        [Column("manhunt_time")]
        public virtual int HuntTime { get; set; }
        [Column("bonus")]
        public virtual ushort RedeemPrice { get; set; }

        public static async Task<List<DbDetainedItem>> GetFromHunterAsync(uint hunter)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.DetainedItems.Where(x => x.HunterIdentity == hunter).ToListAsync();
        }

        public static async Task<List<DbDetainedItem>> GetFromDischargerAsync(uint target)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.DetainedItems.Where(x => x.TargetIdentity == target).ToListAsync();
        }

        public static async Task<DbDetainedItem> GetByIdAsync(uint id)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.DetainedItems.FirstOrDefaultAsync(x => x.Identity == id);
        }

        public static async Task<List<DbDetainedItem>> GetByItemAsync(uint idItem)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.DetainedItems.Where(x => x.ItemIdentity == idItem).ToListAsync();
        }
    }
}
