namespace Moth.Tasks
{
    using System;

    public struct WorkerOptions
    {
        public IProfiler Profiler { get; set; }

        public ProfilerProvider ProfilerProvider { get; set; }

        public IWorkerThread WorkerThread { get; set; }

        public EventHandler<TaskExceptionEventArgs> ExceptionEventHandler { get; set; }
    }
}
