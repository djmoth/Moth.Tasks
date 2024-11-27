using System;
using System.Threading;

namespace Moth.Tasks
{
    public interface ITaskQueue
    {
        void Enqueue<T> (in T task) where T : struct, ITask;
        void Enqueue<TTask> (in TTask task, out TaskHandle handle) where TTask : struct, ITask;
        void RunNextTask (IProfiler profiler = null, CancellationToken token = default);
        void RunNextTask (out Exception exception, IProfiler profiler = null, CancellationToken token = default);
        bool TryRunNextTask (IProfiler profiler = null);
        bool TryRunNextTask (out Exception exception, IProfiler profiler = null);
    }
}