using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace Nanover.Grpc.Tests
{
    /// <summary>
    /// Wrapper around a <see cref="IServerStreamWriter{T}"/>, which adds latency before writing
    /// every message.
    /// </summary>
    internal class DelayedStreamWriter<TResponse> : IServerStreamWriter<TResponse>
    {
        public int Latency { get; set; }
        private IServerStreamWriter<TResponse> wrapped;

        public DelayedStreamWriter(IServerStreamWriter<TResponse> wrapped)
        {
            this.wrapped = wrapped;
        }
        
        public async Task WriteAsync(TResponse message)
        {
            if (Latency == 0)
                await wrapped.WriteAsync(message);
            else
                SendInBackground(message);
        }

        private async void SendInBackground(TResponse message)
        {
            await Task.Delay(Latency);
            await wrapped.WriteAsync(message);
        }

        public WriteOptions WriteOptions
        {
            get => wrapped.WriteOptions;
            set => wrapped.WriteOptions = value;
        }
    }
}