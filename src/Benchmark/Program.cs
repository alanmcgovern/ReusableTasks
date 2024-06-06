using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using ReusableTasks;

namespace MyBenchmarks
{
    [MemoryDiagnoser]
    public class ReusableTask_AlreadyCompleted
    {
        int Concurrency = 1;
        int Depth = 100;
        int Iterations = 100;
        public ReusableTask_AlreadyCompleted()
        {
        }

        [Benchmark]
        public void ReusableTask_1000Deep()
        {
            async Task Async_SyncCompletion_Looper()
            {
                async ReusableTask<int> AsyncCompletion(int count, TaskCompletionSource<int> tcs)
                {
                    if (count == 0)
                    {
                        await Task.Yield();
                        return await tcs.Task;
                    }
                    return await AsyncCompletion(count - 1, tcs);
                }

                for (int i = 0; i < Iterations; i++)
                {
                    var tcs = new TaskCompletionSource<int>();
                    var task = AsyncCompletion(Depth, tcs);
                    tcs.SetResult(10);
                    await task;
                }
            }

            Task.WhenAll(Enumerable.Range(0, Concurrency)
                .Select(t => Task.Run(Async_SyncCompletion_Looper)))
                .Wait();
        }

        [Benchmark]
        public void ValueTask_1000Deep()
        {
            async Task Async_SyncCompletion_Looper()
            {
                async ValueTask<int> AsyncCompletion(int count)
                {
                    if (count == 0)
                    {
                        await Task.Yield();
                        return 10;
                    }
                    return await AsyncCompletion(count - 1);
                }

                for (int i = 0; i < Iterations; i++)
                    await AsyncCompletion(Depth);
            }

            Task.WhenAll(Enumerable.Range(0, Concurrency)
                .Select(t => Task.Run(Async_SyncCompletion_Looper)))
                .Wait();
        }

    }

    public class Program
    {
        public static void Main(string[] args)
        {
            //RunTest();
            var summary = BenchmarkRunner.Run(typeof(ReusableTask_AlreadyCompleted));
        }

        static void RunTest()
        {
            var bench = new ReusableTask_AlreadyCompleted();
            for (int i = 0; i < 0; i++)
                bench.ReusableTask_1000Deep();
            Console.WriteLine("10");
            //return;
            for (int i = 0; i < 100; i++)
                bench.ReusableTask_1000Deep();
            Console.WriteLine("100");
            Console.ReadLine();
        }
    }
}
