//
// ReusableTask_TTests.cs
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


#pragma warning disable IDE0062 // Make local function 'static'

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace ReusableTasks.Tests
{
    [TestFixture]
    public class ReusableTask_TTests
    {
        SynchronizationContext OriginalContext;

        [SetUp]
        public void Setup ()
        {
            TestSynchronizationContext.Instance.ResetCounts ();
            OriginalContext = SynchronizationContext.Current;
            ReusableTaskMethodBuilder<int>.ClearCache ();
        }

        [TearDown]
        public void Teardown ()
        {
            SynchronizationContext.SetSynchronizationContext (OriginalContext);
        }

        [Test]
        public async Task AsTask ()
        {
            async ReusableTask<int> Test ()
            {
                await Task.Delay (1);
                return 5;
            }

            var task = Test ().AsTask ();
            Assert.AreEqual (5, await task, "#1");
            Assert.AreEqual (5, await task, "#2");
            Assert.AreEqual (5, await task, "#3");

            Assert.AreEqual (1, ReusableTaskMethodBuilder<int>.CacheCount, "#4");
        }

        [Test]
        public void Asynchronous_GetAwaiterTwice ()
        {
            async ReusableTask<int> Test ()
            {
                await Task.Yield ();
                return 1;
            }

            var task = Test ();
            task.GetAwaiter ();
            Assert.Throws<InvalidOperationException> (() => task.GetAwaiter (), "#1");
        }

        [Test]
        public void Asynchronous_AwaiterGetResultTwice ()
        {
            async ReusableTask<int> Test ()
            {
                await Task.Yield ();
                return 1;
            }

            var task = Test ();
            var awaiter = task.GetAwaiter ();
            awaiter.GetResult ();
            Assert.Throws<InvalidOperationException> (() => awaiter.GetResult (), "#1");
        }

        [Test]
        public void Asynchronous_AwaiterOnCompletedTwice ()
        {
            async ReusableTask<int> Test ()
            {
                await Task.Yield ();
                return 1;
            }

            var task = Test ();
            var awaiter = task.GetAwaiter ();
            awaiter.OnCompleted (() => { });
            Assert.Throws<InvalidOperationException> (() => awaiter.OnCompleted (() => { }), "#1");
        }

        [Test]
        public async Task Asynchronous_ConfigureAwaitFalse ()
        {
            var context = TestSynchronizationContext.Instance;
            SynchronizationContext.SetSynchronizationContext (context);

            async ReusableTask<int> Test ()
            {
                await Task.Delay (1).ConfigureAwait (false);
                await Task.Delay (1).ConfigureAwait (false);
                return 1;
            }

            await Test ().ConfigureAwait (false);
            Assert.AreEqual (0, context.Posted + context.Sent, "#1");
        }

        [Test]
        public async Task Asynchronous_ConfigureAwaitTrue ()
        {
            var context = TestSynchronizationContext.Instance;
            SynchronizationContext.SetSynchronizationContext (context);

            async ReusableTask<int> Test ()
            {
                await Task.Delay (1).ConfigureAwait (true);
                await Task.Delay (1).ConfigureAwait (true);
                return 1;
            }

            await Test ().ConfigureAwait (true);
            Assert.AreEqual (2, context.Posted + context.Sent, "#1");
        }

        [Test]
        public void Asynchronous_Exception ()
        {
            async ReusableTask<int> Test ()
            {
                await Task.Yield ();
                throw new TimeoutException ();
            }

            Assert.ThrowsAsync<TimeoutException> (async () => await Test ());
            Assert.AreEqual (1, ReusableTaskMethodBuilder<int>.CacheCount, "#1");
        }

        [Test]
        public async Task Asynchronous_IsCompleted ()
        {
            var firstWaiter = new SemaphoreSlim (0, 1);
            var secondWaiter  = new SemaphoreSlim (0, 1);

            async ReusableTask<int> Test ()
            {
                firstWaiter.Release ();
                await secondWaiter.WaitAsync ();
                return 1;
            }

            var task = Test ();
            await firstWaiter.WaitAsync ();
            Assert.IsFalse (task.IsCompleted, "#1");

            secondWaiter.Release ();
            int i = 0;
            while (!task.IsCompleted && i++ < 1000)
                await Task.Yield ();

            Assert.IsTrue (i < 1000, "#2");
            Assert.IsTrue (task.IsCompleted, "#3");
        }

        [Test]
        public async Task Asynchronous_IsCompleted_ThenAwait ()
        {
            async ReusableTask<int> Test ()
            {
                await Task.Yield ();
                return 1;
            }

            var task = Test ();
            await task;
            Assert.IsFalse (task.IsCompleted, "#1");
        }

        [Test]
        public async Task Asynchronous_ThreeConcurrent ()
        {
            async ReusableTask<int> Test ()
            {
                await Task.Yield ();
                return 1;
            }

            var t1 = Test ();
            var t2 = Test ();
            var t3 = Test ();

            await t1;
            Assert.AreEqual (1, ReusableTaskMethodBuilder<int>.CacheCount, "#1");
            await t2;
            Assert.AreEqual (2, ReusableTaskMethodBuilder<int>.CacheCount, "#2");
            await t3;
            Assert.AreEqual (3, ReusableTaskMethodBuilder<int>.CacheCount, "#3");
        }

        [Test]
        public async Task Asynchronous_ThreeSequential ()
        {
            async ReusableTask<int> Test ()
            {
                await Task.Yield ();
                return 1;
            }

            await Test ();
            await Test ();
            await Test ();

            Assert.AreEqual (1, ReusableTaskMethodBuilder<int>.CacheCount, "#1");
        }

        [Test]
        public async Task CompletedTask ()
        {
            await ReusableTask.CompletedTask;
            await ReusableTask.CompletedTask;
            await ReusableTask.CompletedTask;
        }

        [Test]
        public async Task Synchronous_ConfigureAwaitFalse ()
        {
            var context = TestSynchronizationContext.Instance;
            SynchronizationContext.SetSynchronizationContext (context);

            async ReusableTask<int> Test ()
            {
                return 1;
            }

            await Test ().ConfigureAwait (false);
            Assert.AreEqual (0, context.Posted + context.Sent, "#1");
            Assert.AreEqual (0, ReusableTaskMethodBuilder<int>.CacheCount, "#2");
        }

        [Test]
        public async Task Synchronous_ConfigureAwaitTrue ()
        {
            var context = TestSynchronizationContext.Instance;
            SynchronizationContext.SetSynchronizationContext (context);

            async ReusableTask<int> Test ()
            {
                return 1;
            }

            await Test ().ConfigureAwait (true);
            Assert.AreEqual (0, context.Posted + context.Sent, "#1");
            Assert.AreEqual (0, ReusableTaskMethodBuilder<int>.CacheCount, "#2");
        }

        [Test]
        public void Synchronous_Exception ()
        {
#pragma warning disable CS1998 // Make local function 'static'
            async ReusableTask<int> Test ()
            {
                throw new TimeoutException ();
            }
#pragma warning restore CS1998 // Make local function 'static'

            Assert.ThrowsAsync<TimeoutException> (async () => await Test ());
            Assert.AreEqual (1, ReusableTaskMethodBuilder<int>.CacheCount, "#1");
        }

        [Test]
        public void Synchronous_IsCompleted ()
        {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async ReusableTask<int> Test ()
            {
                return 1;
            }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

            var task = Test ();
            Assert.IsTrue (task.IsCompleted, "#1");
        }

        [Test]
        public async Task Synchronous_IsCompleted_ThenAwait ()
        {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async ReusableTask<int> Test ()
            {
                return 1;
            }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

            var task = Test ();
            await task;
            Assert.IsTrue (task.IsCompleted, "#1");
        }

        [Test]
        public async Task Synchronous_ThreeConcurrent ()
        {
#pragma warning disable CS1998 // Make local function 'static'
            async ReusableTask<int> Test ()
            {
                return 1;
            }
#pragma warning restore CS1998 // Make local function 'static'

            var t1 = Test ();
            var t2 = Test ();
            var t3 = Test ();

            await t1;
            Assert.AreEqual (0, ReusableTaskMethodBuilder<int>.CacheCount, "#1");
            await t2;
            Assert.AreEqual (0, ReusableTaskMethodBuilder<int>.CacheCount, "#2");
            await t3;
            Assert.AreEqual (0, ReusableTaskMethodBuilder<int>.CacheCount, "#3");
        }

        [Test]
        public async Task Synchronous_ThreeSequential ()
        {
#pragma warning disable CS1998 // Make local function 'static'
            async ReusableTask<int> Test ()
            {
                return 1;
            }
#pragma warning restore CS1998 // Make local function 'static'

            await Test ();
            await Test ();
            await Test ();
            Assert.AreEqual (0, ReusableTaskMethodBuilder<int>.CacheCount, "#1");
        }
    }
}
