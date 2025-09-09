using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using UnityEngine;

namespace Nanover.Grpc.Tests
{
    /// <summary>
    /// gRPC interceptor which adds a configurable latency to messages streamed from the server.
    /// </summary>
    public class LatencySimulator : Interceptor
    {
        private int serverStreamLatency;

        private event Action ServerStreamLatencyChanged;

        public int ServerStreamLatency
        {
            get => serverStreamLatency;
            set
            {
                serverStreamLatency = value;
                ServerStreamLatencyChanged?.Invoke();
            }
        }
        
        public int ServerReplyLatency { get; set; }

        public override Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request,
                                                                               IServerStreamWriter<TResponse> responseStream,
                                                                               ServerCallContext context,
                                                                               ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var delayed = new DelayedStreamWriter<TResponse>(responseStream);
            ServerStreamLatencyChanged += () => delayed.Latency = serverStreamLatency;
            return base.ServerStreamingServerHandler(request, delayed, context, continuation);
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request,
                                                                                ServerCallContext context,
                                                                                UnaryServerMethod<TRequest, TResponse> continuation)
        {
            var result = await base.UnaryServerHandler(request, context, continuation);
            await Task.Delay(ServerReplyLatency);
            return result;
        }
    }
}