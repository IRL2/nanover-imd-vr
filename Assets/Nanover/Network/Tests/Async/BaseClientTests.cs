using System;
using System.Threading;
using System.Threading.Tasks;
using Nanover.Core.Async;
using Nanover.Grpc.Tests.Multiplayer;
using Nanover.Testing.Async;
using NUnit.Framework;

namespace Nanover.Grpc.Tests.Async
{
    /// <summary>
    /// Setup a client and a server for testing gRPC.
    /// </summary>
    internal abstract class BaseClientTests<TService, TClient>
        where TService : IBindableService
        where TClient : IAsyncClosable, ICancellable
    {
        protected abstract TService GetService();
        protected abstract TClient GetClient(GrpcConnection connection);

        protected GrpcServer server;
        protected TService service;
        protected TClient client;
        protected GrpcConnection connection;

        [AsyncSetUp]
        public virtual Task SetUp()
        {
            service = GetService();
            (server, connection) = GrpcServer.CreateServerAndConnection(service);
            client = GetClient(connection);

            return Task.CompletedTask;
        }

        [AsyncTearDown]
        public virtual async Task TearDown()
        {
            await connection.CloseAsync();
            await server.CloseAsync();
        }
    }
}