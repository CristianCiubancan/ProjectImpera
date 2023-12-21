// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Core - TimerBase.cs
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

namespace Comet.Shared
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using Timer = System.Timers.Timer;

    namespace Comet.Shared
    {
        public abstract class TimerBase
        {
            private CancellationToken m_cancellationToken = CancellationToken.None;
            private Timer m_timer;
            private string m_name;
            protected int m_interval = 1000;

            protected TimerBase(int intervalMs, string name)
            {
                m_name = name;
                m_interval = intervalMs;

                m_timer = new Timer
                {
                    Interval = intervalMs,
                    AutoReset = false
                };
                m_timer.Elapsed += TimerOnElapse;
                m_timer.Disposed += TimerOnDisposed;
            }

            public string Name => m_name;

            public long ElapsedMilliseconds { get; private set; }

            public bool StopOnException { get; set; }

            public bool CloseRequest = false;

            public async Task StartAsync()
            {
                await OnStartAsync();

                m_timer.Start();
            }

            public Task CloseAsync()
            {
                m_timer.Stop();
                m_cancellationToken = new CancellationToken(true);
                return Task.CompletedTask;
            }

            private async void TimerOnDisposed(object sender, EventArgs e)
            {
                await OnCloseAsync();
            }

            private async void TimerOnElapse(object sender, ElapsedEventArgs e)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                try
                {
                    await OnElapseAsync();
                }
                catch (Exception ex)
                {
                    await Log.WriteLogAsync(LogLevel.Error, $"Exception thrown on [{Name}] thread!!!");
                    await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                }
                finally
                {
                    m_timer.Enabled = !m_cancellationToken.IsCancellationRequested;
                    sw.Stop();
                    ElapsedMilliseconds = sw.ElapsedMilliseconds;
                }
            }

            protected virtual async Task OnStartAsync()
            {
                await Log.WriteLogAsync(LogLevel.Info, $"Timer [{m_name}] has started");
            }

            protected virtual async Task<bool> OnElapseAsync()
            {
                await Log.WriteLogAsync(LogLevel.Info, $"Timer [{m_name}] has elapsed at {DateTime.Now}");
                return true;
            }

            protected virtual async Task OnCloseAsync()
            {
                await Log.WriteLogAsync(LogLevel.Info, $"Timer [{m_name}] has finished");
            }
        }
    }
}