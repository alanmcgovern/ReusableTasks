//
// ReusableTaskTests.cs
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
    public class ReusableTaskTests
    {
        SynchronizationContext OriginalContext;

        [SetUp]
        public void Setup ()
        {
            TestSynchronizationContext.Instance.ResetCounts ();
            OriginalContext = SynchronizationContext.Current;
            ReusableTaskMethodBuilder.ClearCache ();
        }

        [TearDown]
        public void Teardown ()
        {
            SynchronizationContext.SetSynchronizationContext (OriginalContext);
        }

        [Test]
        public async Task AsTask ()
        {
            async ReusableTask Test ()
            {
                await Task.Delay (1);
            }

            var task = Test ().AsTask ();
            await task;
            await task;
            await task;

            Assert.AreEqual (1, ReusableTaskMethodBuilder.CacheCount, "#4");
        }

        [Test]
        public void Asynchronous_AwaiterGetResultTwice ()
        {
            async ReusableTask Test ()
            {
                await Task.Yield ();
            }

            var task = Test ();
            var awaiter = task.GetAwaiter ();
            awaiter.GetResult ();
            Assert.Throws<InvalidTaskReuseException> (() => awaiter.GetResult (), "#1");
        }

        [Test]
        public void Asynchronous_AwaiterOnCompletedTwice ()
        {
            async ReusableTask Test ()
            {
                await Task.Yield ();
            }

            var task = Test ();
            var awaiter = task.GetAwaiter ();
            awaiter.OnCompleted (() => { });
            Assert.Throws<InvalidTaskReuseException> (() => awaiter.OnCompleted (() => { }), "#1");
        }

        [Test]
        public async Task Asynchronous_ConfigureAwaitFalse ()
        {
            var context = TestSynchronizationContext.Instance;
            SynchronizationContext.SetSynchronizationContext (context);

            async ReusableTask Test ()
            {
                await Task.Delay (1).ConfigureAwait (false);
                await Task.Delay (1).ConfigureAwait (false);
            }

            await Test ().ConfigureAwait (false);
            Assert.AreEqual (0, context.Posted + context.Sent, "#1");
        }

        [Test]
        public async Task Asynchronous_ConfigureAwaitTrue ()
        {
            var context = TestSynchronizationContext.Instance;
            SynchronizationContext.SetSynchronizationContext (context);

            async ReusableTask Test ()
            {
                await Task.Delay (1).ConfigureAwait (true);
                await Task.Delay (1).ConfigureAwait (true);
                GC.KeepAlive (this);
            }

            await Test ().ConfigureAwait (true);
            Assert.AreEqual (2, context.Posted + context.Sent, "#1");
        }

        [Test]
        public void Asynchronous_Exception ()
        {
            async ReusableTask Test ()
            {
                await Task.Yield ();
                throw new TimeoutException ();
            }

            Assert.ThrowsAsync<TimeoutException> (async () => await Test ());
            Assert.AreEqual (1, ReusableTaskMethodBuilder.CacheCount, "#1");
        }

        [Test]
        public async Task Asynchronous_IsCompleted ()
        {
            var firstWaiter = new SemaphoreSlim (0, 1);
            var secondWaiter = new SemaphoreSlim (0, 1);

            async ReusableTask Test ()
            {
                firstWaiter.Release ();
                await secondWaiter.WaitAsync ();
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
            async ReusableTask Test ()
            {
                await Task.Yield ();
            }

            var task = Test ();
            await task;
            Assert.IsFalse (task.IsCompleted, "#1");
        }

        [Test]
        public async Task Asynchronous_ThreeConcurrent ()
        {
            async ReusableTask Test ()
            {
                await Task.Yield ();
            }

            var t1 = Test ();
            var t2 = Test ();
            var t3 = Test ();

            await t1;
            Assert.AreEqual (1, ReusableTaskMethodBuilder.CacheCount, "#1");
            await t2;
            Assert.AreEqual (2, ReusableTaskMethodBuilder.CacheCount, "#2");
            await t3;
            Assert.AreEqual (3, ReusableTaskMethodBuilder.CacheCount, "#3");
        }

        [Test]
        public async Task Asynchronous_ThreeSequential ()
        {
            async ReusableTask Test ()
            {
                await Task.Yield ();
            }

            await Test ();
            await Test ();
            await Test ();

            Assert.AreEqual (1, ReusableTaskMethodBuilder.CacheCount, "#1");
        }

        [Test]
        public async Task CompletedTask ()
        {
            await ReusableTask.CompletedTask;
            await ReusableTask.CompletedTask.ConfigureAwait (false);
            await ReusableTask.CompletedTask.ConfigureAwait (true);
            ReusableTask.CompletedTask.GetAwaiter ().GetResult ();
            Assert.IsTrue (ReusableTask.CompletedTask.IsCompleted);
            Assert.IsTrue (ReusableTask.CompletedTask.GetAwaiter ().IsCompleted);
        }

        [Test]
        public void Synchronous_Exception ()
        {
#pragma warning disable CS1998 // Make local function 'static'
            async ReusableTask Test ()
            {
                throw new TimeoutException ();
            }
#pragma warning restore CS1998 // Make local function 'static'

            Assert.ThrowsAsync<TimeoutException> (async () => await Test ());
            Assert.AreEqual (1, ReusableTaskMethodBuilder.CacheCount, "#1");
        }

        [Test]
        public async Task FromResult_TwiceSequential ()
        {
            var task = ReusableTask.FromResult (5);
            Assert.AreEqual (0, ReusableTaskMethodBuilder<int>.CacheCount);
            Assert.IsTrue (task.IsCompleted);
            Assert.AreEqual (5, await task);

            Assert.AreEqual (15, await ReusableTask.FromResult (15));
        }

        [Test]
        public async Task FromResult_TwiceConcurrent ()
        {
            var task1 = ReusableTask.FromResult (4);
            var task2 = ReusableTask.FromResult (14);

            Assert.AreEqual (14, await task2);
            Assert.AreEqual (4, await task1);
        }

        [Test]
        public void Synchronous_IsCompleted ()
        {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async ReusableTask Test ()
            {
            }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

            var task = Test ();
            Assert.IsTrue (task.IsCompleted, "#1");
        }

        [Test]
        public async Task Synchronous_IsCompleted_ThenAwait ()
        {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async ReusableTask Test ()
            {
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
            async ReusableTask Test ()
            {
            }
#pragma warning restore CS1998 // Make local function 'static'

            var t1 = Test ();
            var t2 = Test ();
            var t3 = Test ();

            await t1;
            Assert.AreEqual (0, ReusableTaskMethodBuilder.CacheCount, "#1");
            await t2;
            Assert.AreEqual (0, ReusableTaskMethodBuilder.CacheCount, "#2");
            await t3;
            Assert.AreEqual (0, ReusableTaskMethodBuilder.CacheCount, "#3");

        }

        [Test]
        public async Task Synchronous_ThreeSequential ()
        {
#pragma warning disable CS1998 // Make local function 'static'
            async ReusableTask Test ()
            {
            }
#pragma warning restore CS1998 // Make local function 'static'

            await Test ();
            await Test ();
            await Test ();
            Assert.AreEqual (0, ReusableTaskMethodBuilder.CacheCount, "#1");
        }
    }
}
