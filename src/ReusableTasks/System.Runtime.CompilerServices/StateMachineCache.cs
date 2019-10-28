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

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Not intended to be used directly.
    /// </summary>
    class StateMachineCache<TStateMachine>
        where TStateMachine : IAsyncStateMachine
    {
#pragma warning disable RECS0108 // Warns about static fields in generic types
        static readonly Stack<StateMachineCache<TStateMachine>> Cache = new Stack<StateMachineCache<TStateMachine>> ();
#pragma warning restore RECS0108 // Warns about static fields in generic types

        public static StateMachineCache<TStateMachine> GetOrCreate ()
        {
            lock (Cache)
                return Cache.Count > 0 ? Cache.Pop () : new StateMachineCache<TStateMachine> ();
        }

        TStateMachine StateMachine;
        readonly Action OnCompletedAction;

        public StateMachineCache ()
        {
            OnCompletedAction = OnCompleted;
        }

        public void AwaitOnCompleted<TAwaiter> (ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
        {
            StateMachine = stateMachine;
            awaiter.OnCompleted (OnCompletedAction);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter> (ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
        {
            StateMachine = stateMachine;
            awaiter.UnsafeOnCompleted (OnCompletedAction);
        }

        void OnCompleted ()
        {
            var sm = StateMachine;
            StateMachine = default;

            lock (Cache)
                Cache.Push (this);
            sm.MoveNext ();
        }
    }
}
