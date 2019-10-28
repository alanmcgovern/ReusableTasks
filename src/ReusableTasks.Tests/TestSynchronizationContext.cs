using System;
using System.Threading;

namespace ReusableTasks.Tests
{
    public class TestSynchronizationContext : SynchronizationContext
    {
        public int Posted;
        public int Sent;

        public override void Post (SendOrPostCallback d, object state)
        {
            Posted ++;

            ThreadPool.QueueUserWorkItem (t => {
                SetSynchronizationContext (this);
                d (state);
                SetSynchronizationContext (null);
            });
        }

        public override void Send (SendOrPostCallback d, object state)
        {
            Sent ++;

            using var waiter = new ManualResetEventSlim(false);
            ThreadPool.QueueUserWorkItem (t => {
                SetSynchronizationContext (this);
                d (state);
                SetSynchronizationContext (null);
                waiter.Set ();
            });
            waiter.Wait ();
        }
    }
}
