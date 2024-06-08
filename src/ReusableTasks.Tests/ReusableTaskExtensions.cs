using System.Diagnostics;
using System.Threading.Tasks;

using NUnit.Framework;

namespace ReusableTasks.Tests
{
    public static class ReusableTaskExtensions
    {
        static readonly int Timeout = Debugger.IsAttached ? -1 : 1000;

        public static async ReusableTask WithTimeout (this ReusableTask task, string message)
        {
            var t = task.AsTask ();
            if (await Task.WhenAny (Task.Delay (Timeout), t) != t)
                Assert.Fail ("The task timed out. {0}", message);
            await t;
        }

        public static async ReusableTask<T> WithTimeout<T> (this ReusableTask<T> task, string message)
        {
            var t = task.AsTask ();
            if (await Task.WhenAny (Task.Delay (Timeout), t) != t)
                Assert.Fail ("The task timed out. {0}", message);
            return await t;
        }
    }
}
