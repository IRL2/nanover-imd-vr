using System;
using System.Threading;

namespace Nanover.Core.Async
{
    /// <summary>
    /// A base implementation of an object that wraps a
    /// <see cref="CancellationTokenSource" /> using a linked source from other
    /// external cancellation tokens.
    /// </summary>
    public abstract class Cancellable : ICancellable, IDisposable
    {
        private CancellationTokenSource cancellationSource;

        /// <inheritdoc cref="ICancellationTokenSource.GetCancellationToken" />
        public bool IsCancelled => cancellationSource?.IsCancellationRequested ?? true;

        protected Cancellable(params CancellationToken[] externalTokens)
        {
            cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(externalTokens);
        }

        /// <inheritdoc cref="ICancellationTokenSource.GetCancellationToken" />
        public CancellationToken GetCancellationToken()
        {
            if (IsCancelled)
                throw new InvalidOperationException("Object already cancelled.");

            return cancellationSource.Token;
        }

        /// <inheritdoc cref="ICancellable.Cancel" />
        public void Cancel()
        {
            cancellationSource?.Cancel();
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public virtual void Dispose()
        {
            cancellationSource?.Dispose();
            cancellationSource = null;
        }
    }
}