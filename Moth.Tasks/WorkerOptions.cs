namespace Moth.Tasks
{
    using System;

    /// <summary>
    /// Represents options for a worker.
    /// </summary>
    public struct WorkerOptions
    {
        /// <summary>
        /// Gets or sets the <see cref="IProfiler"/> to use.
        /// </summary>
        public IProfiler Profiler { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ProfilerProvider"/> to use.
        /// </summary>
        public ProfilerProvider ProfilerProvider { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IWorkerThread"/> to use.
        /// </summary>
        public IWorkerThread WorkerThread { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="WorkerThreadProvider"/> to use.
        /// </summary>
        public WorkerThreadProvider WorkerThreadProvider { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="EventHandler{TaskExceptionEventArgs}"/> to use.
        /// </summary>
        public EventHandler<TaskExceptionEventArgs> ExceptionEventHandler { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the worker will require <see cref="IWorker.Start"/> to be called manually.
        /// </summary>
        public bool RequiresManualStart { get; set; }
    }
}
