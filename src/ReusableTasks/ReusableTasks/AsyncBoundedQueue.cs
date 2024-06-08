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
            while (true) {
                using (QueueLock.Enter ()) {
                    if (Queue.Count == 0 && IsAddingCompleted)
                        throw new InvalidOperationException ("This queue has been marked as complete, so no further items can be added.");

                    if (Queue.Count > 0) {
                        token.ThrowIfCancellationRequested ();
                        var result = Queue.Dequeue ();
                        if (Queue.Count == Capacity - 1)
                            Dequeued.TrySetResult (true);
                        return result;
                    }
                }

                using (var registration = token == CancellationToken.None ? default : token.Register (CancelEnqueuedCallback))
                    await Enqueued.Task.ConfigureAwait (false);
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

            while (true) {
                using (QueueLock.Enter ()) {
                    if (Queue.Count < Capacity || !IsBounded) {
                        token.ThrowIfCancellationRequested ();
                        Queue.Enqueue (value);
                        if (Queue.Count == 1)
                            Enqueued.TrySetResult (true);
                        return;
                    }
                }
                using (var registration = token == CancellationToken.None ? default : token.Register (CancelDequeuedCallback))
                    await Dequeued.Task.ConfigureAwait (false);
            }
        }
    }
}
