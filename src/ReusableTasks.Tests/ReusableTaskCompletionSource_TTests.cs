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

using System;
using System.Collections.Generic;
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
        public async Task DoNotForceAsyncWithSyncContext ()
        {
            await TestSynchronizationContext.Instance;
            TestSynchronizationContext.Instance.ResetCounts ();

            var tcs = new ReusableTaskCompletionSource<int> (false);
            tcs.SetResult (1);

            // If we are not forcing async, we do allow synchronous completion.
            Assert.IsTrue (tcs.Task.IsCompleted, "#1");
            await tcs.Task;

            Assert.AreEqual (0, TestSynchronizationContext.Instance.Posted, "#2");
        }

        [Test]
        public async Task ForceAsyncWithSyncContext ()
        {
            await TestSynchronizationContext.Instance;
            TestSynchronizationContext.Instance.ResetCounts ();

            var tcs = new ReusableTaskCompletionSource<int> (true);
            tcs.SetResult (1);

            // If we're forcing async, we have to explicitly disallow synchronous completion too.
            Assert.IsFalse (tcs.Task.IsCompleted, "#1");
            await tcs.Task;

            Assert.AreEqual (1, TestSynchronizationContext.Instance.Posted, "#2");
        }

        [Test]
        public async Task NotInCache ()
        {
            _ = new ReusableTaskCompletionSource<int> ();
            Assert.AreEqual (0, ReusableTaskMethodBuilder<int>.CacheCount, "#1");

            var tcs = new ReusableTaskCompletionSource<int> ();
            tcs.SetResult (5);
            await tcs.Task;
            Assert.AreEqual (0, ReusableTaskMethodBuilder<int>.CacheCount, "#2");

            tcs = new ReusableTaskCompletionSource<int> ();
            tcs.SetResult (5);
            await tcs.Task;
            Assert.AreEqual (0, ReusableTaskMethodBuilder<int>.CacheCount, "#2");
        }

        [Test]
        public async Task UseTwice ()
        {
            var tcs = new ReusableTaskCompletionSource<int> ();

            tcs.SetResult (1);
            Assert.IsTrue (tcs.Task.IsCompleted, "#1");
            Assert.AreEqual (1, await tcs.Task, "#2");
            Assert.IsFalse (tcs.Task.IsCompleted, "#3");
            Assert.AreEqual (0, ReusableTaskMethodBuilder<int>.CacheCount, "#4");

            tcs.SetResult (2);
            Assert.AreEqual (2, await tcs.Task, "#6");
            Assert.AreEqual (0, ReusableTaskMethodBuilder<int>.CacheCount, "#7");
        }

        [Test]
        public async Task StressTest_NoReuse ()
        {
            var tasks = new List<Task> ();

            for (int count = 0; count < Environment.ProcessorCount * 2; count++) {
                tasks.Add (Task.Run (async () => {
                    for (int i = 0; i < 50000; i++) {
                        var tcs = new ReusableTaskCompletionSource<int> ();
                        await Task.WhenAll (
                            Task.Run (() => { tcs.SetResult (111); }),
                            Task.Run (async () => {
                                var result = await tcs.Task;
                                Assert.AreEqual (111, result);
                            })
                        );
                    }
                }));
            }

            await Task.WhenAll (tasks);
        }

        [Test]
        public async Task StressTest_Reuse ()
        {
            var tasks = new List<Task> ();

            for (int count = 0; count < Environment.ProcessorCount * 2; count++) {
                tasks.Add (Task.Run (async () => {
                    var tcs = new ReusableTaskCompletionSource<int> ();
                    for (int i = 0; i < 50000; i++) {
                        await Task.WhenAll (
                            Task.Run (() => { tcs.SetResult (111); }),
                            Task.Run (async () => {
                                var result = await tcs.Task;
                                Assert.AreEqual (111, result);
                            })
                        );
                    }
                }));
            }

            await Task.WhenAll (tasks);
        }
    }
}
