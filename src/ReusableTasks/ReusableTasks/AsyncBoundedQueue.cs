using System;
using System.Collections.Generic;

namespace ReusableTasks
{
    /// <summary>
    /// This is a zero allocation collection which implements the Producer-Consumer pattern. It
    /// supports a single producer and single consumer. The capacity can be bounded, or unbounded.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsyncProducerConsumerQueue<T>
    {
        /// <summary>
        /// The maximum number of work items which can be queued. A value of zero means there is
        /// no limit.
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        /// Returns true if the capacity  is greater than zero, indicating a limited number of
        /// items can be queued at any one time.
        /// </summary>
        public bool IsBounded => Capacity > 0;

        ReusableTaskCompletionSource<bool> Dequeued { get; }
        ReusableTaskCompletionSource<bool> Enqueued { get; }
        Queue<T> Queue { get; }

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
        }

        /// <summary>
        /// The new item will be enqueued synchronously if the number of items already
        /// enqueued is less than the capacity. Otherwise an item must be dequeued before the new item
        /// will be added.
        /// /// </summary>
        /// <param name="value">The item to enqueue</param>
        /// <returns></returns>
        public async ReusableTask EnqueueAsync (T value)
        {
            while (true) {
                lock (Queue) {
                    if (Queue.Count < Capacity || !IsBounded) {
                        Queue.Enqueue (value);
                        if (Queue.Count == 1)
                            Enqueued.TrySetResult (true);
                        return;
                    }
                }
                await Dequeued.Task;
            }
        }

        /// <summary>
        /// If an item has already been enqueued, then it will be dequeued and returned synchronously. Otherwise an
        /// item must be enqueued before this will return.
        /// will be added.
        /// /// </summary>
        /// <returns></returns>
        public async ReusableTask<T> DequeueAsync ()
        {
            while (true) {
                lock (Queue) {
                    if (Queue.Count > 0 || !IsBounded) {
                        var result = Queue.Dequeue ();
                        if (Queue.Count == Capacity - 1)
                            Dequeued.TrySetResult (true);
                        return result;
                    }
                }

                await Enqueued.Task;
            }
        }
    }
}
