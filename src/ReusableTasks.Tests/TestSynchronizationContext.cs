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
            thread.Start ();
        }

        public void ResetCounts ()
        {
            Posted = 0;
            Sent = 0;
        }

        public override void Post (SendOrPostCallback d, object state)
        {
            Posted ++;

            Callbacks.Add (() => d(state));
        }

        public override void Send (SendOrPostCallback d, object state)
        {
            Sent ++;

            var waiter = new ManualResetEventSlim(false);
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

        public void GetResult()
        {

        }

        void INotifyCompletion.OnCompleted(Action continuation)
            => Callbacks.Add (continuation);
    }
}
