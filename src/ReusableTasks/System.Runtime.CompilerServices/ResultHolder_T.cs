//
// ResultHolder.cs
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


using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using ReusableTasks;

namespace System.Runtime.CompilerServices
{
    class ResultHolder<T>
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
        : IThreadPoolWorkItem
#endif
    {
        protected const int CacheableFlag = 1 << 29;
        protected const int ForceAsynchronousContinuationFlag = 1 << 30;
        // The top 3 bits are reserved for various flags, the rest is used for the unique ID.
        protected const int IdMask = ~(CacheableFlag | ForceAsynchronousContinuationFlag);
        // When resetting the instance we want to retain the 'Cacheable' and 'ForceAsync' flags.
        protected const int RetainedFlags = CacheableFlag | ForceAsynchronousContinuationFlag;

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        internal static void Invoker (object state)
        {
            Debug.Assert (state is Action || state is StateMachineCache);
            if (state is Action action)
                action.Invoke ();
            else
                Unsafe.As<StateMachineCache> (state).OnCompleted ();
        }

        // This is a string purely to aid debugging. it's obvious when this value is set
        protected static readonly object HasValueSentinel = "has_value_sentinal";

        protected static readonly SendOrPostCallback InvokeOnContext = Invoker;
        protected static readonly WaitCallback InvokeOnThreadPool = Invoker;

        protected object continuation;
        // Volatile: set on one thread (the awaiter), read on another (TryInvoke).
        public volatile SynchronizationContext SyncContext;
        protected int state;


        public bool Cacheable
            => (state & CacheableFlag) == CacheableFlag;

        public object Continuation {
            set {
                // If 'continuation' is set to 'null' then we have not yet set a value. In this
                // scenario we should place the compiler-supplied continuation in the field so
                // that when a value is set we can directly invoke the continuation.
                var action = Interlocked.CompareExchange (ref continuation, value, null);
                if (action == HasValueSentinel) {
                    Volatile.Write (ref continuation, value);
                    TryInvoke ();
                } else if (action != null) {
                    throw new InvalidTaskReuseException ("A mismatch was detected between the ResuableTask and its Result source. This typically means the ReusableTask was awaited twice concurrently. If you need to do this, convert the ReusableTask to a Task before awaiting.");
                }
            }
        }

        public Exception Exception {
            get; protected set;
        }

        /// <summary>
        /// The compiler/runtime uses this to check whether or not the awaitable can
        /// be completed synchronously or asynchronously. If this property is checked
        /// and 'false' is returned, then 'INotifyCompletion.OnCompleted' will be invoked
        /// with the delegate we need to asynchronously invoke. If it returns true then
        /// the compiler/runtime will go ahead and invoke the continuation itself.
        /// </summary>
        public bool HasValue
            => (state & ForceAsynchronousContinuationFlag) != ForceAsynchronousContinuationFlag
            && Volatile.Read(ref continuation) == HasValueSentinel;

        public int Id => state & IdMask;

        protected void TryInvoke ()
        {
            var ctx = SyncContext;
            // If we are supposed to execute on the captured sync context, use it. Otherwise
            // we should ensure the continuation executes on the threadpool. If the user has
            // created a dedicated thread (or whatever) we do not want to be executing on it.
            if ((state & ForceAsynchronousContinuationFlag) != ForceAsynchronousContinuationFlag) {
                if (ctx == null && Thread.CurrentThread.IsThreadPoolThread) {
                    Invoker (continuation);
                    return;
                } else if (ctx != null && SynchronizationContext.Current == ctx) {
                    Invoker (continuation);
                    return;
                }
            }

            if (ctx != null)
                ctx.Post (InvokeOnContext, continuation);
            else {
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
                // This may still have the value 'HasValueSentinel' if the value was set for the task
                // before the continuation was set. In this case we store the callback in the continuation
                // field and then enqueue the execution in the threadpool.
                ThreadPool.UnsafeQueueUserWorkItem (this, true);
#else
                ThreadPool.UnsafeQueueUserWorkItem (InvokeOnThreadPool, continuation);
#endif
            }
        }

        T value;

        public ResultHolder ()
            : this (true, false)
        {
        }

        public ResultHolder (bool cacheable, bool forceAsynchronousContinuation)
        {
            state |= cacheable ? CacheableFlag : 0;
            state |= forceAsynchronousContinuation ? ForceAsynchronousContinuationFlag : 0;
        }

        public T GetResult ()
            => value;

        public void Reset ()
        {
            Exception = null;
            SyncContext = null;
            value = default;
            state = ((state + 1) & IdMask) | (state & RetainedFlags);
            Volatile.Write (ref continuation, null);
        }

        public void SetCanceled ()
        {
            if (!TrySetExceptionOrResult (new OperationCanceledException(), default))
                throw new InvalidOperationException ("A result has already been set on this object");
        }

        public void SetException (Exception exception)
        {
            if (!TrySetExceptionOrResult (exception, default))
                throw new InvalidOperationException ("A result has already been set on this object");
        }

        public void SetResult (T result)
        {
            if (!TrySetExceptionOrResult (null, result))
                throw new InvalidOperationException ("A result has already been set on this object");
        }

        bool TrySetExceptionOrResult (Exception exception, in T result)
        {
            var continuation = Volatile.Read (ref this.continuation);
            if (continuation == HasValueSentinel)
                return false;

            // Plain stores for Exception and Value are safe here: the subsequent CAS
            // on 'continuation' is a full fence, guaranteeing these writes are visible
            Exception = exception;
            value = result;

            // If 'continuation' is set to 'null' then we have not yet set a continuation.
            // In this scenario, set the continuation to a value signifying the result is now available.
            continuation ??= Interlocked.CompareExchange (ref this.continuation, HasValueSentinel, null);

            if (continuation != null) {
                // This means the value returned by the CompareExchange was the continuation passed by the
                // compiler, so we can directly execute it now that we have set a value.
                TryInvoke ();
            }
            return true;
        }

#if !NETSTANDARD2_0 && !NETSTANDARD2_1
        void IThreadPoolWorkItem.Execute ()
        {
            Invoker (continuation);
        }
#endif
    }
}
