//
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

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Not intended to be used directly.
    /// </summary>
    public class ReusableTaskAwaiter<T> : INotifyCompletion
    {
        ReusableTaskMethodBuilder<T> Builder { get; }

        public bool IsCompleted => Builder.Task.IsCompleted;

        internal ReusableTaskAwaiter (ReusableTaskMethodBuilder<T> builder)
        {
            Builder = builder;
        }

        public T GetResult()
        {
            var result = Builder.Task.Result.Value;
            var exception = Builder.Task.Result.Exception;
            ReusableTaskMethodBuilder<T>.Release (Builder);

            if (exception != null)
                ExceptionDispatchInfo.Capture (exception).Throw ();
            return result;
        }

        public void OnCompleted(Action continuation)
            => Builder.Task.Result.Continuation = continuation;
    }
}
