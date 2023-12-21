namespace Comet.Game.Database
{
    using Microsoft.EntityFrameworkCore;
    using Comet.Game.Database.Models;
    using System.Threading.Tasks;
    using System.Data;
    using System;

    /// <summary>
    /// Server database client context implemented using Entity Framework Core, an open
    /// source object-relational mapping framework for ADO.NET. Substitutes in MySQL 
    /// support through a third-party framework provided by Pomelo Foundation.
    /// </summary>
    public partial class ServerDbContext : DbContext
    {
        // Connection String Configuration
        public static ServerConfiguration.DatabaseConfiguration Configuration;
        public virtual DbSet<DbCharacter> Characters { get; set; }
        public virtual DbSet<DbMap> Maps { get; set; }
        public virtual DbSet<DbDynamap> DynaMaps { get; set; }
        public virtual DbSet<DbItemAddition> ItemAdditions { get; set; }
        public virtual DbSet<DbItem> Items { get; set; }
        public virtual DbSet<DbItemtype> Itemtypes { get; set; }
        public virtual DbSet<DbPointAllot> PointAllot { get; set; }
        public virtual DbSet<DbWeaponSkill> WeaponSkills { get; set; }
        public virtual DbSet<DbPeerage> Peerage { get; set; }
        public virtual DbSet<DbMonstertype> Monstertype { get; set; }
        public virtual DbSet<DbMonsterMagic> MonsterMagics { get; set; }
        public virtual DbSet<DbMonsterKill> MonsterKills { get; set; }
        public virtual DbSet<DbGenerator> Generator { get; set; }
        public virtual DbSet<DbPassway> Passway { get; set; }
        public virtual DbSet<DbPortal> Portal { get; set; }
        public virtual DbSet<DbGameLoginRecord> LoginRcd { get; set; }
        public virtual DbSet<DbLevelExperience> LevelExperience { get; set; }
        public virtual DbSet<DbMagictype> Magictype { get; set; }
        public virtual DbSet<DbMagic> Magic { get; set; }
        public virtual DbSet<DbGoods> Goods { get; set; }
        public virtual DbSet<DbTask> Tasks { get; set; }
        public virtual DbSet<DbAction> Actions { get; set; }
        public virtual DbSet<DbNpc> Npcs { get; set; }
        public virtual DbSet<DbDynanpc> DynaNpcs { get; set; }
        public virtual DbSet<DbFriend> Friends { get; set; }
        public virtual DbSet<DbEnemy> Enemies { get; set; }
        public virtual DbSet<DbSyndicate> Syndicates { get; set; }
        public virtual DbSet<DbSyndicateAttr> SyndicatesAttr { get; set; }
        public virtual DbSet<DbSyndicateAllies> SyndicatesAlly { get; set; }
        public virtual DbSet<DbSyndicateEnemy> SyndicatesEnemy { get; set; }
        public virtual DbSet<DbSyndicateMemberHistory> SyndicateMemberHistories { get; set; }
        public virtual DbSet<DbStatistic> Statistic { get; set; }
        public virtual DbSet<DbBonus> Bonus { get; set; }
        public virtual DbSet<DbMagictypeOp> MagictypeOps { get; set; }
        public virtual DbSet<DbRebirth> Rebirths { get; set; }
        public virtual DbSet<DbStatus> Status { get; set; }
        public virtual DbSet<DbTaskDetail> TaskDetail { get; set; }
        public virtual DbSet<DbTrade> Trade { get; set; }
        public virtual DbSet<DbTradeItem> TradeItem { get; set; }
        public virtual DbSet<DbItemOwnerHistory> ItemOwnerHistory { get; set; }
        public virtual DbSet<DbMessageLog> MessageLog { get; set; }
        public virtual DbSet<DbMineRate> MineRates { get; set; }
        public virtual DbSet<DbPigeon> Pigeons { get; set; }
        public virtual DbSet<DbPigeonQueue> PigeonQueues { get; set; }
        public virtual DbSet<DbRegion> Regions { get; set; }
        public virtual DbSet<DbBusiness> Business { get; set; }
        public virtual DbSet<DbLottery> Lottery { get; set; }
        public virtual DbSet<DbTrap> Traps { get; set; }
        public virtual DbSet<DbTrapType> TrapTypes { get; set; }
        public virtual DbSet<DbTutor> Tutor { get; set; }
        public virtual DbSet<DbTutorAccess> TutorAccess { get; set; }
        public virtual DbSet<DbTutorBattleLimitType> TutorBattleLimitTypes { get; set; }
        public virtual DbSet<DbTutorContributions> TutorContributions { get; set; }
        public virtual DbSet<DbTutorType> TutorTypes { get; set; }
        public virtual DbSet<DbDynaRankRec> DynaRankRec { get; set; }
        public virtual DbSet<DbSuperman> Superman { get; set; }
        public virtual DbSet<DbDisdain> Disdains { get; set; }
        public virtual DbSet<DbTotemAdd> TotemAdds { get; set; }
        public virtual DbSet<DbFlower> Flowers { get; set; }
        public virtual DbSet<DbNewbieInfo> NewbieInfo { get; set; }
        public virtual DbSet<DbArenic> Arenics { get; set; }
        public virtual DbSet<DbQuiz> Quiz { get; set; }
        public virtual DbSet<DbUserTitle> UserTitle { get; set; }
        public virtual DbSet<DbFamily> Families { get; set; }
        public virtual DbSet<DbFamilyAttr> FamilyAttrs { get; set; }
        public virtual DbSet<DbFamilyBattleEffectShareLimit> FamilyBattleEffectShareLimits { get; set; }
        public virtual DbSet<DbCard> Cards { get; set; }
        public virtual DbSet<DbDetainedItem> DetainedItems { get; set; }
        

        /// <summary>
        /// Configures the database to be used for this context. This method is called
        /// for each instance of the context that is created. For this project, the MySQL
        /// connector will be initialized with a connection string from the server's
        /// configuration file.
        /// </summary>
        /// <param name="options">Builder to create the context</param>
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseLazyLoadingProxies(false);
            options.UseMySql(string.Format("server={0};database={1};user={2};password={3}",
                ServerDbContext.Configuration.Hostname,
                ServerDbContext.Configuration.Schema,
                ServerDbContext.Configuration.Username,
                ServerDbContext.Configuration.Password),
                MySqlServerVersion.LatestSupportedServerVersion);
        }

        /// <summary>
        /// Typically called only once when the first instance of the context is created.
        /// Allows for model building before the context is fully initialized, and used
        /// to initialize composite keys and relationships.
        /// </summary>
        /// <param name="builder">Builder for creating models in the context</param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<DbCharacter>(e => e.HasKey(x => x.Identity));
        }

        /// <summary>
        /// Tests that the database connection is alive and configured.
        /// </summary>
        public static bool Ping()
        {
            try
            {
                using ServerDbContext ctx = new ServerDbContext();
                return ctx.Database.CanConnect();
            }
            catch { return false; }
        }

                public async Task<DataTable> SelectAsync(string query)
        {
            var result = new DataTable();
            var connection = Database.GetDbConnection();
            var state = connection.State;

            try
            {
                if (state != ConnectionState.Open)
                    await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = query;
                command.CommandType = CommandType.Text;

                await using var reader = await command.ExecuteReaderAsync();
                result.Load(reader);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (state != ConnectionState.Closed)
                    await connection.CloseAsync();
            }

            return result;
        }
    }
}
