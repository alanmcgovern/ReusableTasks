//
// ReusableTaskMethodBuilder.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2019 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System.Collections.Generic;
using System.Threading;

using ReusableTasks;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Not intended to be used directly.
    /// </summary>
    public struct ReusableTaskMethodBuilder
    {
        static readonly Stack<ResultHolder<EmptyStruct>> Cache = new Stack<ResultHolder<EmptyStruct>> ();
        static readonly SimpleSpinLock CacheLock = new SimpleSpinLock ();

        /// <summary>
        /// The number of <see cref="ReusableTaskMethodBuilder"/> instances currently in the cache.
        /// </summary>
        public static int CacheCount => Cache.Count;

        /// <summary>
        /// Removes all <see cref="ReusableTaskMethodBuilder"/> instances from the cache.
        /// </summary>
        public static void ClearCache ()
        {
            using (CacheLock.Enter ())
                Cache.Clear ();
        }

        /// <summary>
        /// The maximum number of instances to store in the cache. Defaults to <see langword="512"/>
        /// </summary>
        public static int MaximumCacheSize { get; set; } = 512;

        /// <summary>
        /// Not intended to be used directly. This method returns an object from the cache, or instantiates
        /// and returns a new object if the cache is empty.
        /// </summary>
        /// <returns></returns>
        public static ReusableTaskMethodBuilder Create ()
            => new ReusableTaskMethodBuilder ();

        /// <summary>
        /// Places the instance into the cache for re-use. This is invoked implicitly when a <see cref="ReusableTask"/> is awaited.
        /// </summary>
        /// <param name="result">The instance to place in the cache</param>
        internal static void Release (ResultHolder<EmptyStruct> result)
        {
            // This is neither cachable or resettable.
            result.Reset ();
            using (CacheLock.Enter ())
                if (Cache.Count < MaximumCacheSize)
                    Cache.Push (result);
        }

        ReusableTask task;

        /// <summary>
        /// 
        /// </summary>
        public ReusableTask Task => task;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void SetException (Exception e)
        {
            if (task.ResultHolder == null)
                using (CacheLock.Enter ())
                    task = new ReusableTask (Cache.Count > 0 ? Cache.Pop () : new ResultHolder<EmptyStruct> ());
            task.ResultHolder.SetException (e);
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetResult ()
        {
            if (task.ResultHolder == null)
                task = ReusableTask.CompletedTask;
            else
                task.ResultHolder.SetResult (new EmptyStruct ());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAwaiter"></typeparam>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="awaiter"></param>
        /// <param name="stateMachine"></param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine> (ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (task.ResultHolder == null) {
                using (CacheLock.Enter ())
                    task = new ReusableTask (Cache.Count > 0 ? Cache.Pop () : new ResultHolder<EmptyStruct> ());
                task.ResultHolder.SyncContext = SynchronizationContext.Current;
            }

            StateMachineCache<TStateMachine>.GetOrCreate ()
                .AwaitOnCompleted (ref awaiter, ref stateMachine);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAwaiter"></typeparam>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="awaiter"></param>
        /// <param name="stateMachine"></param>
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine> (ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (task.ResultHolder == null) {
                using (CacheLock.Enter ())
                    task = new ReusableTask (Cache.Count > 0 ? Cache.Pop () : new ResultHolder<EmptyStruct> ());
                task.ResultHolder.SyncContext = SynchronizationContext.Current;
            }

            StateMachineCache<TStateMachine>.GetOrCreate ()
                .AwaitUnsafeOnCompleted (ref awaiter, ref stateMachine);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="stateMachine"></param>
        public void Start<TStateMachine> (ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext ();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateMachine"></param>
        public void SetStateMachine (IAsyncStateMachine stateMachine)
        {
        }
    }
}
