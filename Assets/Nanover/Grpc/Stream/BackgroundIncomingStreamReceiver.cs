using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using Nanover.Grpc.Stream;
using UnityEngine;

namespace Nanover.Grpc.Trajectory
{
    /// <summary>
    /// A utility to start two separate tasks for an incoming stream - a background thread which
    /// is constantly polling the gRPC stream (not limited to Unity's update loop) and merging
    /// concurrent data together, and a task which runs on the main thread and invokes a callback
    /// on this data.
    /// </summary>
    public class BackgroundIncomingStreamReceiver<TResponse> where TResponse : class, IMessage<TResponse>
    {
        public static BackgroundIncomingStreamReceiver<TResponse> Start(IncomingStream<TResponse> stream, Action<TResponse> messageHandler, Action<TResponse, TResponse> merger)
        {
            return new BackgroundIncomingStreamReceiver<TResponse>(stream, messageHandler, merger);
        }

        private readonly Action<TResponse, TResponse> merger;
        private TResponse receivedDataBuffer = null;

        public readonly List<float> MessageReceiveTimes = new List<float>();

        private void LogMessageReceiveTime()
        {
            var now = Time.realtimeSinceStartup;
            MessageReceiveTimes.Add(now);

            int expired = 0;

            for (; expired < MessageReceiveTimes.Count; ++expired)
                if (now - MessageReceiveTimes[expired] <= 1)
                    break;

            for (; expired > 0; --expired)
                MessageReceiveTimes.RemoveAt(0);
        }

        private BackgroundIncomingStreamReceiver(IncomingStream<TResponse> stream, Action<TResponse> messageHandler, Action<TResponse, TResponse> merger)
        {
            this.merger = merger;
            stream.MessageReceived += ReceiveOnBackgroundThread;
            BackgroundThreadTask().Forget();
            MainThreadTask().Forget();

            async UniTask BackgroundThreadTask()
            {
                await UniTask.RunOnThreadPool(stream.StartReceiving, cancellationToken: stream.GetCancellationToken());
            }

            async UniTask MainThreadTask()
            {
                while (true)
                {
                    if (stream.IsCancelled)
                        break;

                    // Atomically swap so that there is never a conflict with the merge into the same buffer
                    var newReceivedData = Interlocked.Exchange(ref receivedDataBuffer, null);
                    if (newReceivedData != null)
                    {
                        LogMessageReceiveTime();
                        messageHandler.Invoke(newReceivedData);
                    }

                    await UniTask.DelayFrame(1);
                }
            }
        }


        private void ReceiveOnBackgroundThread(TResponse response)
        {
            // In practice this loop is never expected to be run more than twice
            // because we only have one producer and one consumer but the code
            // is still written in a more typical infinite-loop-with-break style
            // that can handle multiple producers
            while (true)
            {
                var initial = receivedDataBuffer;
                // if the buffer is empty, we can just set the new message as the new buffer
                if (initial == null)
                {
                    receivedDataBuffer = response;
                    break;
                }
                else
                {
                    // The buffer is not empty. We cannot merge into it directly because we don't know
                    // when the UI thread wakes up and tries to read from it. We also don't want to lock
                    // the UI thread. But we can afford to throw-away work done on the background thread.
                    // So what we do instead is that we create a clone, merge into it and atomically swap
                    // the whole object. At the swap step we check that the UI hasn't read the original
                    // receivedDataBuffer yet, otherwise we should throw all the work we did and just
                    // set the new message as the buffer
                    var mergeTarget = initial.Clone(); // Deep clone in case of a IMessage!
                    // Unfortunately although there is a IMessage.MergeFrom method it doesn't work for us 
                    // because the only conflict resolution strategy it has is to fail and we want new data
                    // from the last message to override the buffered data from previous messages.
                    // That's why we need an explicit merge handler.
                    // mergeTarget.MergeFrom(response);
                    merger(mergeTarget, response);
                    if (initial == Interlocked.CompareExchange(ref receivedDataBuffer, mergeTarget, initial))
                    {
                        // successful swap, means UI hasn't set receivedDataBuffer to null so
                        // UI is not processing the current (old) buffer. 
                        // Otherwise we don't want the UI to re-process the already processed data so
                        // throw away the merge work we just did and run the loop once again and it
                        // should succeed in the first "if" branch 
                        break; 
                    }
                }
            }
        }
    }
}