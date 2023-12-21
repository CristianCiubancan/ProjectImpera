using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
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

        protected override Task ExecuteAsync(CancellationToken token)
        {
            for (int i = 0; i < Count; i++)
            {
                m_BackgroundTasks[i] = DequeueAsync(i, m_Channels[i], token);
            }

            return Task.WhenAll(m_BackgroundTasks);
        }

        public void Queue(int partition, Func<Task> task)
        {
            if (!m_CancelWrites.Token.IsCancellationRequested)
            {
                m_Channels[partition].Writer.TryWrite(task);
            }
        }

        protected virtual async Task DequeueAsync(int partition, Channel<Func<Task>> channel, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var action = await channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                if (action != null)
                {
                    try
                    {
                        await action.Invoke().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // Assuming Log.WriteLogAsync is a valid method in your project
                        // await Log.WriteLogAsync(LogLevel.Exception, $"{ex.Message}\r\n\t{ex}").ConfigureAwait(false);
                    }
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
            Interlocked.Decrement(ref m_Partitions[partition].Weight);
        }

        protected class Partition
        {
            public uint ID;
            public int Weight;
        }
    }
}
