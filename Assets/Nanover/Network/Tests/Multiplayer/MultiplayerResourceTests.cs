using System.Collections.Generic;
using System.Threading.Tasks;
using Nanover.Grpc.Multiplayer;
using Nanover.Testing.Async;
using NUnit.Framework;

namespace Nanover.Grpc.Tests.Multiplayer
{
    public class MultiplayerResourceTests
    {
        private MultiplayerService service;
        private GrpcServer server;
        private MultiplayerSession session;
        private GrpcConnection connection;

        [SetUp]
        public void AsyncSetup()
        {
            AsyncUnitTests.RunAsyncSetUp(this);
        }

        [AsyncSetUp]
        public async Task Setup()
        {
            service = new MultiplayerService();
            server = new GrpcServer(service);

            session = new MultiplayerSession();

            connection = new GrpcConnection("localhost", server.Port);
            session.OpenClient(connection);

            await Task.Delay(500);
        }

        [TearDown]
        public void AsyncTearDown()
        {
            AsyncUnitTests.RunAsyncTearDown(this);
        }

        [AsyncTearDown]
        public async Task TearDown()
        {
            session?.CloseClient();
            if (connection != null)
                await connection.CloseAsync();
            if (server != null)
                await server.CloseAsync();
        }

        private const string Key = "abc";
        private const string OtherPlayerId = "other_player";
        
        private static IEnumerable<AsyncUnitTests.AsyncTestInfo> GetTests()
        {
            return AsyncUnitTests.FindAsyncTestsInClass<MultiplayerResourceTests>();
        }

        [Test]
        public void TestAsync([ValueSource(nameof(GetTests))] AsyncUnitTests.AsyncTestInfo test)
        {
            AsyncUnitTests.RunAsyncTest(this, test);
        }

        public MultiplayerResource<string> GetResource()
        {
            return new MultiplayerResource<string>(session, Key);
        }

        [AsyncTest]
        public async Task GetResource_InitialValue()
        {
            service.SetValueDirect(Key, "my_value");
            await Task.Delay(500);
            var resource = GetResource();
            Assert.AreEqual("my_value", resource.Value);
        }

        [AsyncTest]
        public async Task RemoteValueChanged_Occurs()
        {
            var resource = GetResource();
            var listener = AsyncAssert.ListenToEvent((cbk) => resource.RemoteValueChanged += cbk);
            service.SetValueDirect(Key, "my_value");

            await Task.Delay(500);
            
            listener.AssertCalledAtLeastOnce();
        }
        
        [AsyncTest]
        public async Task SimulateSlowConnection()
        {
            var resource = GetResource();
            service.SetValueDirect(Key, "old_value");
            await Task.Delay(500);
            
            // Initially, resource and local share state are "old_value"
            Assert.AreEqual("old_value", resource.Value);
            Assert.AreEqual("old_value", session.GetSharedState(Key));
            
            // Introduce 500 ms latency to resource stream
            server.StreamLatency = 500;
            
            // Set the resource value to "new_value". Latency means local shared state is
            // out of date
            resource.UpdateValueWithLock("new_value");
            await Task.Delay(50);
            Assert.AreEqual("new_value", resource.Value);
            Assert.AreEqual("old_value", session.GetSharedState(Key));
            
            // Release the lock. Latency means local shared state is out of data
            resource.ReleaseLock();
            Assert.AreEqual("new_value", resource.Value);
            Assert.AreEqual("old_value", session.GetSharedState(Key));

            // Even a short time later, local shared state is out of date. However, 
            // value is still correct as an up-to-date update has not been received.
            await Task.Delay(50);
            Assert.AreEqual("new_value", resource.Value);
            Assert.AreEqual("old_value", session.GetSharedState(Key));

            // After latency has been overcome, values now agree.
            await Task.Delay(1000);
            Assert.AreEqual("new_value", resource.Value);
            Assert.AreEqual("new_value", session.GetSharedState(Key));
        }
    }
}
