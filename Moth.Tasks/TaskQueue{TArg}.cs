namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a queue of tasks to be run with an argument.
    /// </summary>
    /// <typeparam name="TArg">Type of argument to pass to the tasks.</typeparam>
    public class TaskQueue<TArg> : TaskQueue<TArg, Unit>, ITaskQueue<TArg>
    {
        /// <inheritdoc />
        public bool TryRunNextTask (TArg arg, IProfiler profiler = null)
            => TryRunNextTask (arg, out _, out _, profiler);

        /// <inheritdoc />
        public bool TryRunNextTask (TArg arg, out Exception exception, IProfiler profiler = null)
         => TryRunNextTask (arg, out _, out exception, profiler);
    }
}
