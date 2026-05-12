using System;
using System.Collections.Generic;
using System.Threading;

namespace ReusableTasks
{
    /// <summary>
    /// This is a zero allocation collection which implements the Producer-Consumer pattern. It
    /// supports a single producer and single consumer. The capacity can be bounded, or unbounded.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsyncProducerConsumerQueue<T>
    {
        Action cancelDequeuedCallback;
        Action cancelEnqueuedCallback;

        Action CancelDequeuedCallback {
            get => cancelDequeuedCallback ?? (cancelDequeuedCallback = CancelDequeued);
        }

        Action CancelEnqueuedCallback {
            get => cancelEnqueuedCallback ?? (cancelEnqueuedCallback = CancelEnqueued);
        }

        /// <summary>
        /// The maximum number of work items which can be queued. A value of zero means there is
        /// no limit.
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        /// The number of items in the queue.
        /// </summary>
        public int Count => Queue.Count;

        /// <summary>
        /// Returns true if no more items will be added to the queue.
        /// </summary>
        public bool IsAddingCompleted { get; private set; }

        /// <summary>
        /// Returns true if the capacity  is greater than zero, indicating a limited number of
        /// items can be queued at any one time.
        /// </summary>
        public bool IsBounded => Capacity > 0;

        ReusableTaskCompletionSource<bool> Dequeued { get; }
        ReusableTaskCompletionSource<bool> Enqueued { get; }
        Queue<T> Queue { get; }
        SimpleSpinLock QueueLock { get; }

        /// <summary>
        /// Creates a new instance of <see cref="AsyncProducerConsumerQueue{T}"/>.
        /// </summary>
        /// <param name="capacity">A value of 0 means the collection has an unbounded size. A value greater
        /// than zero represents the maximum number of items which can be queued.</param>
        public AsyncProducerConsumerQueue (int capacity)
        {
            Capacity = capacity;
            Dequeued = new ReusableTaskCompletionSource<bool> (true);
            Enqueued = new ReusableTaskCompletionSource<bool> (true);
            Queue = new Queue<T> ();
            QueueLock = new SimpleSpinLock ();
        }

        void CancelDequeued () => Dequeued.TrySetCanceled ();

        void CancelEnqueued () => Enqueued.TrySetCanceled ();

        /// <summary>
        /// Sets <see cref="IsAddingCompleted"/> to true and interrupts any pending <see cref="DequeueAsync()"/>
        /// calls if the collection is already empty. Future calls to <see cref="EnqueueAsync(T)"/> will throw
        /// an <see cref="InvalidOperationException"/>.
        /// </summary>
        public void CompleteAdding ()
        {
            using (QueueLock.Enter ())
                IsAddingCompleted = true;
            Enqueued.TrySetResult (true);
        }

        /// <summary>
        /// If an item has already been enqueued, then it will be dequeued and returned synchronously. Otherwise an
        /// item must be enqueued before this will return.
        /// will be added.
        /// /// </summary>
        /// <returns></returns>
        public ReusableTask<T> DequeueAsync ()
            => DequeueAsync (CancellationToken.None);

        /// <summary>
        /// If an item has already been enqueued, then it will be dequeued and returned synchronously. Otherwise an
        /// item must be enqueued before this will return.
        /// will be added.
        /// /// </summary>
        /// <param name="token">The token used to cancel the pending dequeue.</param>
        /// <returns></returns>
        public async ReusableTask<T> DequeueAsync (CancellationToken token)
        {
            if (TryDequeue (QueueLock, Queue, IsAddingCompleted, Capacity, Dequeued, token, out var result))
                return result;

            using var registration = token == CancellationToken.None ? default : token.Register (CancelEnqueuedCallback);
            while (!TryDequeue (QueueLock, Queue, IsAddingCompleted, Capacity, Dequeued, token, out result))
                await Enqueued.Task.ConfigureAwait (false);
            return result;

            static bool TryDequeue (SimpleSpinLock queueLock, Queue<T> queue, bool isAddingCompleted, int capacity, ReusableTaskCompletionSource<bool> dequeued, CancellationToken token, out T result)
            {
                using (queueLock.Enter ()) {
                    token.ThrowIfCancellationRequested ();
                    if (queue.Count == 0 && isAddingCompleted)
                        throw new InvalidOperationException ("This queue has been marked as complete, so no further items can be added.");

                    if (queue.Count > 0) {
                        result = queue.Dequeue ();
                        if (queue.Count == capacity - 1)
                            dequeued.TrySetResult (true);
                        return true;
                    }
                }
                result = default;
                return false;
            }
        }

        /// <summary>
        /// The new item will be enqueued synchronously if the number of items already
        /// enqueued is less than the capacity. Otherwise an item must be dequeued before the new item
        /// will be added.
        /// /// </summary>
        /// <param name="value">The item to enqueue</param>
        /// <returns></returns>
        public ReusableTask EnqueueAsync (T value)
            => EnqueueAsync (value, CancellationToken.None);

        /// <summary>
        /// The new item will be enqueued synchronously if the number of items already
        /// enqueued is less than the capacity. Otherwise an item must be dequeued before the new item
        /// will be added.
        /// /// </summary>
        /// <param name="value">The item to enqueue</param>
        /// <param name="token">The token used to cancel the pending enqueue.</param>
        /// <returns></returns>
        public async ReusableTask EnqueueAsync (T value, CancellationToken token)
        {
            if (IsAddingCompleted)
                throw new InvalidOperationException ("This queue has been marked as complete, so no further items can be added.");

            if (TryEnqueue (QueueLock, Queue, in value, Capacity, IsBounded, Enqueued, token))
                return;

            using var registration = token == CancellationToken.None ? default : token.Register (CancelDequeuedCallback);
            while (!TryEnqueue (QueueLock, Queue, in value, Capacity, IsBounded, Enqueued, token))
                await Dequeued.Task.ConfigureAwait (false);

            static bool TryEnqueue (SimpleSpinLock queueLock, Queue<T> queue, in T value, int capacity, bool isBounded, ReusableTaskCompletionSource<bool> enqueued, CancellationToken token)
            {
                using (queueLock.Enter ()) {
                    token.ThrowIfCancellationRequested ();

                    if (queue.Count < capacity || !isBounded) {
                        queue.Enqueue (value);
                        if (queue.Count == 1)
                            enqueued.TrySetResult (true);
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
