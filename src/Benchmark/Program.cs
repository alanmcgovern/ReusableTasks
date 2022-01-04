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
        int Concurrency = 10;
        int Depth = 100;
        public ReusableTask_AlreadyCompleted()
        {
        }

        [Benchmark]
        public void ReusableTask_1000Deep()
        {
            async Task Async_SyncCompletion_Looper()
            {
                async ReusableTask<int> AsyncCompletion(int count)
                {
                    if (count == 0)
                    {
                        await Task.Yield();
                        return 10;
                    }
                    return await AsyncCompletion(count - 1);
                }

                for (int i = 0; i < 1000; i++)
                    await AsyncCompletion(Depth);
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

                for (int i = 0; i < 1000; i++)
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
            var bench = new ReusableTask_AlreadyCompleted();
            for (int i = 0; i < 0; i++)
                bench.ReusableTask_1000Deep();
            Console.WriteLine("10");
            //return;
            var summary = BenchmarkRunner.Run(typeof(ReusableTask_AlreadyCompleted));
        }
    }
}
