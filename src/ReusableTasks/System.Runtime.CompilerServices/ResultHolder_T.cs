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


using System.Threading;
using System.Threading.Tasks;

using ReusableTasks;

namespace System.Runtime.CompilerServices
{
    class ResultHolder
    {
        protected static readonly SendOrPostCallback InvokeOnContext = state => ((Action) state).Invoke ();
        protected static readonly WaitCallback InvokeOnThreadPool = state => ((Action) state).Invoke ();

        protected static Action HasValueSentinel = () => { throw new InvalidTaskReuseException ("HasValueSentinel - Should not be invoked."); };
    }

    /// <summary>
    /// Not intended to be used directly.
    /// </summary>
    class ResultHolder<T> : ResultHolder
    {
        const int CacheableFlag = 1 << 29;
        const int ForceAsynchronousContinuationFlag = 1 << 30;
        const int SettingValueFlag = 1 << 31;
        // The top 3 bits are reserved for various flags, the rest is used for the unique ID.
        const int IdMask = ~(CacheableFlag | ForceAsynchronousContinuationFlag | SettingValueFlag);
        // When resetting the instance we want to retain the 'Cacheable' and 'ForceAsync' flags.
        const int RetainedFlags = CacheableFlag | ForceAsynchronousContinuationFlag;

        internal static ResultHolder<T> CreateUncachedCompleted ()
        {
            var result = new ResultHolder<T> (false);
            result.TrySetExceptionOrResult (null, default);
            return result;
        }

        Action continuation;
        int state;

        public bool Cacheable {
            get => (state & CacheableFlag) == CacheableFlag;
        }

        public Action Continuation {
            get => continuation;
            set {
                // If 'continuation' is set to 'null' then we have not yet set a value. In this
                // scenario we should place the compiler-supplied continuation in the field so
                // that when a value is set we can directly invoke the continuation.
                var sentinel = HasValueSentinel;
                var action = Interlocked.CompareExchange (ref continuation, value, null);
                if (action == sentinel) {
                    // A non-null action means that the 'HasValueSentinel' was set on the field.
                    // This indicates a value has already been set, so we can execute the
                    // compiler-supplied continuation immediately.
                    if (Interlocked.CompareExchange (ref continuation, value, sentinel) != sentinel)
                        throw new InvalidTaskReuseException ("A mismatch was detected when attempting to invoke the continuation. This typically means the ReusableTask was awaited twice concurrently. If you need to do this, convert the ReusableTask to a Task before awaiting.");
                    TryInvoke (value);
                } else if (action != null) {
                    throw new InvalidTaskReuseException ("A mismatch was detected between the ResuableTask and its Result source. This typically means the ReusableTask was awaited twice concurrently. If you need to do this, convert the ReusableTask to a Task before awaiting.");
                }
            }
        }

        public Exception Exception { get; private set; }

        /// <summary>
        /// The compiler/runtime uses this to check whether or not the awaitable can
        /// be completed synchronously or asynchronously. If this property is checked
        /// and 'false' is returned, then 'INotifyCompletion.OnCompleted' will be invoked
        /// with the delegate we need to asynchronously invoke. If it returns true then
        /// the compiler/runtime will go ahead and invoke the continuation itself.
        /// </summary>
        public bool HasValue
            => continuation == HasValueSentinel;

        public bool ForceAsynchronousContinuation {
            get => (state & ForceAsynchronousContinuationFlag) == ForceAsynchronousContinuationFlag;
            set {
                if (value)
                    state |= ForceAsynchronousContinuationFlag;
                else
                    state &= ~ForceAsynchronousContinuationFlag;
            }
        }

        public int Id => state & IdMask;

        public SynchronizationContext SyncContext;

        T Value { get; set; }

        public ResultHolder (bool cacheable)
            : this (cacheable, false)
        {
        }

        public ResultHolder (bool cacheable, bool forceAsynchronousContinuation)
        {
            state |= cacheable ? CacheableFlag : 0;
            ForceAsynchronousContinuation = forceAsynchronousContinuation;
        }

        public T GetResult ()
            => Value;

        public void Reset ()
        {
            continuation = null;
            Exception = null;
            SyncContext = null;
            Value = default;

            var retained = state & RetainedFlags;
            Interlocked.Exchange (ref state, ((state + 1) & IdMask) | retained);
        }

        public void SetCanceled ()
        {
            if (!TrySetCanceled ())
                throw new InvalidOperationException ("A result has already been set on this object");
        }

        public void SetException (Exception exception)
        {
            if (!TrySetException (exception))
                throw new InvalidOperationException ("A result has already been set on this object");
        }

        public void SetResult (T result)
        {
            if (!TrySetResult (result))
                throw new InvalidOperationException ("A result has already been set on this object");
        }

        public bool TrySetCanceled ()
            => TrySetExceptionOrResult (new TaskCanceledException (), default);

        public bool TrySetException (Exception exception)
            => TrySetExceptionOrResult (exception, default);

        public bool TrySetResult (T result)
            => TrySetExceptionOrResult (null, result);

        bool TrySetExceptionOrResult (Exception exception, T result)
        {
            var originalState = state;
            if ((originalState & SettingValueFlag) == SettingValueFlag)
                return false;

            if (Interlocked.CompareExchange (ref state, originalState | SettingValueFlag, originalState) != originalState)
                return false;

            // Set the exception/value and update the state
            Exception = exception;
            Value = result;

            // If 'continuation' is set to 'null' then we have not yet set a continuation.
            // In this scenario, set the continuation to a value signifying the result is now available.
            var action = Interlocked.CompareExchange (ref continuation, HasValueSentinel, null);
            if (action != null) {
                // This means the value returned by the CompareExchange was the continuation passed by the
                // compiler, so we can directly execute it now that we have set a value.
                TryInvoke (action);
            }
            return true;
        }

        void TryInvoke (Action callback)
        {
            // If we are supposed to execute on the captured sync context, use it. Otherwise
            // we should ensure the continuation executes on the threadpool. If the user has
            // created a dedicated thread (or whatever) we do not want to be executing on it.
            if (SyncContext == null && Thread.CurrentThread.IsThreadPoolThread && !ForceAsynchronousContinuation)
                callback ();
            else if (SyncContext != null && SynchronizationContext.Current == SyncContext && !ForceAsynchronousContinuation)
                callback ();
            else if (SyncContext != null)
                SyncContext.Post (InvokeOnContext, callback);
            else
#if NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                ThreadPool.UnsafeQueueUserWorkItem (ActionWorkItem.GetOrCreate (callback), false);
#else
                ThreadPool.UnsafeQueueUserWorkItem (InvokeOnThreadPool, callback);
#endif
        }
    }
}
