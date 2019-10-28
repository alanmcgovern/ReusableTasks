//
// ReusableTaskCompletionSource_TTests.cs
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


using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using NUnit.Framework;

namespace ReusableTasks.Tests
{
    [TestFixture]
    public class ReusableTaskCompletionSource_TTests
    {
        [SetUp]
        public void Setup ()
        {
            ReusableTaskMethodBuilder<int>.ClearCache ();
        }

        [Test]
        public void NotInCache ()
        {
            _ = new ReusableTaskCompletionSource<int> ();
            Assert.AreEqual (0, ReusableTaskMethodBuilder<int>.CacheCount, "#1");
        }

        [Test]
        public async Task UseTwice ()
        {
            var tcs = new ReusableTaskCompletionSource<int> ();
            var task = tcs.Task;

            tcs.SetResult (1);
            Assert.IsTrue (tcs.Task.IsCompleted, "#1");
            Assert.AreEqual (1, await task, "#2");
            Assert.IsFalse (tcs.Task.IsCompleted, "#3");
            Assert.AreEqual (0, ReusableTaskMethodBuilder<int>.CacheCount, "#4");

            tcs.SetResult (2);
            Assert.AreSame (task, tcs.Task, "#5");
            Assert.AreEqual (2, await task, "#6");
            Assert.AreEqual (0, ReusableTaskMethodBuilder<int>.CacheCount, "#7");
        }
    }
}
