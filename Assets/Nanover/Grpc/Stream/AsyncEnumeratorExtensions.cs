using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace Nanover.Grpc.Stream
{
    /// <summary>
    /// Extension methods for iterating over asynchronous enumerators
    /// </summary>
    public static class AsyncEnumeratorExtensions
    {
        /// <summary>
        /// Asynchronously iterate over an enumerator and execute the callback with each
        /// element.
        /// </summary>
        /// <remarks>
        /// If an exception occurs at any point in the iteration, the whole task will end.
        /// </remarks>
        public static async Task ForEachAsync<T>(this IAsyncStreamReader<T> enumerator,
                                                 Action<T> callback,
                                                 CancellationToken cancellationToken)
        {
            while (await enumerator.MoveNext(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                callback.Invoke(enumerator.Current);
                await Task.Delay(50);
            }
        }

        public static async Task ForEachAsyncDebug<T>(this IAsyncStreamReader<T> enumerator,
                                                      Action<T> callback,
                                                      CancellationToken cancellationToken,
                                                      IncomingStream<T> test)
        {
            test.WaitingItem = true;
            while (await enumerator.MoveNext(cancellationToken))
            {
                test.WaitingItem = false;
                cancellationToken.ThrowIfCancellationRequested();
                test.WaitingCall = true;
                callback.Invoke(enumerator.Current);
                test.WaitingCall = false;
                test.WaitingDelay = true;
                await Task.Delay(50);
                test.WaitingDelay = false;
                test.WaitingItem = true;
            }
        }
    }
}