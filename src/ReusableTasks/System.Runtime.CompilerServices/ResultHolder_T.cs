﻿//
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

namespace System.Runtime.CompilerServices
{
    class ResultHolder
    {
        protected static readonly SendOrPostCallback InvokeOnContext = state => ((Action) state).Invoke ();
        protected static readonly WaitCallback InvokeOnThreadPool = state => ((Action) state).Invoke ();

        protected static Action HasValueSentinel = () => { throw new Exception ("HasValueSentinel - Should not be invoked."); };
    }

    /// <summary>
    /// Not intended to be used directly.
    /// </summary>
    class ResultHolder<T> : ResultHolder
    {
        const int CacheableFlag = 1 << 0;
        const int HasValueFlag = 1 << 1;

        Action continuation;
        Exception exception;
        int state;
        T result;

        public bool Cacheable {
            get => (state & CacheableFlag) == CacheableFlag;
            set {
                if (value)
                    state |= CacheableFlag;
                else
                    state &= ~CacheableFlag;
            }
        }

        public Action Continuation {
            get => continuation;
            set {
                // If 'continuation' is set to 'null' then we have not yet set a value. In this
                // scenario we should place the compiler-supplied continuation in the field so
                // that when a value is set we can directly invoke the continuation.
                var action = Interlocked.CompareExchange(ref continuation, value, null);
                if (action != null) {
                    // A non-null action means that the 'HasValueSentinel' was set on the field.
                    // This indicates a value has already been set, so we can execute the
                    // compiler-supplied continuation immediately.
                    TryInvoke (value);
                }
            }
        }

        public Exception Exception {
            get => exception;
            set {
                exception = value;
                state |= HasValueFlag;

                // If 'continuation' is set to 'null' then we have not yet set a continuation.
                // In this scenario, set the continuation to a value signifying the result is now available.
                var action = Interlocked.CompareExchange(ref continuation, HasValueSentinel, null);
                if (action != null) {
                    // This means the value returned by the CompareExchange was the continuation passed by the
                    // compiler, so we can directly execute it now that we have set a value.
                    TryInvoke(action);
                }
            }
        }

        public bool HasValue
            => (state & HasValueFlag) == HasValueFlag;

        public SynchronizationContext SyncContext;

        public T Value {
            get => result;
            set {
                result = value;
                state |= HasValueFlag;

                // If 'continuation' is set to 'null' then we have not yet set a continuation.
                // In this scenario, set the continuation to a value signifying the result is now available.
                var action = Interlocked.CompareExchange(ref continuation, HasValueSentinel, null);
                if (action != null) {
                    // This means the value returned by the CompareExchange was the continuation passed by the
                    // compiler, so we can directly execute it now that we have set a value.
                    TryInvoke(action);
                }
            }
        }

        public ResultHolder (bool cacheable)
        {
            Cacheable = cacheable;
        }

        public void Reset ()
        {
            continuation = null;
            exception = null;
            state &= ~HasValueFlag;
            result = default;
            SyncContext = null;
        }

        void TryInvoke (Action callback)
        {
            // If we are supposed to execute on the captured sync context, use it. Otherwise
            // we should ensure the continuation executes on the threadpool. If the user has
            // created a dedicated thread (or whatever) we do not want to be executing on it.
            if (SyncContext == null && Thread.CurrentThread.IsThreadPoolThread)
                callback ();
            else if (SyncContext != null && SynchronizationContext.Current == SyncContext)
                callback ();
            else if (SyncContext != null)
                SyncContext.Post (InvokeOnContext, callback);
            else
                ThreadPool.UnsafeQueueUserWorkItem (InvokeOnThreadPool, callback);
        }
    }
}
