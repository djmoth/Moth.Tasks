namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using Validation;

    public class WorkerGroup : IDisposable
    {
        private readonly Worker[] workers;
        private readonly bool disposeTasks;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource ();

        public WorkerGroup (int workerCount, TaskQueue taskQueue = null, bool isBackground = true, EventHandler<TaskExceptionEventArgs> exceptionEventHandler = null, IProfiler profiler = null, bool disposeTaskQueue = false)
        {
            Requires.Range (workerCount > 0, nameof (workerCount), $"{nameof (workerCount)} must be greater than zero.");

            Tasks = taskQueue ?? new TaskQueue ();
            disposeTasks = disposeTaskQueue;

            workers = new Worker[workerCount];

            for (int i = 0; i < workerCount; i++)
            {
                workers[i] = new Worker (taskQueue, cancellation.Token, isBackground, exceptionEventHandler, profiler, false);
            }
        }

        public TaskQueue Tasks { get; }

        public void Dispose ()
        {
            if (disposeTasks)
            {
                Tasks.Dispose ();
            }

            cancellation.Cancel ();
        }
    }
}
