using System;
using System.Collections.Generic;
using System.Threading;

namespace ReusableTasks
{
#if NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
    class ActionWorkItem : IThreadPoolWorkItem
    {
        static readonly Action EmptyAction = () => { };
        static readonly Stack<ActionWorkItem> Cache = new Stack<ActionWorkItem> ();
        static readonly SimpleSpinLock CacheLock = new SimpleSpinLock ();

        public Action Continuation { get; private set; } = EmptyAction;

        public static ActionWorkItem GetOrCreate (Action action)
        {
            using (CacheLock.Enter ()) {
                if (Cache.Count == 0) {
                    return new ActionWorkItem { Continuation = action };
                } else {
                    var worker = Cache.Pop ();
                    worker.Continuation = action;
                    return worker;
                }
            }
        }

        public void Execute ()
        {
            var continuation = Continuation;
            Continuation = EmptyAction;

            using (CacheLock.Enter ())
                Cache.Push (this);
            continuation ();
        }
    }
#endif
}
