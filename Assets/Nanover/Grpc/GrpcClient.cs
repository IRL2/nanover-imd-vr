using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using JetBrains.Annotations;
using Nanover.Core.Async;
using Nanover.Grpc.Stream;
using Nanover.Protocol.Command;

namespace Nanover.Grpc
{
    /// <summary>
    /// Base implementation of a C# wrapper around a gRPC client to a specific service.
    /// </summary>
    public abstract class GrpcClient<TClient> : Cancellable, IAsyncClosable
        where TClient : ClientBase<TClient>
    {
        /// <summary>
        /// gRPC Client that provides access to RPC calls.
        /// </summary>
        protected TClient Client { get; }

        /// <summary>
        /// The client used to access the Command service on the same port as this client.
        /// </summary>
        protected Command.CommandClient CommandClient { get; }

        /// <summary>
        /// Create a client to a server described by the provided
        /// <see cref="GrpcConnection" />.
        /// </summary>
        protected GrpcClient([NotNull] GrpcConnection connection) : base(
            connection.GetCancellationToken())
        {
            if (connection.IsCancelled)
                throw new ArgumentException("Connection has already been shutdown.");

            Client = (TClient) Activator.CreateInstance(typeof(TClient), connection.Channel);

            CommandClient = new Command.CommandClient(connection.Channel);
        }


        /// <summary>
        /// Run a command on a gRPC service that uses the command service.
        /// </summary>
        /// <param name="command">The name of the command to run, which must be registered on the server.</param>
        /// <param name="arguments">Name/value arguments to provide to the command.</param>
        /// <returns>Dictionary of results produced by the command.</returns>
        public async Task<Dictionary<string, object>> RunCommandAsync(string command,
                                                                      Dictionary<string, object> arguments = null)
        {
            var message = new CommandMessage
            {
                Name = command,
                Arguments = arguments?.ToProtobufStruct(),
            };
            return (await CommandClient.RunCommandAsync(message)).Result.ToDictionary();
        }

        /// <summary>
        /// Run a command on a gRPC service that uses the command service.
        /// </summary>
        /// <param name="command">The name of the command to run, which must be registered on the server.</param>
        /// <param name="arguments">Name/value arguments to provide to the command.</param>
        /// <returns>Dictionary of results produced by the command.</returns>
        public async Task<IReadOnlyList<CommandMessage>> GetCommandsAsync()
        {
            var message = new GetCommandsRequest();
            return (await CommandClient.GetCommandsAsync(message)).Commands;
        }

        /// <summary>
        /// Create an incoming stream from the definition of a gRPC call.
        /// </summary>
        protected IncomingStream<TResponse> GetIncomingStream<TRequest, TResponse>(
            ServerStreamingCall<TRequest, TResponse> call,
            TRequest request,
            CancellationToken externalToken = default)
        {
            if (IsCancelled)
                throw new InvalidOperationException("The client is closed.");

            return IncomingStream<TResponse>.CreateStreamFromServerCall(
                call,
                request,
                GetCancellationToken(), externalToken);
        }

        /// <summary>
        /// Create an outgoing stream from the definition of a gRPC call.
        /// </summary>
        protected OutgoingStream<TOutgoing, TResponse> GetOutgoingStream<TOutgoing, TResponse>(
            ClientStreamingCall<TOutgoing, TResponse> call,
            CancellationToken externalToken = default)
        {
            if (IsCancelled)
                throw new InvalidOperationException("The client is closed.");

            return OutgoingStream<TOutgoing, TResponse>.CreateStreamFromClientCall(
                call,
                GetCancellationToken(), externalToken);
        }

        /// <inheritdoc cref="IAsyncClosable.CloseAsync" />
        public Task CloseAsync()
        {
            //Cancel();

            return Task.CompletedTask;
        }

        public void CloseAndCancelAllSubscriptions()
        {
            CloseAsync();
        }
    }
}