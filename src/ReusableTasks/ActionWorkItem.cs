using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ReusableTasks
{
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
    class ActionWorkItem : IThreadPoolWorkItem
    {
        static readonly Action EmptyAction = () => { };
        static readonly Stack<ActionWorkItem> Cache = new Stack<ActionWorkItem> ();
        static readonly SimpleSpinLock CacheLock = new SimpleSpinLock ();

        public object Continuation { get; private set; } = EmptyAction;

        public static ActionWorkItem GetOrCreate (object action)
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
            ResultHolder.Invoker (Continuation);
            Continuation = EmptyAction;

            using (CacheLock.Enter ())
                Cache.Push (this);
        }
    }
#endif
}
