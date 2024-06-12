//
// StateMachineCache.cs
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


using System.Collections.Generic;
using System.Threading;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Not intended to be used directly.
    /// </summary>
    public class StateMachineCache<TStateMachine>
        where TStateMachine : IAsyncStateMachine
    {
#pragma warning disable RECS0108 // Warns about static fields in generic types
        static readonly Stack<StateMachineCache<TStateMachine>> Cache = new Stack<StateMachineCache<TStateMachine>> ();
        static readonly ReusableTasks.SimpleSpinLock CacheLock = new ReusableTasks.SimpleSpinLock ();
#pragma warning restore RECS0108 // Warns about static fields in generic types

        /// <summary>
        /// Retrieves an instance of <see cref="StateMachineCache{TStateMachine}"/> from the cache. If the cache is
        /// empty, a new instance will be created and returned. You must invoke either <see cref="AwaitOnCompleted{TAwaiter}(ref TAwaiter, ref TStateMachine)"/> or
        /// <see cref="AwaitUnsafeOnCompleted{TAwaiter}(ref TAwaiter, ref TStateMachine)"/>. This will ensure the
        /// <see cref="StateMachineCache{TStateMachine}"/> instance is added back into the cache as soon as the
        /// async continuation has been executed.
        /// </summary>
        /// <returns></returns>
        public static StateMachineCache<TStateMachine> GetOrCreate ()
        {
            using (CacheLock.Enter ())
                return Cache.Count > 0 ? Cache.Pop () : new StateMachineCache<TStateMachine> ();
        }

        TStateMachine StateMachine;
        readonly Action OnCompletedAction;

        StateMachineCache ()
        {
            OnCompletedAction = OnCompleted;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAwaiter"></typeparam>
        /// <param name="awaiter"></param>
        /// <param name="stateMachine"></param>
        public void AwaitOnCompleted<TAwaiter> (ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
        {
            StateMachine = stateMachine;
            awaiter.OnCompleted (OnCompletedAction);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAwaiter"></typeparam>
        /// <param name="awaiter"></param>
        /// <param name="stateMachine"></param>
        public void AwaitUnsafeOnCompleted<TAwaiter> (ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
        {
            StateMachine = stateMachine;
            awaiter.UnsafeOnCompleted (OnCompletedAction);
        }

        void OnCompleted ()
        {
            // Run the callback *before* pushing this object back into the cache.
            // This makes things a teeny bit more responsive.
            StateMachine.MoveNext ();
            StateMachine = default;

            using (CacheLock.Enter ())
                Cache.Push (this);
        }
    }
}
