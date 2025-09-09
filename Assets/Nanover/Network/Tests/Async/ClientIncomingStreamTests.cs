using System;
using System.Threading.Tasks;
using Nanover.Core.Async;
using Nanover.Grpc.Stream;
using Nanover.Testing.Async;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Nanover.Grpc.Tests.Async
{
    internal abstract class
        ClientIncomingStreamTests<TService, TClient, TMessage> : ClientStreamTests<
            TService, TClient, IncomingStream<TMessage>>
        where TService : IBindableService
        where TClient : IAsyncClosable, ICancellable
    {
        public abstract Task GetStreamTask(IncomingStream<TMessage> stream);

        public abstract void SetServerDelay(int delay);

        public abstract void SetServerMaxMessage(int count);

        [AsyncTest]
        public async Task CancelConnection_StreamTask_IsCancelled()
        {
            var stream = GetStream(client);
            var streamTask = GetStreamTask(stream);

            await AsyncAssert.CompletesWithinTimeout(connection.CloseAsync());

            void IsStreamTaskCancelled()
            {
                Assert.AreEqual(TaskStatus.Canceled, streamTask.Status);
                Assert.IsTrue(streamTask.IsCanceled);
            }

            await AsyncAssert.PassesWithinTimeout(IsStreamTaskCancelled);
        }

        [AsyncTest]
        public async Task CancelConnection_StartStreamTaskAfter_Exception()
        {
            var stream = GetStream(client);

            await AsyncAssert.CompletesWithinTimeout(connection.CloseAsync());

            Assert.Throws<InvalidOperationException>(() => GetStreamTask(stream));
        }

        [AsyncTest]
        public async Task CancelClient_StreamTask_IsCancelled()
        {
            var stream = GetStream(client);
            var streamTask = GetStreamTask(stream);

            await AsyncAssert.CompletesWithinTimeout(client.CloseAsync());

            void IsStreamTaskCancelled()
            {
                Assert.AreEqual(TaskStatus.Canceled, streamTask.Status);
                Assert.IsTrue(streamTask.IsCanceled);
            }

            await AsyncAssert.PassesWithinTimeout(IsStreamTaskCancelled);
        }

        [AsyncTest]
        public async Task CancelClient_StartStreamTaskAfter_Exception()
        {
            var stream = GetStream(client);

            await AsyncAssert.CompletesWithinTimeout(client.CloseAsync());

            Assert.Throws<InvalidOperationException>(() => GetStreamTask(stream));
        }


        [AsyncTest]
        public async Task CancelStream_StreamTask_IsCancelled()
        {
            var stream = GetStream(client);
            var streamTask = GetStreamTask(stream);

            await AsyncAssert.CompletesWithinTimeout(stream.CloseAsync());

            void IsStreamTaskCancelled()
            {
                Assert.AreEqual(TaskStatus.Canceled, streamTask.Status);
                Assert.IsTrue(streamTask.IsCanceled);
            }

            await AsyncAssert.PassesWithinTimeout(IsStreamTaskCancelled);
        }

        [AsyncTest]
        public async Task CancelStream_StartStreamTaskAfter_Exception()
        {
            var stream = GetStream(client);

            await AsyncAssert.CompletesWithinTimeout(stream.CloseAsync());

            Assert.Throws<InvalidOperationException>(() => GetStreamTask(stream));
        }

        [AsyncTest]
        public async Task IncomingStream_ResponseWithinTimePeriod()
        {
            SetServerDelay(100);
            SetServerMaxMessage(1);

            var callback = Substitute.For<Action<TMessage>>();
            var stream = GetStream(client);
            stream.MessageReceived += callback;

            void ClientReceivesMessage() => callback.Received(1).Invoke(Arg.Any<TMessage>());

            await AsyncAssert.PassesWithinTimeout(ClientReceivesMessage,
                                                  backgroundTask: GetStreamTask(stream));
        }

        [AsyncTest]
        public async Task IncomingStream_AwaitResponse()
        {
            SetServerDelay(100);
            SetServerMaxMessage(1);

            var callback = Substitute.For<Action<TMessage>>();

            var stream = GetStream(client);
            stream.MessageReceived += callback;
            var task = GetStreamTask(stream);

            // Wait for server to finish sending frames
            await AsyncAssert.CompletesWithinTimeout(task);
            
            callback.Received(1).Invoke(Arg.Any<TMessage>());
        }

        [AsyncTest]
        public async Task IncomingStream_WithDelay()
        {
            SetServerDelay(100);
            SetServerMaxMessage(2);

            var callback = Substitute.For<Action<TMessage>>();

            var stream = GetStream(client);
            stream.MessageReceived += callback;
            var task = GetStreamTask(stream);

            // Wait for server to finish sending frames
            await AsyncAssert.CompletesWithinTimeout(task);

            callback.Received(2).Invoke(Arg.Any<TMessage>());
        }

        [AsyncTest]
        public async Task IncomingStream_WithDelay_Interrupted()
        {
            SetServerMaxMessage(2);
            SetServerDelay(100);

            var callback = Substitute.For<Action<TMessage>>();

            var stream = GetStream(client);
            stream.MessageReceived += callback;
            var task = GetStreamTask(stream);

            await AsyncAssert.RunTasksForDuration(300, task);

            await AsyncAssert.CompletesWithinTimeout(server.CloseAsync());

            callback.Received(2).Invoke(Arg.Any<TMessage>());
        }

        [AsyncTest]
        public async Task IncomingStream_CloseStream_StopSending()
        {
            SetServerMaxMessage(5);
            SetServerDelay(100);

            var callback = Substitute.For<Action<TMessage>>();

            var stream = GetStream(client);
            stream.MessageReceived += callback;
            var task = GetStreamTask(stream);
            
            await AsyncAssert.RunTasksForDuration(180, task);

            callback.Received(2).Invoke(Arg.Any<TMessage>());

            stream.Cancel();
            
            await AsyncAssert.RunTasksForDuration(500, task);

            callback.Received(2).Invoke(Arg.Any<TMessage>());
        }
    }
}