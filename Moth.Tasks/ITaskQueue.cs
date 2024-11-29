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

    public interface ITaskQueue<TArg>
    {
        void Enqueue<TTask> (in TTask task) where TTask : struct, ITask<TArg>;

        void Enqueue<TTask> (in TTask task, out TaskHandle handle) where TTask : struct, ITask<TArg>;

        void RunNextTask (TArg arg, IProfiler profiler = null, CancellationToken token = default);

        void RunNextTask (TArg arg, out Exception exception, IProfiler profiler = null, CancellationToken token = default);

        bool TryRunNextTask (TArg arg, IProfiler profiler = null);

        bool TryRunNextTask (TArg arg, out Exception exception, IProfiler profiler = null);
    }

    public interface ITaskQueue<TArg, TResult>
    {
        void Enqueue<T> (in T task) where T : struct, ITask<TArg, TResult>;

        void Enqueue<TTask> (in TTask task, out TaskHandle handle) where TTask : struct, ITask<TArg, TResult>;

        TResult RunNextTask (TArg arg, IProfiler profiler = null, CancellationToken token = default);

        TResult RunNextTask (TArg arg, out Exception exception, IProfiler profiler = null, CancellationToken token = default);

        bool TryRunNextTask (TArg arg, out TResult result, IProfiler profiler = null);

        bool TryRunNextTask (TArg arg, out TResult result, out Exception exception, IProfiler profiler = null);
    }
}