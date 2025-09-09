using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Grpc.Core;
using Nanover.Grpc.Multiplayer;
using Nanover.Testing.Async;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using NUnit.Framework;
using UnityEngine;

namespace Nanover.Grpc.Tests.Multiplayer
{
    public class MultiplayerCollectionTests
    {
        private MultiplayerService service;
        private GrpcServer server;
        private MultiplayerSession session;
        private GrpcConnection connection;

        [DataContract]
        private class Item
        {
            [DataMember(Name = "named_field")]
            public string NamedField;
        }
        
        private class ItemCollection : MultiplayerCollection<Item>
        {
            public ItemCollection(MultiplayerSession session) : base(session)
            {
            }

            protected override string KeyPrefix => "item.";
            
            protected override bool ParseItem(string key, object value, out Item parsed)
            {
                if (value is Dictionary<string, object> dict)
                {
                    parsed = Core.Serialization.Serialization.FromDataStructure<Item>(value);
                    return true;
                }

                parsed = null;
                return false;
            }

            protected override object SerializeItem(Item item)
            {
                return Core.Serialization.Serialization.ToDataStructure(item);
            }
        }
        
        private ItemCollection collection;

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
            collection = new ItemCollection(session);

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

        private static IEnumerable<AsyncUnitTests.AsyncTestInfo> GetTests()
        {
            return AsyncUnitTests.FindAsyncTestsInClass<MultiplayerCollectionTests>();
        }

        [Test]
        public void TestAsync([ValueSource(nameof(GetTests))] AsyncUnitTests.AsyncTestInfo test)
        {
            AsyncUnitTests.RunAsyncTest(this, test);
        }

        [Test]
        public void Initial_EmptyCollection()
        {
            Assert.IsEmpty(collection.Values);
        }

        [AsyncTest]
        public async Task ServerInsertItem()
        {
            service.SetValueDirect("item.abc", new Dictionary<string, object>());
            await AsyncAssert.PassesWithinTimeout(() =>
            {
                Assert.AreEqual(1, collection.Values.Count());
                CollectionAssert.Contains(service.Resources.Keys, "item.abc");
            });
        }
        
        [AsyncTest]
        public async Task ClientInsertItem()
        {
            collection.UpdateValue("item.abc", new Item());
            await AsyncAssert.PassesWithinTimeout(() =>
            {
                Assert.AreEqual(1, collection.Values.Count());
                CollectionAssert.Contains(service.Resources.Keys, "item.abc");
            });
        }
        
        [AsyncTest]
        public async Task ClientInsertWithDelay()
        {
            server.StreamLatency = 1000;
            
            var handler = Substitute.For<Action<string, object>>();
            session.SharedStateDictionaryKeyUpdated += handler;
            
            collection.UpdateValue("item.abc", new Item());
            await AsyncAssert.PassesWithinTimeout(() =>
            {
                Assert.AreEqual(1, collection.Values.Count());
                CollectionAssert.Contains(service.Resources.Keys, "item.abc");
                handler.DidNotReceive().Invoke(Arg.Any<string>(), Arg.Any<object>());
            }, 200);
        }
        
        [AsyncTest]
        public async Task ServerRemoveItem()
        {
            await ServerInsertItem();
            service.RemoveValueDirect("item.abc");
            await AsyncAssert.PassesWithinTimeout(() =>
            {
                Assert.AreEqual(0, collection.Values.Count());
                CollectionAssert.DoesNotContain(service.Resources.Keys, "item.abc");
            });
        }
        
        [AsyncTest]
        public async Task ClientRemoveItem()
        {
            await ClientInsertItem();
            collection.RemoveValue("item.abc");
            await AsyncAssert.PassesWithinTimeout(() =>
            {
                Assert.AreEqual(0, collection.Values.Count());
                CollectionAssert.DoesNotContain(service.Resources.Keys, "item.abc");
            });
        }
        
        [AsyncTest]
        public async Task ClientRemoveItemWithDelay()
        {
            server.StreamLatency = 1000;
            
            var handler = Substitute.For<Action<string, object>>();
            session.SharedStateDictionaryKeyUpdated += handler;
            
            await ClientInsertItem();

            collection.RemoveValue("item.abc");
            await AsyncAssert.PassesWithinTimeout(() =>
            {
                Assert.AreEqual(0, collection.Values.Count());
                CollectionAssert.DoesNotContain(service.Resources.Keys, "item.abc");
                handler.DidNotReceive().Invoke(Arg.Any<string>(), Arg.Any<object>());
            }, 200);
        }
        
        [AsyncTest]
        public async Task ClientUpdate()
        {
            service.SetValueDirect("item.abc", new Dictionary<string, object>()
            {
                ["named_field"] = "old_value"
            });
            await AsyncAssert.PassesWithinTimeout(() =>
            {
                Assert.IsTrue(collection.ContainsKey("item.abc"));
                Assert.AreEqual("old_value", collection.GetValue("item.abc").NamedField);
            });
            
            collection.UpdateValue("item.abc", new Item()
            {
                NamedField = "new_value"
            });
            
            await AsyncAssert.PassesWithinTimeout(() =>
            {
                Assert.IsTrue(collection.ContainsKey("item.abc"));
                Assert.AreEqual("new_value", collection.GetValue("item.abc").NamedField);
            });
        }
        
        [AsyncTest]
        public async Task ServerSetInvalidItem()
        {
            await ServerInsertItem();
            service.SetValueDirect("item.abc", "invalid");
            await AsyncAssert.PassesWithinTimeout(() =>
            {
                Assert.AreEqual(0, collection.Values.Count());
            });
        }
    }
}