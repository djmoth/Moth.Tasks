namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    /// <summary>
    /// Represents a queue of tasks to be run.
    /// </summary>
    public class TaskQueue : TaskQueue<Unit>, ITaskQueue
    {
        /// <inheritdoc />
        public void RunNextTask (IProfiler profiler = null, CancellationToken token = default)
            => RunNextTask (default, profiler, token);

        /// <inheritdoc />
        public void RunNextTask (out Exception exception, IProfiler profiler = null, CancellationToken token = default)
            => RunNextTask (default, out exception, profiler, token);

        /// <inheritdoc />
        public bool TryRunNextTask (IProfiler profiler = null)
            => TryRunNextTask (default, profiler);

        /// <inheritdoc />
        public bool TryRunNextTask (out Exception exception, IProfiler profiler = null)
            => TryRunNextTask (default, out exception, profiler);
    }
}
