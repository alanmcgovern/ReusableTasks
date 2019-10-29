//
// ReusableTask.cs
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
#pragma warning disable RECS0108 // Warns about static fields in generic types

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ReusableTasks
{
    /// <summary>
    /// This is equivalent to a <see cref="System.Threading.Tasks.Task"/>, except the underlying <see cref="ReusableTask"/>
    /// instance is cached and re-used. If an async method using <see cref="ReusableTask"/> is declared,
    /// the returned <see cref="ReusableTask"/> must be awaited exactly once. If the <see cref="ReusableTask"/>
    /// is not awaited then it will not be returned to the cache for reuse. There are no other negative effects.
    /// If an instance of <see cref="ReusableTask"/> is awaited twice, then it will corrupt the cache and
    /// future behaviour will be indeterminate.
    /// </summary>
    [AsyncMethodBuilder(typeof(ReusableTaskMethodBuilder))]
    public class ReusableTask
    {
        /// <summary>
        /// Gets an instance of <see cref="ReusableTask"/> which has already been completed. It is safe
        /// to await this instance multiple times.
        /// </summary>
        public static ReusableTask CompletedTask = CreateCompletedTask ();

        static ReusableTask CreateCompletedTask ()
        {
            var builder = ReusableTaskMethodBuilder.CreateUncachedUnresettable ();
            builder.SetResult ();
            return builder.Task;
        }

        ReusableTaskAwaiter Awaiter { get; }

        /// <summary>
        /// Returns true if the task has completed.
        /// </summary>
        public bool IsCompleted => Result.HasValue;

        internal ResultHolder<EmptyStruct> Result { get; }

        internal ReusableTask (ReusableTaskMethodBuilder builder)
        {
            Awaiter = new ReusableTaskAwaiter (builder);
            Result = new ResultHolder<EmptyStruct> ();
        }

        /// <summary>
        /// Converts this <see cref="ReusableTask"/> into a standard
        /// <see cref="System.Threading.Tasks.Task"/>
        /// </summary>
        /// <returns></returns>
        public async Task AsTask ()
            => await this;

        /// <summary>
        /// Configures the awaiter used by this <see cref="ReusableTask{T}"/>
        /// </summary>
        /// <param name="continueOnCapturedContext">If <see langword="true"/> then the continuation will
        /// be invoked on the captured <see cref="System.Threading.SynchronizationContext"/>, otherwise
        /// the continuation will be executed on a <see cref="ThreadPool"/> thread.</param>
        /// <returns></returns>
        public ReusableTask ConfigureAwait (bool continueOnCapturedContext)
        {
            if (continueOnCapturedContext)
                Result.SyncContext = SynchronizationContext.Current;
            else
                Result.SyncContext = null;
            return this;
        }

        /// <summary>
        /// Gets the awaiter used to await this <see cref="ReusableTask"/>
        /// </summary>
        /// <returns></returns>
        public ReusableTaskAwaiter GetAwaiter ()
            => Awaiter;

        internal void Reset ()
            => Result.Reset ();
    }
}
