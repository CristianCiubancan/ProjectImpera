using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Comet.Network.Sockets;
using Comet.Shared;
using Microsoft.Extensions.Hosting;

public class PacketProcessor<TClient> : BackgroundService
    where TClient : TcpServerActor
{
    protected readonly Task[] BackgroundTasks;
    protected readonly Channel<Message>[] Channels;
    protected readonly Partition[] Partitions;
    protected readonly Func<TClient, byte[], Task> Process;
    private CancellationTokenSource cancelWritesSource = new CancellationTokenSource();

    public PacketProcessor(Func<TClient, byte[], Task> process, int count = 0)
    {
        count = count == 0 ? Environment.ProcessorCount : count;
        this.BackgroundTasks = new Task[count];
        this.Channels = new Channel<Message>[count];
        this.Partitions = new Partition[count];
        this.Process = process;

        for (int i = 0; i < count; i++)
        {
            this.Partitions[i] = new Partition { ID = (uint)i, Weight = 0 };
            this.Channels[i] = Channel.CreateUnbounded<Message>();
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.WriteLogAsync(LogLevel.Debug, $"Starting nebun ia de {this.BackgroundTasks.Length} background tasks").ConfigureAwait(false);
        for (int i = 0; i < this.BackgroundTasks.Length; i++)
        {
            // Link cancellation tokens to properly handle shutdowns.
            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, cancelWritesSource.Token);
            this.BackgroundTasks[i] = DequeueAsync(this.Channels[i], linkedTokenSource.Token);
        }
        
        return Task.WhenAll(this.BackgroundTasks);
    }

    public void Queue(TClient actor, byte[] packet)
    {
        Log.WriteLogAsync(LogLevel.Debug, $"Queue: {actor}").ConfigureAwait(false);
        if (!cancelWritesSource.Token.IsCancellationRequested)
        {
            Log.WriteLogAsync(LogLevel.Debug, $"Queue: {actor}").ConfigureAwait(false);
            this.Channels[actor.Partition].Writer.TryWrite(new Message { Actor = actor, Packet = packet });
        }
    }

    protected async Task DequeueAsync(Channel<Message> channel, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Log.WriteLogAsync(LogLevel.Debug, $"DequeueAsync: {channel.Reader.Count}").ConfigureAwait(false);
            try
            {
                var msg = default(Message);
                try
                {
                    msg = await channel.Reader.ReadAsync(cancellationToken);
                } catch (Exception ex)
                {
                    await Log.WriteLogAsync(LogLevel.Exception, $"Exception in ServerProcessor: {ex.Message}\r\n\t{ex}");
                    break;
                }
                await Log.WriteLogAsync(LogLevel.Debug, $"DequeueAsync: {msg.Actor}").ConfigureAwait(false);
                if (msg != null)
                {
                    await Log.WriteLogAsync(LogLevel.Debug, $"DequeueAsync: {msg.Actor}").ConfigureAwait(false);
                    await this.Process(msg.Actor, msg.Packet).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
                break;
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Exception, $"Exception in ServerProcessor: {ex.Message}\r\n\t{ex}");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        cancelWritesSource.Cancel();
        foreach (var channel in this.Channels)
        {
            channel.Writer.Complete();
        }

        await Task.WhenAll(this.BackgroundTasks);

        await base.StopAsync(cancellationToken);
    }

    public uint SelectPartition()
    {
        uint partition = this.Partitions.OrderBy(p => p.Weight).First().ID;
        Interlocked.Increment(ref this.Partitions[partition].Weight);
        return partition;
    }

    public void DeselectPartition(uint partition)
    {
        Interlocked.Decrement(ref this.Partitions[partition].Weight);
    }

    protected class Message
    {
        public TClient Actor;
        public byte[] Packet;
    }

    protected class Partition
    {
        public uint ID;
        public int Weight;
    }
}
