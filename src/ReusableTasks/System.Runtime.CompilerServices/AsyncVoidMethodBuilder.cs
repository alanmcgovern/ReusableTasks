//
// AsyncVoidMethodBuilder.cs
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


#pragma warning disable CS0436 // Type conflicts with imported type

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// A reimplementation of the built-in AsyncVoidMethodBuilder which is backed by
    /// <see cref="ReusableTasks.ReusableTask"/> instead of <see cref="System.Threading.Tasks.Task"/>.
    /// </summary>
    readonly struct AsyncVoidMethodBuilder
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static AsyncVoidMethodBuilder Create () => new AsyncVoidMethodBuilder ();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void SetException (Exception e)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetResult ()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateMachine"></param>
        public void SetStateMachine (IAsyncStateMachine stateMachine)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="stateMachine"></param>
        public void Start<TStateMachine> (ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext ();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAwaiter"></typeparam>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="awaiter"></param>
        /// <param name="stateMachine"></param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine> (ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            StateMachineCache<TStateMachine>.GetOrCreate ()
                .AwaitOnCompleted (ref awaiter, ref stateMachine);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAwaiter"></typeparam>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="awaiter"></param>
        /// <param name="stateMachine"></param>
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine> (ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            StateMachineCache<TStateMachine>.GetOrCreate ()
                .AwaitUnsafeOnCompleted (ref awaiter, ref stateMachine);
        }
    }
}
