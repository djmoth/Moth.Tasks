namespace Moth.Tasks
{
    using System;


    public struct WorkerGroupOptions
    {
        public WorkerThreadProvider WorkerThreadProvider { get; set; }

        public ProfilerProvider ProfilerProvider { get; set; }

        public EventHandler<TaskExceptionEventArgs> ExceptionEventHandler { get; set; }
    }
}
