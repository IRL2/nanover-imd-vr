using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Grpc.Core;
using Nanover.Core.Async;

namespace Nanover.Grpc.Stream
{
    /// <summary>
    /// Delegate for a gRPC server streaming call
    /// </summary>
    public delegate AsyncServerStreamingCall<TReply> ServerStreamingCall<in TRequest, TReply>(
        TRequest request,
        Metadata headers = null,
        DateTime? deadline = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Wraps the incoming response stream of a gRPC call and raises an event
    /// when new content is received.
    /// </summary>
    public sealed class IncomingStream<TIncoming> : Cancellable
    {
        /// <summary>
        /// Callback for when a new item is received from the stream.
        /// </summary>
        public event Action<TIncoming> MessageReceived;

        private AsyncServerStreamingCall<TIncoming> streamingCall;
        private UniTask? iterationTask;

        private IncomingStream(params CancellationToken[] externalTokens) : base(externalTokens)
        {
        }

        /// <summary>
        /// Call a gRPC method with the provided <paramref name="request" />,
        /// and return a stream which has not been started yet.
        /// </summary>
        public static IncomingStream<TIncoming> CreateStreamFromServerCall<TRequest>(
            ServerStreamingCall<TRequest, TIncoming> grpcCall,
            TRequest request,
            params CancellationToken[] externalTokens)
        {
            var stream = new IncomingStream<TIncoming>(externalTokens);

            stream.streamingCall = grpcCall(request,
                                            Metadata.Empty,
                                            null,
                                            stream.GetCancellationToken());

            return stream;
        }

        /// <summary>
        /// Start consuming the stream and raising events. Returns the
        /// iteration task.
        /// </summary>
        public void StartReceiving()
        {
            if (iterationTask != null)
                throw new InvalidOperationException("Streaming has already started.");

            if (IsCancelled)
                throw new InvalidOperationException("Stream has already been closed.");

            iterationTask = Iterate();

            async UniTask Iterate()
            {
                var enumerator = streamingCall.ResponseStream;
                var token = GetCancellationToken();

                try
                {
                    while (await enumerator.MoveNext(token))
                    {
                        token.ThrowIfCancellationRequested();
                        MessageReceived(enumerator.Current);
                        await UniTask.DelayFrame(1);
                    }
                }
                catch (RpcException)
                {
                    token.ThrowIfCancellationRequested();
                    throw;
                }
            }
        }

        public void Close()
        {
            Cancel();
        }
    }
}