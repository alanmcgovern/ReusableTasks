//
// ReusableTaskMethodBuilder_T.cs
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


#pragma warning disable RECS0108 // Warns about static fields in generic types

using System.Collections.Generic;
using System.Threading;

using ReusableTasks;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Not intended to be used directly.
    /// </summary>
    public struct ReusableTaskMethodBuilder<T>
    {
        static readonly Stack<ResultHolder<T>> Cache = new Stack<ResultHolder<T>> ();
        static readonly SimpleSpinLock CacheLock = new SimpleSpinLock ();

        /// <summary>
        /// The number of <see cref="ReusableTaskMethodBuilder{T}"/> instances currently in the cache.
        /// </summary>
        public static int CacheCount => Cache.Count;

        /// <summary>
        /// Removes all <see cref="ReusableTaskMethodBuilder{T}"/> instances from the cache.
        /// </summary>
        public static void ClearCache ()
        {
            using (CacheLock.Enter ())
                Cache.Clear ();
        }

        /// <summary>
        /// Not intended to be used directly. This method returns an object from the cache, or instantiates
        /// and returns a new object if the cache is empty.
        /// </summary>
        /// <returns></returns>
        public static ReusableTaskMethodBuilder<T> Create ()
            => new ReusableTaskMethodBuilder<T> ();

        internal static ResultHolder<T> GetOrCreate ()
        {
            using (CacheLock.Enter ())
                return Cache.Count > 0 ? Cache.Pop () : new ResultHolder<T> ();
        }

        /// <summary>
        /// Places the instance into the cache for re-use. This is invoked implicitly when a <see cref="ReusableTask{T}"/> is awaited.
        /// </summary>
        /// <param name="result">The instance to place in the cache</param>
        internal static void Release (ResultHolder<T> result)
        {
            // This is always resettable, but sometimes cacheable.
            result.Reset ();
            if (result.Cacheable) {
                using (CacheLock.Enter ())
                    if (Cache.Count < ReusableTaskMethodBuilder.MaximumCacheSize)
                        Cache.Push (result);
            }
        }

        ReusableTask<T> task;

        /// <summary>
        /// 
        /// </summary>
        public ReusableTask<T> Task => task;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void SetException (Exception e)
        {
            if (task.ResultHolder == null)
                task = new ReusableTask<T> (GetOrCreate ());
            task.ResultHolder.SetException (e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        public void SetResult (T result)
        {
            if (task.ResultHolder == null)
                task = new ReusableTask<T> (result);
            else
                task.ResultHolder.SetResult (result);
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
                task = new ReusableTask<T> (GetOrCreate ());
                task.ResultHolder.SyncContext = SynchronizationContext.Current;
            }

            ReusableTaskMethodBuilderCore.AwaitOnCompleted (ref awaiter, ref stateMachine);
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
                task = new ReusableTask<T> (GetOrCreate ());
                task.ResultHolder.SyncContext = SynchronizationContext.Current;
            }

            ReusableTaskMethodBuilderCore.AwaitUnsafeOnCompleted (ref awaiter, ref stateMachine);
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
