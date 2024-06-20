using System;
using System.Collections.Generic;
using System.Text;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Not intended to be used directly.
    /// </summary>
    public static class ReusableTaskMethodBuilderCore
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAwaiter"></typeparam>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="awaiter"></param>
        /// <param name="stateMachine"></param>
        public static void AwaitOnCompleted<TAwaiter, TStateMachine> (ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (awaiter is IReusableTaskAwaiter) {
                ref ReusableTaskAwaiter typedAwaiter = ref Unsafe.As<TAwaiter, ReusableTaskAwaiter> (ref awaiter);
                var sm = StateMachineCache<TStateMachine>.GetOrCreate ();
                sm.SetStateMachine (ref stateMachine);
                typedAwaiter.ResultHolder.Continuation = sm;
            } else {
                var smwc = StateMachineWithActionCache<TStateMachine>.GetOrCreate ();
                smwc.SetStateMachine (ref stateMachine);
                awaiter.OnCompleted (smwc.Callback);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAwaiter"></typeparam>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="awaiter"></param>
        /// <param name="stateMachine"></param>
        public static void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine> (ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (awaiter is IReusableTaskAwaiter) {
                ref ReusableTaskAwaiter typedAwaiter = ref Unsafe.As<TAwaiter, ReusableTaskAwaiter> (ref awaiter);
                var sm = StateMachineCache<TStateMachine>.GetOrCreate ();
                sm.SetStateMachine (ref stateMachine);
                typedAwaiter.ResultHolder.Continuation = sm;
            } else {
                var smwc = StateMachineWithActionCache<TStateMachine>.GetOrCreate ();
                smwc.SetStateMachine (ref stateMachine);
                awaiter.OnCompleted (smwc.Callback);
            }
        }
    }
}
