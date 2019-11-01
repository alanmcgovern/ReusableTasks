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

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Not intended to be used directly.
    /// </summary>
    class ResultHolder<T>
    {
        Action continuation;
        Exception exception;
        T result;

#pragma warning disable RECS0108 // Warns about static fields in generic types
        static readonly SendOrPostCallback InvokeOnContext = state => ((Action) state).Invoke ();
        static readonly WaitCallback InvokeOnThreadPool = state => ((Action) state).Invoke ();
#pragma warning restore RECS0108 // Warns about static fields in generic types

        int state;

        public bool Cacheable {
            get => (state & 1) == 1;
            set {
                if (value)
                    state |= 1;
                else
                    state &= ~1;
            }
        }

        public Action Continuation {
            get => continuation;
            set {
                Action tmp = null;
                lock (this) {
                    continuation = value;
                    if (HasValue)
                        tmp = continuation;
                }
                TryInvoke (tmp);
            }
        }

        public Exception Exception {
            get => exception;
            set {
                Action tmp = null;
                lock (this) {
                    exception = value;
                    HasValue = true;
                    tmp = continuation;
                }
                TryInvoke (tmp);
            }
        }

        public bool HasValue {
            get => (state & (1 << 1)) == 1 << 1;
            set {
                if (value)
                    state |= (1 << 1);
                else
                    state &= ~(1 << 1);
            }
        }

        public SynchronizationContext SyncContext {
            get; set;
        }

        public int Token { get; set; }

        public T Value {
            get => result;
            set {
                Action tmp = null;
                lock (this) {
                    result = value;
                    HasValue = true;
                    tmp = continuation;
                }
                TryInvoke (tmp);
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
            HasValue = false;
            result = default;
            SyncContext = null;
            Token = 0;
        }

        void TryInvoke (Action callback)
        {
            // If we are supposed to execute on the captured sync context, use it. Otherwise
            // we should ensure the continuation executes on the threadpool. If the user has
            // created a dedicated thread (or whatever) we do not want to be executing on it.
            if (callback != null) {
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
}
