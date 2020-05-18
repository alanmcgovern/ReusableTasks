//
// ReusableTaskCompletionSource.cs
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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ReusableTasks
{
    /// <summary>
    /// This is equivalent to a <see cref="TaskCompletionSource{T}"/> where the underlying <see cref="ReusableTask"/>
    /// instance is reset after it has been awaited and completed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ReusableTaskCompletionSource<T>
    {
        ResultHolder<T> Result { get; }

        /// <summary>
        /// The <see cref="ReusableTask{T}"/> controlled by this <see cref="ReusableTaskCompletionSource{T}"/>.
        /// Once the Task has been both completed and awaited it will be reset to it's initial state, allowing
        /// this <see cref="ReusableTaskCompletionSource{T}"/> instance to be reused.
        /// </summary>
        public ReusableTask<T> Task => new ReusableTask<T> (Result, SynchronizationContext.Current);

        /// <summary>
        /// Instantiates a new <see cref="ReusableTaskCompletionSource{T}"/>.
        /// </summary>
        public ReusableTaskCompletionSource ()
            : this (false)
        {
        }

        /// <summary>
        /// The <see cref="ReusableTask{T}"/> controlled by this <see cref="ReusableTaskCompletionSource{T}"/>.
        /// Once the Task has been both completed and awaited it will be reset to it's initial state, allowing
        /// this <see cref="ReusableTaskCompletionSource{T}"/> instance to be reused.
        /// </summary>
        /// <param name="forceAsynchronousContinuation">True if the continuation should always be invoked asynchronously.</param>
        public ReusableTaskCompletionSource (bool forceAsynchronousContinuation)
        {
            Result = new ResultHolder<T> (false, forceAsynchronousContinuation);
        }

        /// <summary>
        /// Moves <see cref="Task"/> to the Canceled state. 
        /// </summary>
        public void SetCanceled ()
            => Result.SetCanceled ();

        /// <summary>
        /// Moves <see cref="Task"/> to the Faulted state using the specified exception. 
        /// </summary>
        public void SetException (Exception exception)
            => Result.SetException (exception);

        /// <summary>
        /// Moves <see cref="Task"/> to the Faulted state using the specified exception. 
        /// </summary>
        public void SetResult (T result)
            => Result.SetResult (result);

        /// <summary>
        /// Returns true if the underlying task is successfully marked as canceled. Returns false
        /// if the underlying task has already completed.
        /// </summary>
        /// <returns></returns>
        internal bool TrySetCanceled ()
            => Result.TrySetCanceled  ();

        /// <summary>
        /// Returns true if the underlying task is successfully marked as faulted. Returns false
        /// if the underlying task has already completed.
        /// </summary>
        /// <returns></returns>
        internal bool TrySetException (Exception exception)
            => Result.TrySetException (exception);

        /// <summary>
        /// Returns true if the underlying task is successfully marked as completed. Returns false
        /// if the underlying task has already completed.
        /// </summary>
        /// <returns></returns>
        internal bool TrySetResult (T result)
            => Result.TrySetResult (result);
    }
}
