// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Basic Processing.cs
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
using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.Internal;
using Comet.Shared;
using Comet.Shared.Comet.Shared;
using Comet.Shared.Models;

#endregion

namespace Comet.Game.World.Threading
{
    public sealed class SystemProcessor : TimerBase
    {
        public const string TITLE_FORMAT_S = @"[{0}] - Conquer Online Game Server [{8}] - {1} - Players: {3} (max:{4}) - {2} - Threads[S:{5:0000},U:{6:0000},A:{7:0000}]ms";

        private TimeOut m_analytics = new TimeOut(300);
        private TimeOut m_accountPing = new TimeOut(5);
        private DateTime m_serverStartTime;

        public SystemProcessor()
            : base(1000, "System Thread")
        {
        }

        protected override Task OnStartAsync()
        {
            m_serverStartTime = DateTime.Now;
            m_analytics.Update();

            return base.OnStartAsync();
        }

        protected override async Task<bool> OnElapseAsync()
        {
            // Console.Title = string.Format(TITLE_FORMAT_S, Kernel.Configuration.ServerName, DateTime.Now.ToString("G"),
            //     Kernel.NetworkMonitor.UpdateStatsAsync(m_interval), Kernel.RoleManager.OnlinePlayers, Kernel.RoleManager.MaxOnlinePlayers,
            //     Kernel.SystemThread.ElapsedMilliseconds, Kernel.UserThread.ElapsedMilliseconds,
            //     Kernel.AiThread.ElapsedMilliseconds, Kernel.Version);

            // if (m_analytics.ToNextTime())
            // {
            //     await DoAnalyticsAsync();
            // }

            // if (Kernel.AccountClient == null && m_accountPing.ToNextTime())
            // {
            //     Console.WriteLine("Attempting connection with the account server...");

            //     Kernel.AccountClient = new AccountClient();
            //     if (await Kernel.AccountClient.ConnectToAsync(AccountClient.Configuration.IPAddress, AccountClient.Configuration.Port))
            //     {
            //         Console.WriteLine("Connected to the account server!");
            //     }
            // }

            return true;
        }

        public async Task DoAnalyticsAsync()
        {
            var interval = DateTime.Now - m_serverStartTime;
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, "=".PadLeft(64, '='));
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, $"Server Start Time: {m_serverStartTime:G}");
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, $"Total Online Time: {(int)interval.TotalDays} days, {interval.Hours} hours, {interval.Minutes} minutes, {interval.Seconds} seconds");
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, $"Online Players[{Kernel.RoleManager.OnlinePlayers}], Max Online Players[{Kernel.RoleManager.MaxOnlinePlayers}], Distinct Players[{Kernel.RoleManager.OnlineUniquePlayers}], Role Count[{Kernel.RoleManager.RolesCount}]");
            // await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, $"Total Bytes Sent: {Kernel.NetworkMonitor.TotalBytesSent:N0}, Total Packets Sent: {Kernel.NetworkMonitor.TotalPacketsSent:N0}");
            // await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, $"Total Bytes Recv: {Kernel.NetworkMonitor.TotalBytesRecv:N0}, Total Packets Recv: {Kernel.NetworkMonitor.TotalPacketsRecv:N0}");
            // await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, $"System Thread: {Kernel.SystemThread.ElapsedMilliseconds:N0}ms");
            // await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, $"Generator Thread: {Kernel.GeneratorManager.ElapsedMilliseconds}ms");
            // await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, $"User Thread: {Kernel.UserThread.ElapsedMilliseconds:N0}ms");
            // await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, $"Ai Thread: {Kernel.AiThread.ElapsedMilliseconds:N0}ms ({Kernel.AiThread.ProcessedMonsters} AI Agents)");
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, $"Identities Remaining: ");
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, $"\tMonster: {IdentityGenerator.Monster.IdentitiesCount()}");
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, $"\tFurniture: {IdentityGenerator.Furniture.IdentitiesCount()}");
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, $"\tMapItem: {IdentityGenerator.MapItem.IdentitiesCount()}");
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, $"\tTraps: {IdentityGenerator.Traps.IdentitiesCount()}");
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, "=".PadLeft(64, '='));
        }
    }
}