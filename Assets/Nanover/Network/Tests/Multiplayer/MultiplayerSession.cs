using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nanover.Grpc.Multiplayer;
using Nanover.Testing.Async;
using NSubstitute;
using NUnit.Framework;

namespace Nanover.Grpc.Tests.Multiplayer
{
    internal class MultiplayerSessionTests
    {
        private MultiplayerService service;
        private GrpcServer server;
        private MultiplayerSession session;
        private GrpcConnection connection;

        private MultiplayerSession session2;
        private GrpcConnection connection2;

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
            session2 = new MultiplayerSession();

            connection = new GrpcConnection("localhost", server.Port);
            session.OpenClient(connection);

            connection2 = new GrpcConnection("localhost", server.Port);
            session2.OpenClient(connection2);

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
            session2?.CloseClient();
            if (connection != null)
                await connection.CloseAsync();
            if (connection2 != null)
                await connection2.CloseAsync();
            if (server != null)
                await server.CloseAsync();
        }

        private static IEnumerable<AsyncUnitTests.AsyncTestInfo> GetTests()
        {
            return AsyncUnitTests.FindAsyncTestsInClass<MultiplayerSessionTests>();
        }

        [Test]
        public void TestAsync([ValueSource(nameof(GetTests))] AsyncUnitTests.AsyncTestInfo test)
        {
            AsyncUnitTests.RunAsyncTest(this, test);
        }

        [AsyncTest]
        public async Task ValueChanged_ClientGetsUpdate()
        {
            service.SetValueDirect("abc", 1.2);

            void HasReceivedKey() => CollectionAssert.Contains(session.SharedStateDictionary.Keys,
                                                               "abc");

            await AsyncAssert.PassesWithinTimeout(HasReceivedKey);
        }

        [AsyncTest]
        public async Task ValueChanged_ClientCallback()
        {
            var callback = Substitute.For<Action<string, object>>();
            session.SharedStateDictionaryKeyUpdated += callback;

            void HasReceivedCallback() =>
                callback.Received(1).Invoke(Arg.Is("abc"), Arg.Any<object>());

            service.SetValueDirect("abc", 1.2);

            await AsyncAssert.PassesWithinTimeout(HasReceivedCallback);
        }

        [AsyncTest]
        public async Task ValueChanged_MultiplayerResource()
        {
            var value = session.GetSharedResource("abc");
            service.SetValueDirect("abc", 1.2);

            void HasReceivedValue() => Assert.AreEqual(1.2, value.Value);

            await AsyncAssert.PassesWithinTimeout(HasReceivedValue);
        }

        [AsyncTest]
        public async Task ValueChanged_MultiplayerResourceCallback()
        {
            var callback = Substitute.For<Action>();
            var value = session.GetSharedResource("abc");
            value.ValueChanged += callback;

            service.SetValueDirect("abc", 1.2);

            void HasReceivedCallback() => callback.Received(1).Invoke();

            await AsyncAssert.PassesWithinTimeout(HasReceivedCallback);
        }

        [AsyncTest]
        public async Task TryLock_Success()
        {
            server.ReplyLatency = 400;
            var value = session.GetSharedResource("abc");
            value.ObtainLock();
            Assert.AreEqual(MultiplayerResourceLockState.Pending, value.LockState);

            void LockSuccessful()
            {
                Assert.AreEqual(MultiplayerResourceLockState.Locked, value.LockState);
                Assert.IsTrue(service.Locks.TryGetValue("abc", out var v)
                           && v.Equals(session.AccessToken));
            }

            await AsyncAssert.PassesWithinTimeout(LockSuccessful, timeout: 2000);
        }

        [AsyncTest]
        public async Task TryLock_SomeoneElseHasLock()
        {
            var value2 = session2.GetSharedResource("abc");
            value2.ObtainLock();

            void DoesPlayer2HaveLock() => Assert.IsTrue(service.Locks.TryGetValue("abc", out var v1)
                                                     && v1.Equals(session2.AccessToken));

            await AsyncAssert.PassesWithinTimeout(DoesPlayer2HaveLock);

            var value1 = session.GetSharedResource("abc");
            value1.ObtainLock();

            Assert.AreEqual(MultiplayerResourceLockState.Pending, value1.LockState);

            void IsPlayer1LockRejected() =>
                Assert.AreEqual(MultiplayerResourceLockState.Unlocked, value1.LockState);


            await AsyncAssert.PassesWithinTimeout(IsPlayer1LockRejected);
            await AsyncAssert.PassesWithinTimeout(DoesPlayer2HaveLock);
        }

        [AsyncTest]
        public async Task TryLock_SomeoneElseHadLockThenReleased()
        {
            var value2 = session2.GetSharedResource("abc");
            value2.ObtainLock();

            void DoesPlayer2HaveLock() => Assert.IsTrue(service.Locks.TryGetValue("abc", out var v1)
                                                     && v1.Equals(session2.AccessToken));

            await AsyncAssert.PassesWithinTimeout(DoesPlayer2HaveLock);

            var value1 = session.GetSharedResource("abc");
            value1.ObtainLock();

            Assert.AreEqual(MultiplayerResourceLockState.Pending, value1.LockState);

            void IsPlayer1LockRejected() =>
                Assert.AreEqual(MultiplayerResourceLockState.Unlocked, value1.LockState);

            void IsPlayer1LockAccepted() =>
                Assert.AreEqual(MultiplayerResourceLockState.Locked, value1.LockState);

            void DoesPlayer1HaveLock() => Assert.IsTrue(service.Locks.TryGetValue("abc", out var v1)
                                                     && v1.Equals(session.AccessToken));

            await AsyncAssert.PassesWithinTimeout(IsPlayer1LockRejected);
            await AsyncAssert.PassesWithinTimeout(DoesPlayer2HaveLock);

            value2.ReleaseLock();

            void NoLocks() => CollectionAssert.IsEmpty(service.Locks);

            await AsyncAssert.PassesWithinTimeout(NoLocks);

            value1.ObtainLock();
            Assert.AreEqual(MultiplayerResourceLockState.Pending, value1.LockState);

            await AsyncAssert.PassesWithinTimeout(IsPlayer1LockAccepted);
            await AsyncAssert.PassesWithinTimeout(DoesPlayer1HaveLock);
        }
    }
}
