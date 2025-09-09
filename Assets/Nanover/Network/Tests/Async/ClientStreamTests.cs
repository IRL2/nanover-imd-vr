using System;
using System.Threading;
using System.Threading.Tasks;
using Nanover.Core.Async;
using Nanover.Testing.Async;
using NUnit.Framework;

namespace Nanover.Grpc.Tests.Async
{
    internal abstract class ClientStreamTests<TService, TClient, TStream> :
        ClientTests<TService, TClient>
        where TService : IBindableService
        where TClient : IAsyncClosable, ICancellable
        where TStream : IAsyncClosable, ICancellable
    {
        protected abstract TStream GetStream(TClient client);

        [AsyncTest]
        public async Task CloseConnection_Stream_IsCancelled()
        {
            var stream = GetStream(client);

            await AsyncAssert.CompletesWithinTimeout(connection.CloseAsync());

            Assert.IsTrue(stream.IsCancelled);
        }

        [AsyncTest]
        public async Task CloseConnection_StartStreamAfter_Exception()
        {
            await AsyncAssert.CompletesWithinTimeout(connection.CloseAsync());

            Assert.Throws<InvalidOperationException>(() => GetStream(client));
        }

        [AsyncTest]
        public async Task CloseConnection_StreamToken_IsCancelled()
        {
            var stream = GetStream(client);

            var streamToken = stream.GetCancellationToken();

            await AsyncAssert.CompletesWithinTimeout(connection.CloseAsync());

            Assert.IsTrue(streamToken.IsCancellationRequested);
        }

        [AsyncTest]
        public async Task CloseConnection_StreamTokenAfter_Exception()
        {
            var stream = GetStream(client);

            CancellationToken streamToken;

            await AsyncAssert.CompletesWithinTimeout(connection.CloseAsync());

            Assert.Throws<InvalidOperationException>(
                () => streamToken = stream.GetCancellationToken());
        }

        [AsyncTest]
        public async Task CloseClient_Stream_IsCancelled()
        {
            var stream = GetStream(client);

            await AsyncAssert.CompletesWithinTimeout(client.CloseAsync());

            Assert.IsTrue(stream.IsCancelled);
        }

        [AsyncTest]
        public async Task CancelClient_Stream_IsCancelled()
        {
            var stream = GetStream(client);

            await AsyncAssert.CompletesWithinTimeout(client.CloseAsync());

            Assert.IsTrue(stream.IsCancelled);
        }

        [AsyncTest]
        public async Task CloseClient_StartStreamAfter_Exception()
        {
            await AsyncAssert.CompletesWithinTimeout(client.CloseAsync());

            Assert.Throws<InvalidOperationException>(() => GetStream(client));
        }

        [AsyncTest]
        public Task CancelClient_StartStreamAfter_Exception()
        {
            client.Cancel();

            Assert.Throws<InvalidOperationException>(() => GetStream(client));

            return Task.CompletedTask;
        }

        [AsyncTest]
        public async Task CloseClient_StreamToken_IsCancelled()
        {
            var stream = GetStream(client);

            var streamToken = stream.GetCancellationToken();

            await AsyncAssert.CompletesWithinTimeout(client.CloseAsync());

            Assert.IsTrue(streamToken.IsCancellationRequested);
        }

        [AsyncTest]
        public Task CancelClient_StreamToken_IsCancelled()
        {
            var stream = GetStream(client);

            var streamToken = stream.GetCancellationToken();

            client.Cancel();

            Assert.IsTrue(streamToken.IsCancellationRequested);

            return Task.CompletedTask;
        }

        [AsyncTest]
        public async Task CloseClient_StreamTokenAfter_Exception()
        {
            var stream = GetStream(client);

            CancellationToken streamToken;

            await AsyncAssert.CompletesWithinTimeout(client.CloseAsync());

            Assert.Throws<InvalidOperationException>(
                () => streamToken = stream.GetCancellationToken());
        }

        [AsyncTest]
        public Task CancelClient_StreamTokenAfter_Exception()
        {
            var stream = GetStream(client);

            CancellationToken streamToken;

            client.Cancel();

            Assert.Throws<InvalidOperationException>(
                () => streamToken = stream.GetCancellationToken());

            return Task.CompletedTask;
        }

        [AsyncTest]
        public async Task CloseStream_Stream_IsCancelled()
        {
            var stream = GetStream(client);

            await AsyncAssert.CompletesWithinTimeout(stream.CloseAsync());

            Assert.IsTrue(stream.IsCancelled);
        }

        [AsyncTest]
        public Task CancelStream_Stream_IsCancelled()
        {
            var stream = GetStream(client);

            stream.Cancel();

            Assert.IsTrue(stream.IsCancelled);

            return Task.CompletedTask;
        }

        [AsyncTest]
        public async Task CloseStream_StreamToken_IsCancelled()
        {
            var stream = GetStream(client);

            var streamToken = stream.GetCancellationToken();

            await AsyncAssert.CompletesWithinTimeout(stream.CloseAsync());

            Assert.IsTrue(streamToken.IsCancellationRequested);
        }

        [AsyncTest]
        public Task CancelStream_StreamToken_IsCancelled()
        {
            var stream = GetStream(client);

            var streamToken = stream.GetCancellationToken();

            stream.Cancel();

            Assert.IsTrue(streamToken.IsCancellationRequested);

            return Task.CompletedTask;
        }

        [AsyncTest]
        public async Task CloseStream_StreamTokenAfter_Exception()
        {
            var stream = GetStream(client);

            CancellationToken streamToken;

            await AsyncAssert.CompletesWithinTimeout(stream.CloseAsync());

            Assert.Throws<InvalidOperationException>(
                () => streamToken = stream.GetCancellationToken());
        }

        [AsyncTest]
        public Task CancelStream_StreamTokenAfter_Exception()
        {
            var stream = GetStream(client);

            CancellationToken streamToken;

            stream.Cancel();

            Assert.Throws<InvalidOperationException>(
                () => streamToken = stream.GetCancellationToken());

            return Task.CompletedTask;
        }

        [AsyncTest]
        public async Task CloseStream_OtherStreamStillActive()
        {
            var stream1 = GetStream(client);
            var stream2 = GetStream(client);

            var stream1Token = stream1.GetCancellationToken();
            var stream2Token = stream2.GetCancellationToken();

            await AsyncAssert.CompletesWithinTimeout(stream1.CloseAsync());

            Assert.IsTrue(stream1Token.IsCancellationRequested);
            Assert.IsFalse(stream2Token.IsCancellationRequested);

            await AsyncAssert.CompletesWithinTimeout(stream2.CloseAsync());
        }

        [AsyncTest]
        public Task CancelStream_OtherStreamStillActive()
        {
            var stream1 = GetStream(client);
            var stream2 = GetStream(client);

            var stream1Token = stream1.GetCancellationToken();
            var stream2Token = stream2.GetCancellationToken();

            stream1.Cancel();

            Assert.IsTrue(stream1Token.IsCancellationRequested);
            Assert.IsFalse(stream2Token.IsCancellationRequested);

            stream2.Cancel();

            return Task.CompletedTask;
        }

        [AsyncTest]
        public async Task CloseStream_Idempotent()
        {
            var stream = GetStream(client);

            await AsyncAssert.CompletesWithinTimeout(stream.CloseAsync());

            await AsyncAssert.CompletesWithinTimeout(stream.CloseAsync());
        }

        [AsyncTest]
        public Task CancelStream_Idempotent()
        {
            var stream = GetStream(client);

            stream.Cancel();

            stream.Cancel();

            return Task.CompletedTask;
        }

        [AsyncTest]
        public async Task CloseThenCancelStream()
        {
            var stream = GetStream(client);

            await AsyncAssert.CompletesWithinTimeout(stream.CloseAsync());

            stream.Cancel();
        }

        [AsyncTest]
        public async Task CancelThenCloseStream()
        {
            var stream = GetStream(client);

            stream.Cancel();

            await AsyncAssert.CompletesWithinTimeout(stream.CloseAsync());
        }

        [AsyncTest]
        public async Task CloseStream_Simultaneous()
        {
            var stream = GetStream(client);

            await AsyncAssert.CompletesWithinTimeout(
                Task.WhenAll(stream.CloseAsync(), stream.CloseAsync()));
        }
    }
}