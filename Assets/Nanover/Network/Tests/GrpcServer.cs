using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Nanover.Core.Async;
using Nanover.Grpc.Tests.Trajectory;

namespace Nanover.Grpc.Tests.Multiplayer
{
    /// <summary>
    /// Generic gRPC server for testing purposes.
    /// </summary>
    internal class GrpcServer : IAsyncClosable
    {
        private class ServerCredentials
        {
            public static object Insecure { get; internal set; }
        }



        private class Service
        {

        }

        private class Server
        {
            public List<ServerPort> Ports { get; internal set; }
            public List<Service> Services { get; internal set; }

            internal Task KillAsync() => throw new NotImplementedException();
            internal void Start() => throw new NotImplementedException();
        }

        private class ServerPort
        {
            public ServerPort(string v1, int v2, object insecure)
            {
            }

            public int BoundPort { get; internal set; }
        }

        private Server server;

        private LatencySimulator latency;

        public int StreamLatency
        {
            get => latency.ServerStreamLatency;
            set => latency.ServerStreamLatency = value;
        }
        
        public int ReplyLatency
        {
            get => latency.ServerReplyLatency;
            set => latency.ServerReplyLatency = value;
        }

        public GrpcServer(params IBindableService[] services) : this(
            services.Select(s => s.BindService()).ToArray())
        {
        }

        public GrpcServer(params ServerServiceDefinition[] services)
        {
            server = new Server
            {
                Ports =
                {
                    new ServerPort("localhost", 0, ServerCredentials.Insecure)
                }
            };
            latency = new LatencySimulator();
            foreach (var service in services)
                server.Services.Add(Intercept(service, latency));
            server.Start();

            Service Intercept(ServerServiceDefinition s, LatencySimulator l) => throw new NotImplementedException();
        }

        public int Port => server.Ports.First().BoundPort;

        public async Task CloseAsync()
        {
            if (server == null)
                return;
            await server.KillAsync();
            server = null;
        }

        public static (GrpcServer server, GrpcConnection connection) CreateServerAndConnection(params IBindableService[] services)
        {
            var server = new GrpcServer(services);
            return (server, new GrpcConnection("localhost", server.Port));
        }
    }
}