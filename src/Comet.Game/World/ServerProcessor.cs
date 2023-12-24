using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Comet.Shared;
using Microsoft.Extensions.Hosting;

namespace Comet.Game.World
{
    public class ServerProcessor : BackgroundService
    {
        protected readonly Task[] m_BackgroundTasks;
        protected readonly Channel<Func<Task>>[] m_Channels;
        protected readonly Partition[] m_Partitions;
        protected CancellationTokenSource m_CancelReads;
        protected CancellationTokenSource m_CancelWrites;

        public readonly int Count;

        public ServerProcessor(int processorCount)
        {
            _ = Log.WriteLogAsync(LogLevel.Debug, $"ServerProcessor created with {processorCount} partitions").ConfigureAwait(false);
            Count = Math.Max(1, processorCount);

            m_BackgroundTasks = new Task[Count];
            m_Channels = new Channel<Func<Task>>[Count];
            m_Partitions = new Partition[Count];
            m_CancelReads = new CancellationTokenSource();
            m_CancelWrites = new CancellationTokenSource();

            for (int i = 0; i < Count; i++)
            {
                m_Partitions[i] = new Partition { ID = (uint)i, Weight = 0 };
                m_Channels[i] = Channel.CreateUnbounded<Func<Task>>();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            try
            {
                await Log.WriteLogAsync(LogLevel.Debug, $"Starting {Count} background tasks");
                for (int i = 0; i < Count; i++)
                {
                    int taskIndex = i; // Capture the current loop index
                    await Log.WriteLogAsync(LogLevel.Debug, $"Starting background task {taskIndex}");
                    m_BackgroundTasks[i] = DequeueAsync(taskIndex, m_Channels[i], token);
                }
                await Log.WriteLogAsync(LogLevel.Debug, $"All background tasks started");
                await Task.WhenAll(m_BackgroundTasks);
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Exception, $"Exception in ServerProcessor: {ex.Message}\r\n\t{ex}");
            }
        }

        public void Queue(int partition, Func<Task> task)
        {
            if (task == null)
            {
                _ = Log.WriteLogAsync(LogLevel.Warning, "Attempted to queue a null task.").ConfigureAwait(false);
                return;
            }
            if (partition < 0 || partition >= Count)
            {
                _ = Log.WriteLogAsync(LogLevel.Warning, $"Attempted to queue a task to partition {partition} which is out of range.").ConfigureAwait(false);
                return;
            }

            if (!m_CancelWrites.Token.IsCancellationRequested)
            {
                _ = m_Channels[partition].Writer.TryWrite(task);
            }
            else
            {
                _ = Log.WriteLogAsync(LogLevel.Warning, $"Write operation cancelled. Task not queued to partition {partition}.").ConfigureAwait(false);
            }
        }


        protected async Task DequeueAsync(int partition, Channel<Func<Task>> channel, CancellationToken cancellationToken)
        {
            await Log.WriteLogAsync(LogLevel.Debug, $"Starting dequeue task for partition {partition}");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Log.WriteLogAsync(LogLevel.Debug, $"Waiting for task in partition {partition}");
                    var action = await channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                    if (action != null)
                    {
                        await Log.WriteLogAsync(LogLevel.Debug, $"Task received in partition {partition}");
                        await action().ConfigureAwait(false);
                    }
                    else
                    {
                        await Log.WriteLogAsync(LogLevel.Warning, $"Received null task in partition {partition}");
                    }
                }
                catch (OperationCanceledException)
                {
                    await Log.WriteLogAsync(LogLevel.Info, $"Operation canceled in partition {partition}");
                    break;
                }
                catch (Exception ex)
                {
                    await Log.WriteLogAsync(LogLevel.Exception, $"Exception in partition {partition}: {ex.Message}\r\n\t{ex}");
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            m_CancelWrites.Cancel();
            foreach (var channel in m_Channels)
            {
                channel.Writer.Complete();
            }
            m_CancelReads.Cancel();

            await base.StopAsync(cancellationToken);
        }
        public uint SelectPartition()
        {
            return m_Partitions.MinBy(p => p.Weight).ID;
        }

        public void DeselectPartition(uint partition)
        {
            _ = Interlocked.Decrement(ref m_Partitions[partition].Weight);
        }
        protected class Partition
        {
            public uint ID;
            public int Weight;
        }
    }
}