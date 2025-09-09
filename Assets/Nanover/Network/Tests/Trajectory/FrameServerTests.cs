using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nanover.Grpc.Tests.Async;
using Nanover.Grpc.Trajectory;
using Nanover.Protocol.Trajectory;
using Nanover.Testing.Async;
using NSubstitute;
using NUnit.Framework;

namespace Nanover.Grpc.Tests.Trajectory
{
    internal class FrameServerTests : BaseClientTests<QueueTrajectoryService, TrajectoryClient>
    {
        private static IEnumerable<AsyncUnitTests.AsyncTestInfo> GetTests()
        {
            return AsyncUnitTests.FindAsyncTestsInClass<FrameServerTests>();
        }

        [Test]
        public void TestAsync([ValueSource(nameof(GetTests))] AsyncUnitTests.AsyncTestInfo test)
        {
            AsyncUnitTests.RunAsyncTest(this, test);
        }

        [SetUp]
        public void AsyncSetUp()
        {
            AsyncUnitTests.RunAsyncSetUp(this);
        }

        [TearDown]
        public void AsyncTearDown()
        {
            AsyncUnitTests.RunAsyncTearDown(this);
        }

        private FrameData data;

        protected override QueueTrajectoryService GetService()
        {
            data = new FrameData();
            data.SetBondPairs(new[]
            {
                0u, 1u, 1u, 2u
            });
            data.SetParticleElements(new[]
            {
                1u, 6u, 1u
            });
            data.SetParticlePositions(new[]
            {
                -1f, 1f, 0f, 0f, 0f, 0f, 1f, -1f, 0f
            });

            return new QueueTrajectoryService(data);
        }

        protected override TrajectoryClient GetClient(GrpcConnection connection)
        {
            return new TrajectoryClient(connection);
        }

        [AsyncSetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
        }

        [AsyncTearDown]
        public override async Task TearDown()
        {
            await base.TearDown();
        }


        [AsyncTest]
        public async Task FrameDataTransmission()
        {
            var callback = Substitute.For<Action<GetFrameResponse>>();

            var stream = client.SubscribeLatestFrames();
            stream.MessageReceived += callback;
            var getFrameTask = stream.StartReceiving();

            await Task.WhenAny(getFrameTask, Task.Delay(500));

            callback.Received(1)
                    .Invoke(Arg.Is<GetFrameResponse>(rep => rep.Frame.Equals(data)));
        }
    }
}