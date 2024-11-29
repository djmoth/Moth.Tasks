namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    public readonly struct TaskRunInfo
    {
        public TaskQueue TaskQueue { get; }

        public IProfiler Profiler { get; }
    }

    public unsafe class TaskQueue<TArg, TResult> : ITaskQueue<TArg, TResult>
    {
        
    }
}
