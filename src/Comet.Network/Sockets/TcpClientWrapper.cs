using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
// using Comet.Shared;

namespace Comet.Network.Sockets
{
    public abstract class TcpClientWrapper<TActor> : TcpClientEvents<TActor>
        where TActor : TcpServerActor
    {
        public const int MaxBufferSize = 4096;
        public const int ReceiveTimeoutSeconds = 600;
        private readonly Memory<byte> Buffer;
        private readonly int FooterLength;
        private readonly CancellationTokenSource ShutdownToken;
        private readonly bool TimeOut;

        private readonly Socket Socket;

        protected TcpClientWrapper(int expectedFooterLength = 0, bool timeout = false)
        {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                LingerState = new LingerOption(false, 0)
            };
            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            ShutdownToken = new CancellationTokenSource();

            TimeOut = timeout;
            FooterLength = expectedFooterLength;

            Buffer = new Memory<byte>(new byte[MaxBufferSize]);
        }

        private ConfiguredTaskAwaitable<Task> ReceiveTask { get; set; }

        public async Task<bool> ConnectToAsync(string address, int port)
        {
            try
            {
                await Socket.ConnectAsync(address, port, ShutdownToken.Token);
                var actor = await ConnectedAsync(Socket, Buffer);
                // await Log.WriteLogAsync(LogLevel.Info, $"Connected to {address}:{port}!");
                ReceiveTask = (new TaskFactory().StartNew(ReceivingAsync, actor, ShutdownToken.Token)).ConfigureAwait(false);
                return Socket.Connected;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Receiving receives bytes from the accepted client socket when bytes become
        ///     available. While the client is connected and the server hasn't issued the
        ///     shutdown signal, bytes will be received in a loop.
        /// </summary>
        /// <param name="state">Created actor around the accepted client socket</param>
        /// <returns>Returns task details for fault tolerance processing.</returns>
        private Task ReceivingAsync(object state)
        {
            return ReceivingAsync(state, 0);
        }

        /// <summary>
        ///     Receiving receives bytes from the accepted client socket when bytes become
        ///     available. While the client is connected and the server hasn't issued the
        ///     shutdown signal, bytes will be received in a loop.
        /// </summary>
        /// <param name="state">Created actor around the accepted client socket</param>
        /// <param name="remaining">Starting offset to receive bytes to</param>
        /// <returns>Returns task details for fault tolerance processing.</returns>
        private async Task ReceivingAsync(object state, int remaining)
        {
            // Initialize multiple receive variables
            var actor = state as TActor;
            var timeout = new CancellationTokenSource();
            int examined = 0, consumed = 0;

            while (actor.Socket.Connected && !ShutdownToken.IsCancellationRequested)
            {
                try
                {
                    using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                        timeout.Token, ShutdownToken.Token);
                    // Receive data from the client socket
                    var receiveOperation = actor.Socket.ReceiveAsync(
                        actor.Buffer.Slice(remaining),
                        SocketFlags.None,
                        cancellation.Token);

                    timeout.CancelAfter(TimeSpan.FromSeconds(ReceiveTimeoutSeconds));
                    examined = await receiveOperation;
                    if (examined == 0) break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode < SocketError.ConnectionAborted ||
                        e.SocketErrorCode > SocketError.Shutdown)
                        Console.WriteLine(e);
                    break;
                }

                // Decrypt traffic
                actor.Cipher?.Decrypt(
                    actor.Buffer.Slice(remaining, examined).Span,
                    actor.Buffer.Slice(remaining, examined).Span);

                // Handle splitting and processing of data
                consumed = 0;
                if (!Splitting(actor, examined + remaining, ref consumed))
                {
                    actor.Disconnect();
                    break;
                }

                remaining = examined + remaining - consumed;
                actor.Buffer.Slice(consumed, remaining).CopyTo(actor.Buffer);
            }

            if (actor.Socket.Connected)
                actor.Disconnect();
            // Disconnect the client
            Disconnecting(actor);
        }

        /// <summary>
        ///     Splitting splits the actor's receive buffer into multiple packets that can
        ///     then be processed by Received individually. The default behavior of this method
        ///     unless otherwise overridden is to split packets from the buffer using an unsigned
        ///     short packet header for the length of each packet.
        /// </summary>
        /// <param name="actor">Actor for consuming bytes from the buffer</param>
        /// <param name="examined">Number of examined bytes from the receive</param>
        /// <param name="consumed">Number of consumed bytes by the split reader</param>
        /// <returns>Returns true if the client should remain connected.</returns>
        protected virtual bool Splitting(TActor actor, int examined, ref int consumed)
        {
            // Consume packets from the socket buffer
            var buffer = actor.Buffer.Span;
            while (consumed + 2 < examined)
            {
                var length = BitConverter.ToUInt16(buffer.Slice(consumed, 2));
                var expected = consumed + length + FooterLength;
                if (expected > buffer.Length) return false;
                if (expected > examined) break;

                Received(actor, buffer.Slice(consumed, length + FooterLength));
                consumed += length + FooterLength;
            }

            return true;
        }

        /// <summary>
        ///     Disconnecting is called when the client is disconnecting from the server. Allows
        ///     the server to handle client events post-disconnect, and reclaim resources first
        ///     leased to the client on accept.
        /// </summary>
        /// <param name="actor">Actor being disconnected</param>
        private void Disconnecting(TActor actor)
        {
            // Reclaim resources and release back to server pools
            actor.Buffer.Span.Clear();
            // Complete processing for disconnect
            Disconnected(actor);
        }
    }
}
