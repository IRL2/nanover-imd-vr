using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Nanover.Grpc.Tests.Commands;
using Nanover.Grpc.Tests.Multiplayer;
using Nanover.Grpc.Tests.Trajectory;
using Nanover.Grpc.Trajectory;
using Nanover.Protocol.Trajectory;
using Nanover.Testing.Async;
using NSubstitute;
using NUnit.Framework;

namespace Nanover.Grpc.Tests.Session
{
    internal class TrajectorySessionTests
    {
        private QueueTrajectoryService service;
        private CommandService commandService;
        private GrpcServer server;
        private GrpcConnection connection;
        private TrajectorySession session;
        private Action<string, Struct> callback;

        private static IEnumerable<AsyncUnitTests.AsyncTestInfo> GetTests()
        {
            return AsyncUnitTests.FindAsyncTestsInClass<TrajectorySessionTests>();
        }

        [Test]
        public void TestAsync([ValueSource(nameof(GetTests))] AsyncUnitTests.AsyncTestInfo test)
        {
            AsyncUnitTests.RunAsyncTest(this, test);
        }

        [SetUp]
        public void AsyncSetup()
        {
            AsyncUnitTests.RunAsyncSetUp(this);
        }

        [AsyncSetUp]
        public Task Setup()
        {
            service = new QueueTrajectoryService(new FrameData());
            commandService = new CommandService();
            (server, connection) = GrpcServer.CreateServerAndConnection(service, commandService);
            session = new TrajectorySession();
            session.OpenClient(connection);
            callback = Substitute.For<Action<string, Struct>>();
            commandService.ReceivedCommand += callback;

            return Task.CompletedTask;
        }

        [TearDown]
        public void AsyncTearDown()
        {
            AsyncUnitTests.RunAsyncTearDown(this);
        }

        [AsyncTearDown]
        public async Task TearDown()
        {
            session.CloseClient();
            await connection.CloseAsync();
            await server.CloseAsync();
        }

        [AsyncTest]
        public async Task CommandPlay()
        {
            session.Play();

            void ReceivedCommand() => callback.Received(1)
                                              .Invoke(TrajectoryClient.CommandPlay,
                                                      Arg.Any<Struct>());

            await AsyncAssert.PassesWithinTimeout(ReceivedCommand);
        }

        [AsyncTest]
        public async Task CommandPause()
        {
            session.Pause();

            void ReceivedCommand() => callback.Received(1)
                                              .Invoke(TrajectoryClient.CommandPause,
                                                      Arg.Any<Struct>());

            await AsyncAssert.PassesWithinTimeout(ReceivedCommand);
        }

        [AsyncTest]
        public async Task CommandReset()
        {
            session.Reset();

            void ReceivedCommand() => callback.Received(1)
                                              .Invoke(TrajectoryClient.CommandReset,
                                                      Arg.Any<Struct>());

            await AsyncAssert.PassesWithinTimeout(ReceivedCommand);
        }

        [AsyncTest]
        public async Task CommandStep()
        {
            session.Step();

            void ReceivedCommand() => callback.Received(1)
                                              .Invoke(TrajectoryClient.CommandStep,
                                                      Arg.Any<Struct>());

            await AsyncAssert.PassesWithinTimeout(ReceivedCommand);
        }

        [AsyncTest]
        public async Task ArbitraryCommand()
        {
            await session.Client.RunCommandAsync("some_command",
                                                 new Dictionary<string, object>
                                                 {
                                                     ["abc"] = 1.23,
                                                     ["xyz"] = "string"
                                                 });

            void ReceivedCommand() => callback.Received(1)
                                              .Invoke("some_command",
                                                      Arg.Is<Struct>(
                                                          s => s.Fields["abc"].NumberValue == 1.23
                                                            && s.Fields["xyz"].StringValue ==
                                                               "string"));

            await AsyncAssert.PassesWithinTimeout(ReceivedCommand);
        }
    }
}