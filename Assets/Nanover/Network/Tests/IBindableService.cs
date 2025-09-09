using Grpc.Core;

namespace Nanover.Grpc.Tests
{
    /// <summary>
    /// Wrapper for a gRPC service that knows how to bind itself.
    /// </summary>
    internal interface IBindableService
    {
        ServerServiceDefinition BindService();
    }
}