using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ReusableTasks.Tests
{
    [TestFixture]
    public class AsyncProducerConsumerQueueTests
    {
        [Test]
        public void CompleteWhilePendingDequeue ()
        {
            var queue = new AsyncProducerConsumerQueue<int> (2);
            var dequeue = queue.DequeueAsync ();
            queue.CompleteAdding ();
            Assert.ThrowsAsync<InvalidOperationException> (() => dequeue.AsTask (), "#1");
        }

        [Test]
        public async Task CompleteWhilePendingEnqueue ()
        {
            var queue = new AsyncProducerConsumerQueue<int> (1);
            await queue.EnqueueAsync (5);
            var enqueueTask = queue.EnqueueAsync (6);

            queue.CompleteAdding ();
            Assert.AreEqual (5, await queue.DequeueAsync (), "#1");

            await enqueueTask;
            Assert.AreEqual (6, await queue.DequeueAsync (), "#2");
        }

        [Test]
        public void DequeueAfterComplete_Empty ()
        {
            var queue = new AsyncProducerConsumerQueue<int> (2);
            queue.CompleteAdding ();
            Assert.ThrowsAsync<InvalidOperationException> (() => queue.DequeueAsync ().AsTask (), "#1");
        }

        [Test]
        public async Task DequeueAfterComplete_NotEmpty ()
        {
            var queue = new AsyncProducerConsumerQueue<int> (2);
            await queue.EnqueueAsync (5);
            queue.CompleteAdding ();
            Assert.AreEqual (5, await queue.DequeueAsync (), "#1");
        }

        [Test]
        public async Task DequeueFirst ()
        {
            var queue = new AsyncProducerConsumerQueue<int> (2);

            var task = queue.DequeueAsync ();
            Assert.IsFalse (task.IsCompleted, "#1");

            await queue.EnqueueAsync (5);
            Assert.AreEqual (5, await task.WithTimeout ("#2"));
        }

        [Test]
        public void EnqueueAfterComplete ()
        {
            var queue = new AsyncProducerConsumerQueue<int> (2);
            queue.CompleteAdding ();
            Assert.ThrowsAsync<InvalidOperationException> (() => queue.EnqueueAsync (5).AsTask (), "#1");
        }

        [Test]
        public async Task EnqueueFirst ()
        {
            var queue = new AsyncProducerConsumerQueue<int> (2);

            await queue.EnqueueAsync (5).WithTimeout ("#1");
            Assert.AreEqual (5, await queue.DequeueAsync (), "#2");
        }

        [Test]
        public async Task EnqueueTooMany ()
        {
            var queue = new AsyncProducerConsumerQueue<int> (1);
            await queue.EnqueueAsync (5).WithTimeout ("#1");

            var task2 = queue.EnqueueAsync (5);
            Assert.IsFalse (task2.IsCompleted, "#2");

            await queue.DequeueAsync ().WithTimeout ("#3");
            await task2.WithTimeout ("#4");

            await queue.DequeueAsync ().WithTimeout ("#5");
        }

        [Test]
        public async Task EmptyTwice ()
        {
            var queue = new AsyncProducerConsumerQueue<int> (1);
            await queue.EnqueueAsync (1);
            await queue.DequeueAsync ();
            await queue.EnqueueAsync (1);
            await queue.DequeueAsync ();
        }
    }
}
