using System;
using System.Threading;
using Grpc.Net.Client;
using JetBrains.Annotations;
using Nanover.Core.Async;
using Cysharp.Net.Http;

namespace Nanover.Grpc
{
    /// <summary>
    /// Represents a connection to a server using the GRPC protocol. A GRPC
    /// server provides several services, which can be connected to using
    /// <see cref="Channel" />. An implementation of a client for
    /// a GRPC service should hold this connection and reference the
    /// CancellationToken to shut itself down when the connection is terminated.
    /// </summary>
    public sealed class GrpcConnection : ICancellationTokenSource
    {
        /// <summary>
        /// GRPC channel which represents a connection to a GRPC server.
        /// </summary>
        [CanBeNull]
        public GrpcChannel Channel { get; private set; }
        [CanBeNull]
        private CancellationTokenSource CancellationTokenSource { get; set; }

        /// <summary>
        /// Is this connection cancelled?
        /// </summary>
        public bool IsCancelled => Channel == null
                                || CancellationTokenSource == null
                                || CancellationTokenSource.IsCancellationRequested;

        /// <summary>
        /// Create a new connection to a GRPC server and begin connecting.
        /// </summary>
        /// <param name="address">The ip address of the server.</param>
        /// <param name="port">The port of the server.</param>
        public GrpcConnection(string address, int port)
        {
            if (address == null)
                throw new ArgumentException(nameof(address));
            if (port < 0)
                throw new ArgumentOutOfRangeException(nameof(port));
            Channel = GrpcChannel.ForAddress(
                $"http://{address}:{port}",
                new GrpcChannelOptions()
                {
                    HttpHandler = new YetAnotherHttpHandler() { Http2Only = true },
                    DisposeHttpClient = true,
                    MaxReceiveMessageSize = null,
                });
            CancellationTokenSource = new CancellationTokenSource();
        }

        /// <inheritdoc cref="ICancellationTokenSource.GetCancellationToken" />
        public CancellationToken GetCancellationToken()
        {
            if (IsCancelled)
                throw new InvalidOperationException(
                    "Trying to get a cancellation token for an already shutdown connection.");
            return CancellationTokenSource.Token;
        }

        /// <summary>
        /// Closes the GRPC channel asynchronously. This can be awaited or
        /// executed in the background.
        /// </summary>
        public void Close()
        {
            if (IsCancelled)
                return;

            CancellationTokenSource?.Cancel();
            CancellationTokenSource?.Dispose();
            Channel?.Dispose();
            Channel = null;
            CancellationTokenSource = null;
        }
    }
}