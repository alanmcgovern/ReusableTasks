﻿//
// ReusableTaskAwaiter_T.cs
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


using System.Runtime.ExceptionServices;

using ReusableTasks;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Not intended to be used directly.
    /// </summary>
    public readonly struct ReusableTaskAwaiter<T> : INotifyCompletion, IReusableTaskAwaiter
    {
        internal readonly ResultHolder<T> ResultHolder;
        readonly int Id;
        readonly T Result;

        /// <summary>
        /// 
        /// </summary>
        public bool IsCompleted => ResultHolder == null || (ResultHolder.HasValue && !ResultHolder.ForceAsynchronousContinuation);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="resultHolder"></param>
        /// <param name="result"></param>
        internal ReusableTaskAwaiter (int id, ResultHolder<T> resultHolder, in T result)
        {
            Id = id;
            ResultHolder = resultHolder;
            Result = result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public T GetResult ()
        {
            // Represents a successfully completed task no data.
            if (ResultHolder == null)
                return Result;

            if (ResultHolder.Id != Id)
                throw new InvalidTaskReuseException ("A mismatch was detected between the ResuableTask and its Result source. This typically means the ReusableTask was awaited twice concurrently. If you need to do this, convert the ReusableTask to a Task before awaiting.");

            var result = ResultHolder.GetResult ();
            var exception = ResultHolder.Exception;
            ReusableTaskMethodBuilder<T>.Release (ResultHolder);

            if (exception != null)
                ExceptionDispatchInfo.Capture (exception).Throw ();
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="continuation"></param>
        public void OnCompleted (Action continuation)
        {
            if (ResultHolder.Id != Id)
                throw new InvalidTaskReuseException ("A mismatch was detected between the ResuableTask and its Result source. This typically means the ReusableTask was awaited twice concurrently. If you need to do this, convert the ReusableTask to a Task before awaiting.");

            ResultHolder.Continuation = continuation;
        }
    }
}
