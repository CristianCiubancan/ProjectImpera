namespace Comet.Game
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Comet.Game.Database;
    using Comet.Game.Packets;
    using Comet.Game.States;
    using Comet.Network.Packets;
    using Comet.Network.Security;
    using Comet.Network.Sockets;
    using Comet.Shared;

    /// <summary>
    /// Server inherits from a base server listener to provide the game server with
    /// listening functionality and event handling. This class defines how the server 
    /// listener and invoked events are customized for the game server.
    /// </summary>
    internal sealed class Server : TcpServerListener<Client>
    {
        private const int MaxPacketsPerSecond = 100; // Example limit
        private readonly Dictionary<uint, (DateTime lastCheck, int packetCount)> rateLimitRecords = new Dictionary<uint, (DateTime, int)>();

        // Fields and Properties
        private readonly PacketProcessor<Client> Processor;
        private readonly Task ProcessorTask;

        /// <summary>
        /// Instantiates a new instance of <see cref="Server"/> by initializing the 
        /// <see cref="PacketProcessor"/> for processing packets from the players using 
        /// channels and worker threads. Initializes the TCP server listener.
        /// </summary>
        /// <param name="config">The server's read configuration file</param>
        public Server(ServerConfiguration config)
            : base(maxConn: config.GameNetwork.MaxConn, exchange: true, footerLength: 8)
        {
            _ = Log.WriteLogAsync(LogLevel.Info, $"Server listening on {config.GameNetwork.IPAddress}:{config.GameNetwork.Port}").ConfigureAwait(false);
            this.Processor = new PacketProcessor<Client>(this.ProcessAsync);
            this.ProcessorTask = this.Processor.StartAsync(CancellationToken.None);
        }

        /// <summary>
        /// Invoked by the server listener's Accepting method to create a new server actor
        /// around the accepted client socket. Gives the server an opportunity to initialize
        /// any processing mechanisms or authentication routines for the client connection.
        /// </summary>
        /// <param name="socket">Accepted client socket from the server socket</param>
        /// <param name="buffer">Preallocated buffer from the server listener</param>
        /// <returns>A new instance of a ServerActor around the client socket</returns>
        protected override async Task<Client> AcceptedAsync(Socket socket, Memory<byte> buffer)
        {
            try
            {
                var partition = this.Processor.SelectPartition();
                var client = new Client(socket, buffer, partition);

                // Execute all asynchronous operations concurrently
                var tasks = new List<Task>
            {
                client.DiffieHellman.ComputePublicKeyAsync(),
                Kernel.NextBytesAsync(client.DiffieHellman.DecryptionIV),
                Kernel.NextBytesAsync(client.DiffieHellman.EncryptionIV)
            };
                await Task.WhenAll(tasks);

                var handshakeRequest = new MsgHandshake(
                    client.DiffieHellman,
                    client.DiffieHellman.EncryptionIV,
                    client.DiffieHellman.DecryptionIV);

                await handshakeRequest.RandomizeAsync();
                await client.SendAsync(handshakeRequest);

                return client;
            }
            catch (Exception ex)
            {
                // Log and handle the exception
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString()).ConfigureAwait(false);
                socket?.Close(); // Ensure the socket is closed in case of an error
                return null; // Consider how you wish to handle this scenario
            }
        }

        /// <summary>
        /// Invoked by the server listener's Exchanging method to process the client 
        /// response from the Diffie-Hellman Key Exchange. At this point, the raw buffer 
        /// from the client has been decrypted and is ready for direct processing.
        /// </summary>
        /// <param name="actor">Server actor that represents the remote client</param>
        /// <param name="buffer">Packet buffer to be processed</param>
        /// <returns>True if the exchange was successful.</returns>
        protected override bool Exchanged(Client actor, ReadOnlySpan<byte> buffer)
        {
            try
            {
                MsgHandshake msg = new MsgHandshake();
                msg.Decode(buffer.ToArray());

                // // Validate client key data
                // if (!IsValidKeyData(msg.ClientKey))
                // {
                //     Log.WriteLogAsync(LogLevel.Warning, $"Invalid key data from {actor.IPAddress}").ConfigureAwait(false);
                //     return false;
                // }

                actor.DiffieHellman.ComputePrivateKey(msg.ClientKey);

                actor.Cipher.GenerateKeys(new object[] {
                actor.DiffieHellman.PrivateKey.ToByteArrayUnsigned() });
                (actor.Cipher as BlowfishCipher).SetIVs(
                    actor.DiffieHellman.DecryptionIV,
                    actor.DiffieHellman.EncryptionIV);

                actor.DiffieHellman = null;
                return true;
            }
            catch (Exception ex)
            {
                _ = Log.WriteLogAsync(LogLevel.Exception, ex.ToString()).ConfigureAwait(false);
                return false;
            }
        }

        /// <summary>
        /// Invoked by the server listener's Receiving method to process a completed packet
        /// from the actor's socket pipe. At this point, the packet has been assembled and
        /// split off from the rest of the buffer.
        /// </summary>
        /// <param name="actor">Server actor that represents the remote client</param>
        /// <param name="packet">Packet bytes to be processed</param>
        protected override void Received(Client actor, ReadOnlySpan<byte> packet)
        {
            // Implement rate limiting and packet validation here
            if (IsRateLimitExceeded(actor))
            {
                _ = Log.WriteLogAsync(LogLevel.Warning, $"Invalid packet from {actor.IPAddress}").ConfigureAwait(false);
                return; // Optionally disconnect the client
            }

            this.Processor.Queue(actor, packet.ToArray());
        }

        /// <summary>
        /// Invoked by one of the server's packet processor worker threads to process a
        /// single packet of work. Allows the server to process packets as individual 
        /// messages on a single channel.
        /// </summary>
        /// <param name="actor">Actor requesting packet processing</param>
        /// <param name="packet">An individual data packet to be processed</param>
        private async Task ProcessAsync(Client actor, byte[] packet)
        {
            // Validate connection
            if (!actor.Socket.Connected)
                return;

            // Read in TQ's binary header
            var length = BitConverter.ToUInt16(packet, 0);
            PacketType type = (PacketType)BitConverter.ToUInt16(packet, 2);

            try
            {
                // Switch on the packet type
                MsgBase<Client> msg = null;
                switch (type)
                {
                    case PacketType.MsgRegister: msg = new MsgRegister(); break;
                    case PacketType.MsgItem: msg = new MsgItem(); break;
                    case PacketType.MsgAction: msg = new MsgAction(); break;
                    case PacketType.MsgConnect: msg = new MsgConnect(); break;

                    default:
                        await Log.WriteLogAsync(LogLevel.Warning, $"Missing packet {type}, Length {length}").ConfigureAwait(false);
                        await actor.SendAsync(new MsgTalk(actor.Identity, MsgTalk.TalkChannel.Service,
                            String.Format("Missing packet {0}, Length {1}",
                            type, length)));
                        return;
                }

                // Decode packet bytes into the structure and process
                msg.Decode(packet);
                await msg.ProcessAsync(actor);
            }
            catch (Exception e)
            {
                await Log.WriteLogAsync(LogLevel.Exception, e.ToString()).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Invoked by the server listener's Disconnecting method to dispose of the actor's
        /// resources. Gives the server an opportunity to cleanup references to the actor
        /// from other actors and server collections.
        /// </summary>
        /// <param name="actor">Server actor that represents the remote client</param>
        protected override void Disconnected(Client actor)
        {
            if (actor == null)
            {
                Console.WriteLine(@"Disconnected with actor null ???");
                return;
            }

            Processor.DeselectPartition(actor.Partition);

            bool fromCreation = false;
            if (actor.Creation != null)
            {
                _ = Kernel.Registration.Remove(actor.Creation.Token);
                fromCreation = true;
            }

            if (actor.Character != null)
            {
                _ = Log.WriteLogAsync(LogLevel.Info, $"{actor.Character.Name} has logged out.").ConfigureAwait(false);
                actor.Character.Connection = Character.ConnectionStage.Disconnected;

                Kernel.Services.Processor.Queue(actor.Character.Map?.Partition ?? 0, async () =>
                {
                    Kernel.RoleManager.ForceLogoutUser(actor.Character.Identity);
                    await actor.Character.OnDisconnectAsync();
                });
            }
            else
            {
                if (fromCreation)
                {
                    _ = Log.WriteLogAsync(LogLevel.Info, $"{actor.AccountIdentity} has created a new character and has logged out.").ConfigureAwait(false);
                }
                else
                {
                    _ = Log.WriteLogAsync(LogLevel.Info, $"[{actor.IPAddress}] {actor.AccountIdentity} has logged out.").ConfigureAwait(false);
                }
            }
        }


        private bool IsRateLimitExceeded(Client actor)
        {
            var now = DateTime.UtcNow;
            if (!rateLimitRecords.TryGetValue(actor.Identity, out var record))
            {
                rateLimitRecords[actor.Identity] = (now, 1);
                return false;
            }

            if (now - record.lastCheck > TimeSpan.FromSeconds(1))
            {
                // Reset the count every second
                rateLimitRecords[actor.Identity] = (now, 1);
                return false;
            }

            if (record.packetCount > MaxPacketsPerSecond)
            {
                return true; // Rate limit exceeded
            }

            // Increment the count and update the record
            rateLimitRecords[actor.Identity] = (record.lastCheck, record.packetCount + 1);
            return false;
        }
    }
}
