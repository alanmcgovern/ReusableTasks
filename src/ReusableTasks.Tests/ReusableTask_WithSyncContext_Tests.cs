//
// ReusableTask_WithSyncContext_Tests.cs
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


using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace ReusableTasks.Tests
{
    [TestFixture]
    public class ReusableTask_WithSyncContext_Tests
    {
        [Test]
        public async Task SyncMethod_ConfigureTrue ()
        {
            await TestSynchronizationContext.Instance;
            TestSynchronizationContext.Instance.ResetCounts ();

            await First ();
            Assert.AreEqual (0, TestSynchronizationContext.Instance.Posted, "#1");
        }

        [Test]
        public async Task SyncMethod_ConfigureFalse ()
        {
            await TestSynchronizationContext.Instance;
            TestSynchronizationContext.Instance.ResetCounts ();

            await Second ();
            Assert.AreEqual (0, TestSynchronizationContext.Instance.Posted, "#1");
        }

        [Test]
        public async Task AsyncMethod_ConfigureTrue ()
        {
            await TestSynchronizationContext.Instance;
            TestSynchronizationContext.Instance.ResetCounts ();

            await Third ().ConfigureAwait (false);
            Assert.AreEqual (1, TestSynchronizationContext.Instance.Posted, "#1");
        }

        [Test]
        public async Task AsyncMethod_ConfigureFalse ()
        {
            await TestSynchronizationContext.Instance;
            TestSynchronizationContext.Instance.ResetCounts ();

            await Fourth ().ConfigureAwait (false);
            Assert.AreEqual (1, TestSynchronizationContext.Instance.Posted, "#1");
        }

        [Test]
        public async Task AsyncMethod_ConfigureFalse_ConfigureTrue ()
        {
            await TestSynchronizationContext.Instance;
            TestSynchronizationContext.Instance.ResetCounts ();

            await Fifth ().ConfigureAwait (false);
            Assert.AreEqual (1, TestSynchronizationContext.Instance.Posted, "#1");
        }

        [Test]
        public async Task AsyncMethod_ConfigureFalse_ConfigureFalse ()
        {
            await TestSynchronizationContext.Instance;
            TestSynchronizationContext.Instance.ResetCounts ();

            await Sixth ().ConfigureAwait (false);
            Assert.AreEqual (0, TestSynchronizationContext.Instance.Posted, "#1");
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        async ReusableTask EmptyMethod ()
        {
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        async ReusableTask DelayMethodCapture ()
        {
            await Task.Delay (1).ConfigureAwait (true);
            Assert.AreEqual (TestSynchronizationContext.Instance, SynchronizationContext.Current, "#1");
            Assert.IsFalse (Thread.CurrentThread.IsThreadPoolThread, "#2");
        }

        async ReusableTask DelayMethodDoNotCapture ()
        {
            await Task.Delay (1).ConfigureAwait (false);
            Assert.AreEqual (null, SynchronizationContext.Current, "#1");
            Assert.IsTrue (Thread.CurrentThread.IsThreadPoolThread, "#2");
        }

        async ReusableTask First ()
        {
            await EmptyMethod ().ConfigureAwait (true);
            Assert.AreEqual (TestSynchronizationContext.Instance, SynchronizationContext.Current, "#1");
        }

        async ReusableTask Second ()
        {
            await EmptyMethod ().ConfigureAwait (false);
            Assert.AreEqual (TestSynchronizationContext.Instance, SynchronizationContext.Current, "#1");
        }

        async ReusableTask Third  ()
        {
            await DelayMethodCapture ().ConfigureAwait (true);
            Assert.AreEqual (TestSynchronizationContext.Instance, SynchronizationContext.Current, "#1");
        }

        async ReusableTask Fourth  ()
        {
            await DelayMethodCapture ().ConfigureAwait (false);
            Assert.AreEqual (null, SynchronizationContext.Current, "#1");
        }

        async ReusableTask Fifth ()
        {
            await DelayMethodDoNotCapture ().ConfigureAwait (true);
            Assert.AreEqual (TestSynchronizationContext.Instance, SynchronizationContext.Current, "#1");
        }

        async ReusableTask Sixth ()
        {
            await DelayMethodDoNotCapture ().ConfigureAwait (false);
            Assert.AreEqual (null, SynchronizationContext.Current, "#1");
        }
    }
}
