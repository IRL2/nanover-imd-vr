using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Nanover.Grpc;
using Nanover.Grpc.Tests.Multiplayer;
using Nanover.Grpc.Tests.Trajectory;
using Nanover.Grpc.Trajectory;
using Nanover.Testing.Async;
using NUnit.Framework;

namespace Nanover.Network.Tests
{
    public class GrpcConnectionTests
    {
        private static IEnumerable<AsyncUnitTests.AsyncTestInfo> GetTests()
        {
            return AsyncUnitTests.FindAsyncTestsInClass<GrpcConnectionTests>();
        }

        [Test]
        public void TestAsync([ValueSource(nameof(GetTests))] AsyncUnitTests.AsyncTestInfo test)
        {
            AsyncUnitTests.RunAsyncTest(this, test);
        }

        [Test]
        public void Constructor_NullAddress_Exception()
        {
            GrpcConnection connection;

            Assert.Throws<ArgumentException>(
                () => connection = new GrpcConnection(null, 54321));
        }

        [Test]
        public void Constructor_NegativePort_Exception()
        {
            GrpcConnection connection;

            Assert.Throws<ArgumentOutOfRangeException>(
                () => connection = new GrpcConnection("localhost", -1));
        }

        [AsyncTest]
        public async Task NoServer_IsChannelIdle()
        {
            var connection = new GrpcConnection("localhost", 54321);

            try
            {
                await Task.Delay(100);
                Assert.AreEqual(ChannelState.Idle, connection.Channel.State);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        [AsyncTest]
        public async Task Server_IsChannelReady()
        {
            var service = new QueueTrajectoryService();
            var (server, connection) = GrpcServer.CreateServerAndConnection(service);

            try
            {
                var client = new TrajectoryClient(connection);

                using (var token = new CancellationTokenSource())
                {
                    var stream = client.SubscribeLatestFrames(externalToken: token.Token);
                    var task = stream.StartReceiving();

                    await Task.WhenAny(task, Task.Delay(100));

                    token.Cancel();

                    Assert.AreEqual(ChannelState.Ready, connection.Channel.State);
                }
            }
            finally
            {
                await server.CloseAsync();
                await connection.CloseAsync();
            }
        }

        [AsyncTest]
        public async Task DisconnectedServer_IsChannelIdle()
        {
            var service = new QueueTrajectoryService();
            var (server, connection) = GrpcServer.CreateServerAndConnection(service);

            try
            {
                var client = new TrajectoryClient(connection);

                using (var token = new CancellationTokenSource())
                {
                    var stream = client.SubscribeLatestFrames(externalToken: token.Token);
                    var task = stream.StartReceiving();

                    await Task.WhenAny(task, Task.Delay(100));

                    await server.CloseAsync();

                    await Task.Delay(100);

                    Assert.AreEqual(ChannelState.Idle, connection.Channel.State);

                    token.Cancel();
                }
            }
            finally
            {
                await server.CloseAsync();
                await connection.CloseAsync();
            }
        }

        [AsyncTest]
        public async Task Connection_CloseAsync_IsAtomic()
        {
            var service = new QueueTrajectoryService();
            var (server, connection) = GrpcServer.CreateServerAndConnection(service);

            await connection.CloseAsync();
            await connection.CloseAsync();

            await server.CloseAsync();
        }
    }
}