namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using Validation;

    /// <summary>
    /// A group of <see cref="Worker"/>s, executing tasks from a shared <see cref="TaskQueue"/>.
    /// </summary>
    public class WorkerGroup : IDisposable
    {
        private readonly Worker[] workers;
        private readonly bool disposeTasks;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerGroup"/> class.
        /// </summary>
        /// <param name="workerCount">Number of workers. Must be greater than zero.</param>
        /// <param name="taskQueue">The <see cref="TaskQueue"/> of which the workers will be executing tasks from.</param>
        /// <param name="disposeTaskQueue">Determines whether the <see cref="TaskQueue"/> supplied with <paramref name="taskQueue"/> is disposed when <see cref="Dispose ()"/> is called.</param>
        /// <param name="isBackground">Defines the <see cref="Thread.IsBackground"/> property of the internal thread of each worker.</param>
        /// <param name="exceptionEventHandler">Method invoked if a task throws an exception. May be <see langword="null"/>.</param>
        /// <param name="profiler"><see cref="IProfiler"/> used to profile tasks. May be <see langword="null"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="workerCount"/> must be greater than zero.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="taskQueue"/> cannot be null.</exception>
        public WorkerGroup (int workerCount, TaskQueue taskQueue, bool disposeTaskQueue = false, bool isBackground = true, EventHandler<TaskExceptionEventArgs> exceptionEventHandler = null, IProfiler profiler = null)
        {
            Requires.Range (workerCount > 0, nameof (workerCount), $"{nameof (workerCount)} must be greater than zero.");

            Tasks = taskQueue ?? throw new ArgumentNullException (nameof (taskQueue), $"{nameof (taskQueue)} cannot be null.");
            disposeTasks = disposeTaskQueue;

            if (disposeTasks)
                GC.SuppressFinalize (taskQueue);

            workers = new Worker[workerCount];

            for (int i = 0; i < workerCount; i++)
            {
                workers[i] = new Worker (taskQueue, false, isBackground, exceptionEventHandler, profiler);
                GC.SuppressFinalize (workers[i]);
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="WorkerGroup"/> class.
        /// </summary>
        ~WorkerGroup () => Dispose (false);

        /// <summary>
        /// The <see cref="TaskQueue"/> of which the workers are executing tasks from.
        /// </summary>
        public TaskQueue Tasks { get; }

        /// <summary>
        /// Signals all workers to shutdown. Also disposes of <see cref="Tasks"/> if specified in <see cref="WorkerGroup"/> constructor.
        /// </summary>
        public void Dispose ()
        {
            lock (workers)
            {
                Dispose (true);
                GC.SuppressFinalize (this);
            }
        }

        /// <summary>
        /// Signals all workers to shutdown. Also disposes of <see cref="Tasks"/> if specified in <see cref="WorkerGroup"/> constructor.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if called from <see cref="Dispose ()"/>, <see langword="false"/> if called from finalizer.</param>
        protected virtual void Dispose (bool disposing)
        {
            if (disposed)
                return;

            foreach (Worker worker in workers)
            {
                worker.Dispose ();
            }

            if (disposeTasks)
            {
                Tasks.Dispose ();
            }

            disposed = true;
        }
    }
}
