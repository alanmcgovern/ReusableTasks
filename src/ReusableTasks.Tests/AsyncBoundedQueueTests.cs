using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ReusableTasks.Tests
{
    [TestFixture]
    public class AsyncProducerConsumerQueueTests
    {
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
