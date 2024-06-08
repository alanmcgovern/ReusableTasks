//
// TestSynchronizationContext.cs
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


using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ReusableTasks.Tests
{
    public class TestSynchronizationContext : SynchronizationContext, INotifyCompletion
    {
        public static readonly TestSynchronizationContext Instance = new TestSynchronizationContext ();

        public int Posted;
        public int Sent;
        BlockingCollection<Action> Callbacks = new BlockingCollection<Action> ();

        readonly Thread thread;

        TestSynchronizationContext ()
        {
            thread = new Thread (state => {
                SetSynchronizationContext (this);
                while (true) {
                    try {
                        Callbacks.Take ().Invoke ();
                    } catch {
                        break;
                    }
                }
            });
            thread.IsBackground = true;
            thread.Start ();
        }

        public void ResetCounts ()
        {
            Posted = 0;
            Sent = 0;
        }

        public override void Post (SendOrPostCallback d, object state)
        {
            Posted++;

            Callbacks.Add (() => d (state));
        }

        public override void Send (SendOrPostCallback d, object state)
        {
            Sent++;

            var waiter = new ManualResetEventSlim (false);
            Action action = () => {
                d (state);
                waiter.Set ();
            };
            Callbacks.Add (action);
            waiter.Wait ();
            waiter.Dispose ();
        }

        public TestSynchronizationContext GetAwaiter ()
            => this;

        public bool IsCompleted => thread == Thread.CurrentThread;

        public void GetResult ()
        {

        }

        void INotifyCompletion.OnCompleted (Action continuation)
            => Callbacks.Add (continuation);
    }
}
