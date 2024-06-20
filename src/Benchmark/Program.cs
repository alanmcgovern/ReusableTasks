using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

using ReusableTasks;

namespace MyBenchmarks
{
    [MemoryDiagnoser]
    public class ReusableTask_AlreadyCompleted
    {
        [Params (1)]
        public int Concurrency { get; set; } = 1;

        [Params (500)]
        public int Depth { get; set; } = 500;

        [Params (1000)]
        public int Iterations { get; set; } = 1000;

        public ReusableTask_AlreadyCompleted ()
        {
            ReusableTaskMethodBuilder.MaximumCacheSize = Depth * 4;
        }

        [Benchmark]
        public async Task ReusableTask ()
        {
            static async ReusableTask AsyncCompletion (int count)
            {
                if (count == 0) {
                    await Task.Yield ();
                } else {
                    await AsyncCompletion (count - 1).ConfigureAwait (false);
                }
            }

            for (int i = 0; i < Iterations; i++)
                await AsyncCompletion (Depth).ConfigureAwait (false);
        }

        [Benchmark]
        public void ReusableTaskInt ()
        {
            async Task Async_SyncCompletion_Looper ()
            {
                async ReusableTask<int> AsyncCompletion (int count)
                {
                    if (count == 0) {
                        await Task.Yield ();
                        return 10;
                    } else {
                        return await AsyncCompletion (count - 1);
                    }
                }

                for (int i = 0; i < Iterations; i++)
                    await AsyncCompletion (Depth);
            }

            Task.WhenAll (Enumerable.Range (0, Concurrency)
                .Select (t => Task.Run (Async_SyncCompletion_Looper)))
                .Wait ();
        }

        [Benchmark]
        public void ValueTaskInt ()
        {
            async Task Async_SyncCompletion_Looper ()
            {
                async ValueTask<int> AsyncCompletion (int count)
                {
                    if (count == 0) {
                        await Task.Yield ();
                        return 10;
                    } else {
                        return await AsyncCompletion (count - 1);
                    }
                }

                for (int i = 0; i < Iterations; i++)
                    await AsyncCompletion (Depth);
            }

            Task.WhenAll (Enumerable.Range (0, Concurrency)
                .Select (t => Task.Run (Async_SyncCompletion_Looper)))
                .Wait ();
        }
    }

    public class Program
    {
        public static void Main (string[] args)
        {
            //RunTest ().Wait ();
            //var summary = BenchmarkRunner.Run (typeof (ReusableTask_AlreadyCompleted).Assembly, new DebugInProcessConfig());
            var summary = BenchmarkRunner.Run (typeof (ReusableTask_AlreadyCompleted).Assembly);
        }

        static async Task RunTest ()
        {
            ReusableTask_AlreadyCompleted a = new ReusableTask_AlreadyCompleted ();
            for (int i = 0; i < 10000; i++) {
                a.ReusableTask ();
                await Task.Yield ();
            }
        }
    }
}
