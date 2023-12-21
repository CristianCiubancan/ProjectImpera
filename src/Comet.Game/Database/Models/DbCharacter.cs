// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbCharacter.cs
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
using System.Linq;
using System.Threading.Tasks;
using Comet.Shared;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Comet.Game.Database.Models
{
    /// <summary>
    ///     Character information associated with a player. Every player account is permitted
    ///     a single character on the server. Contains the character's defining look and features,
    ///     level and attribute information, location, etc.
    /// </summary>
    [Table("cq_user")]
    public class DbCharacter
    {
        // Column Properties
        [Key]
        [Column("id")] public virtual uint Identity { get; set; }
        [Column("account_id")] public virtual uint AccountIdentity { get; set; }
        [Column("name")] public virtual string Name { get; set; }
        [Column("mateid")] public virtual uint Mate { get; set; }
        [Column("lookface")] public virtual uint Mesh { get; set; }
        [Column("hair")] public virtual ushort Hairstyle { get; set; }
        [Column("money")] public virtual uint Silver { get; set; }
        [Column("emoney")] public virtual uint ConquerPoints { get; set; }
        [Column("money_saved")] public virtual uint StorageMoney { get; set; }
        [Column("profession")] public virtual byte Profession { get; set; }
        [Column("old_prof")] public virtual byte PreviousProfession { get; set; }
        [Column("first_prof")] public virtual byte FirstProfession { get; set; }
        [Column("metempsychosis")] public virtual byte Rebirths { get; set; }
        [Column("level")] public virtual byte Level { get; set; }
        [Column("exp")] public virtual ulong Experience { get; set; }
        [Column("recordmap_id")] public virtual uint MapID { get; set; }
        [Column("recordx")] public virtual ushort X { get; set; }
        [Column("recordy")] public virtual ushort Y { get; set; }
        [Column("virtue")] public virtual uint Virtue { get; set; }
        [Column("strength")] public virtual ushort Strength { get; set; }
        [Column("speed")] public virtual ushort Agility { get; set; }
        [Column("health")] public virtual ushort Vitality { get; set; }
        [Column("soul")] public virtual ushort Spirit { get; set; }
        [Column("additional_point")] public virtual ushort AttributePoints { get; set; }
        [Column("life")] public virtual ushort HealthPoints { get; set; }
        [Column("mana")] public virtual ushort ManaPoints { get; set; }
        [Column("pk")] public virtual ushort KillPoints { get; set; }
        [Column("creation_date")] public virtual DateTime? Registered { get; set; }
        [Column("donation")] public ulong Donation { get; set; }
        [Column("last_login")] public virtual DateTime LoginTime { get; set; }
        [Column("last_logout")] public virtual DateTime LogoutTime { get; set; }
        [Column("last_logout2")] public virtual DateTime? LogoutTime2 { get; set; } // Offline TG
        [Column("online_time")] public virtual int OnlineSeconds { get; set; }
        [Column("auto_allot")] public virtual byte AutoAllot { get; set; }
        [Column("mete_lev")] public virtual uint MeteLevel { get; set; }
        [Column("exp_ball_usage")] public virtual uint ExpBallUsage { get; set; }
        [Column("exp_ball_num")] public virtual uint ExpBallNum { get; set; }
        [Column("exp_multiply")] public virtual float ExperienceMultiplier { get; set; }
        [Column("exp_expires")] public virtual DateTime? ExperienceExpires { get; set; }
        [Column("god_status")] public virtual DateTime? HeavenBlessing { get; set; }
        [Column("task_mask")] public virtual uint TaskMask { get; set; }
        [Column("home_id")] public virtual uint HomeIdentity { get; set; }
        [Column("lock_key")] public virtual ulong LockKey { get; set; }
        [Column("auto_exercise")] public virtual ushort AutoExercise { get; set; }
        [Column("time_of_life")] public virtual DateTime? LuckyTime { get; set; }
        [Column("vip_value")] public virtual uint VipLevel { get; set; }
        [Column("vip_expire")] public virtual DateTime? VipExpiration { get; set; }
        [Column("business")] public virtual DateTime? Business { get; set; }
        [Column("send_flower_date")] public DateTime? SendFlowerDate { get; set; }
        [Column("flower_r")] public uint FlowerRed { get; set; }
        [Column("flower_w")] public uint FlowerWhite { get; set; }
        [Column("flower_lily")] public uint FlowerOrchid { get; set; }
        [Column("flower_tulip")] public uint FlowerTulip { get; set; }
        /// <summary>
        /// Experience Gained by staying online with Heaven Blessing.
        /// </summary>
        [Column("online_god_exptime")] public uint OnlineGodExpTime { get; set; }
        /// <summary>
        /// Experience gained by killing monsters in the world with Heaven Blessing.
        /// </summary>
        [Column("battle_god_exptime")] public uint BattleGodExpTime { get; set; }
        [Column("mentor_opportunity")] public uint MentorOpportunity { get; set; }
        [Column("mentor_uplev_time")] public uint MentorUplevTime { get; set; }
        [Column("mentor_achieve")] public uint MentorAchieve { get; set; }
        [Column("mentor_day")] public uint MentorDay { get; set; }
        [Column("title")] public uint Title { get; set; }
        [Column("title_select")] public byte TitleSelect { get; set; }

        [Column("athlete_point")] public uint AthletePoint { get; set; }
        [Column("athlete_history_wins")] public uint AthleteHistoryWins { get; set; }
        [Column("athlete_history_loses")] public uint AthleteHistoryLoses { get; set; }
        [Column("athlete_day_wins")] public uint AthleteDayWins { get; set; }
        [Column("athlete_day_loses")] public uint AthleteDayLoses { get; set; }
        [Column("athlete_cur_honor_point")] public uint AthleteCurrentHonorPoints { get; set; }
        [Column("athlete_hisorty_honor_point")] public uint AthleteHistoryHonorPoints { get; set; }

        [Column("quiz_point")] public uint QuizPoints { get; set; }

        [Column("day_reset_date")] public uint DayResetDate { get; set; }

        public static async Task<List<DbCharacter>> GetAthletePointRankAsync(int from, int limit)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Characters
                .Where(x => x.AthletePoint > 0)
                .OrderByDescending(x => x.AthletePoint)
                .ThenByDescending(x => x.AthleteDayWins)
                .ThenBy(x => x.AthleteDayLoses)
                .Skip(from)
                .Take(limit)
                .ToListAsync();
        }

        public static async Task<List<DbCharacter>> GetHonorRankAsync(int from, int limit)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Characters
                .Where(x => x.AthleteHistoryHonorPoints > 0)
                .OrderByDescending(x => x.AthleteHistoryHonorPoints)
                .ThenByDescending(x => x.AthleteHistoryWins)
                .ThenBy(x => x.AthleteHistoryLoses)
                .Skip(from)
                .Take(limit)
                .ToListAsync();
        }

        public static async Task<int> GetHonorRankCountAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Characters
                .Where(x => x.AthleteHistoryHonorPoints > 0)
                .OrderByDescending(x => x.AthleteHistoryHonorPoints)
                .ThenByDescending(x => x.AthleteHistoryWins)
                .ThenBy(x => x.AthleteHistoryLoses)
                .CountAsync();
        }

        public static async Task<List<DbCharacter>> GetDailyResetAsync()
        {
            try
            {
                uint now = uint.Parse(DateTime.Now.ToString("yyyyMMdd"));
                await using var ctx = new ServerDbContext();
                return await ctx
                    .Characters
                    .Where(x => x.Identity > 1000000 
                    && x.Registered != null
                    && x.Registered.Value.Date != DateTime.Now.Date
                    && x.DayResetDate != now).ToListAsync();
            }
            catch (Exception ex)
            {
                // await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                Console.WriteLine(ex.ToString());
                return new List<DbCharacter>();
            }
        }
    }
}