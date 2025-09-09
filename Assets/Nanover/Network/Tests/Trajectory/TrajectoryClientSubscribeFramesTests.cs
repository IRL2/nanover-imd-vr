using System.Collections.Generic;
using System.Threading.Tasks;
using Nanover.Grpc.Stream;
using Nanover.Grpc.Tests.Async;
using Nanover.Grpc.Trajectory;
using Nanover.Protocol.Trajectory;
using Nanover.Testing.Async;
using NUnit.Framework;

namespace Nanover.Grpc.Tests.Trajectory
{
    internal class TrajectoryClientSubscribeLatestFramesTests : ClientIncomingStreamTests<
        InfiniteTrajectoryService,
        TrajectoryClient,
        GetFrameResponse>
    {
        private static IEnumerable<AsyncUnitTests.AsyncTestInfo> GetTests()
        {
            return AsyncUnitTests
                .FindAsyncTestsInClass<TrajectoryClientSubscribeLatestFramesTests>();
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

        protected override InfiniteTrajectoryService GetService()
        {
            return new InfiniteTrajectoryService();
        }

        protected override TrajectoryClient GetClient(GrpcConnection connection)
        {
            return new TrajectoryClient(connection);
        }

        protected override IncomingStream<GetFrameResponse> GetStream(TrajectoryClient client)
        {
            return client.SubscribeLatestFrames();
        }

        public override Task GetStreamTask(IncomingStream<GetFrameResponse> stream)
        {
            return stream.StartReceiving();
        }

        public override void SetServerDelay(int delay)
        {
            service.Delay = delay;
        }

        public override void SetServerMaxMessage(int count)
        {
            service.MaxMessage = count;
        }
    }
}