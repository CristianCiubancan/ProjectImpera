// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Automatic Actions Processing.cs
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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.States;
using Comet.Game.States.Events;
using Comet.Shared;
using Comet.Shared.Comet.Shared;

#endregion

namespace Comet.Game.World.Threading
{
    public sealed class AutomaticActionsProcessing : TimerBase
    {
        private const int _ACTION_SYSTEM_EVENT = 2030000;
        private const int _ACTION_SYSTEM_EVENT_LIMIT = 9999;

        private readonly ConcurrentDictionary<uint, DbAction> m_dicActions;

        private int m_dailyReset = 0;

        public AutomaticActionsProcessing() 
            : base(60000, "AutomaticActionsProcessing")
        {
            m_dicActions = new ConcurrentDictionary<uint, DbAction>(1, _ACTION_SYSTEM_EVENT_LIMIT);
        }

        protected override Task OnStartAsync()
        {
            for (int a = 0; a < _ACTION_SYSTEM_EVENT_LIMIT; a++)
            {
                DbAction action = Kernel.EventManager.GetAction((uint)(_ACTION_SYSTEM_EVENT + a));
                if (action != null)
                    m_dicActions.TryAdd(action.Identity, action);
            }

            m_interval = CalculateInterval();
            return base.OnStartAsync();
        }

        protected override async Task<bool> OnElapseAsync()
        {
            foreach (var action in m_dicActions.Values)
            {
                try
                {
                    await GameAction.ExecuteActionAsync(action.Identity, null, null, null, "");
                }
                catch (Exception ex)
                {
                    await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                }
            }

            if (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0 && m_dailyReset != int.Parse(DateTime.Now.ToString("yyyyMMdd")))
            {
                _ = Task.Run(DailyResetAsync).ConfigureAwait(false);
            }

            m_interval = CalculateInterval();
            return true;
        }

        public async Task DailyResetAsync()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            ArenaQualifier qualifier = Kernel.EventThread.GetEvent<ArenaQualifier>();

            uint today = uint.Parse(DateTime.Now.ToString("yyyyMMdd"));
            var users = await DbCharacter.GetDailyResetAsync();
            for (int i = users.Count - 1; i >= 0; i--)
            {
                try
                {
                    DbCharacter dbUser = users[i];

                    Character player = Kernel.RoleManager.GetUser(dbUser.Identity);
                    player ??= new Character(dbUser, null);

                    // arena
                    if (qualifier != null)
                    {
                        if (qualifier.GetReward(player.Identity, out var reward))
                        {
                            player.HonorPoints += (uint)reward.Honor;
                            player.HistoryHonorPoints += (uint)reward.Honor;
                        }
                    }

                    player.QualifierPoints = ArenaQualifier.GetInitialPoints(dbUser.Level);
                    player.QualifierDayWins = 0;
                    player.QualifierDayLoses = 0;

                    player.DayResetDate = today;

                    await player.SaveAsync();
                    Console.WriteLine($"Daily Reset [{dbUser.Name:-16}] done.");
                }
                catch (Exception ex)
                {
                    await Log.WriteLogAsync(LogLevel.Error, $"Daily Reset [{users[i].Name:-16}] error.");
                    await Log.GmLogAsync("daily_reset_err", ex.ToString());
                }
            }

            await Kernel.FlowerManager.DailyResetAsync().ConfigureAwait(false);

            sw.Stop();
            await BaseRepository.ScalarAsync($"INSERT INTO `daily_reset` (run_time, ms) VALUES (NOW(), {sw.ElapsedMilliseconds});");
            Console.WriteLine($"Daily reset has run in {sw.ElapsedMilliseconds}ms.");

            m_dailyReset = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
        }

        private int CalculateInterval()
        {
            DateTime now = DateTime.Now;

            DateTime future = now.AddSeconds(60 - now.Second % 60).AddMilliseconds(now.Millisecond * -1);
            TimeSpan interval0 = future - now;
            return (int)interval0.TotalMilliseconds;
        }
    }
}