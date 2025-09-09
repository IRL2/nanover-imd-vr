using System;
using System.Threading;
using System.Threading.Tasks;
using Nanover.Core.Async;
using Nanover.Testing.Async;
using NUnit.Framework;

namespace Nanover.Grpc.Tests.Async
{
    internal abstract class ClientTests<TService, TClient> : BaseClientTests<TService, TClient>
        where TService : IBindableService
        where TClient : IAsyncClosable, ICancellable
    {
        [AsyncTest]
        public async Task CloseConnection_ConnectionToken_IsCancelled()
        {
            var connectionToken = connection.GetCancellationToken();

            await AsyncAssert.CompletesWithinTimeout(connection.CloseAsync());

            Assert.IsTrue(connectionToken.IsCancellationRequested);
        }

        [AsyncTest]
        public async Task CloseConnection_ConnectionTokenAfter_Exception()
        {
            await connection.CloseAsync();

            CancellationToken token;

            Assert.Throws<InvalidOperationException>(
                () => token = connection.GetCancellationToken());
        }

        [AsyncTest]
        public async Task CloseConnection_ClientToken_IsCancelled()
        {
            var clientToken = client.GetCancellationToken();

            await AsyncAssert.CompletesWithinTimeout(connection.CloseAsync());

            Assert.IsTrue(clientToken.IsCancellationRequested);
        }

        [AsyncTest]
        public async Task CloseConnection_ClientTokenAfter_Exception()
        {
            CancellationToken clientToken;

            await AsyncAssert.CompletesWithinTimeout(connection.CloseAsync());

            Assert.Throws<InvalidOperationException>(
                () => clientToken = client.GetCancellationToken());
        }


        [AsyncTest]
        public async Task CloseClient_ClientToken_IsCancelled()
        {
            var clientToken = client.GetCancellationToken();

            await AsyncAssert.CompletesWithinTimeout(client.CloseAsync());

            Assert.IsTrue(clientToken.IsCancellationRequested);
        }

        [AsyncTest]
        public Task CancelClient_ClientToken_IsCancelled()
        {
            var clientToken = client.GetCancellationToken();

            client.Cancel();

            Assert.IsTrue(clientToken.IsCancellationRequested);

            return Task.CompletedTask;
        }


        [AsyncTest]
        public async Task CloseClient_ClientTokenAfter_Exception()
        {
            CancellationToken clientToken;

            await AsyncAssert.CompletesWithinTimeout(client.CloseAsync());

            Assert.Throws<InvalidOperationException>(
                () => clientToken = client.GetCancellationToken());
        }

        [AsyncTest]
        public async Task CancelClient_ClientTokenAfter_Exception()
        {
            CancellationToken clientToken;

            await AsyncAssert.CompletesWithinTimeout(client.CloseAsync());

            Assert.Throws<InvalidOperationException>(
                () => clientToken = client.GetCancellationToken());
        }

        [AsyncTest]
        public async Task CloseClient_Idempotent()
        {
            await AsyncAssert.CompletesWithinTimeout(client.CloseAsync());

            await AsyncAssert.CompletesWithinTimeout(client.CloseAsync());
        }

        [AsyncTest]
        public Task CancelClient_Idempotent()
        {
            client.Cancel();

            client.Cancel();

            return Task.CompletedTask;
        }

        [AsyncTest]
        public async Task CloseThenCancelClient()
        {
            await AsyncAssert.CompletesWithinTimeout(client.CloseAsync());

            client.Cancel();
        }

        [AsyncTest]
        public async Task CancelThenCloseClient()
        {
            client.Cancel();

            await AsyncAssert.CompletesWithinTimeout(client.CloseAsync());
        }

        [AsyncTest]
        public async Task CloseClient_Simultaneous()
        {
            await AsyncAssert.CompletesWithinTimeout(
                Task.WhenAll(client.CloseAsync(), client.CloseAsync()));
        }
    }
}