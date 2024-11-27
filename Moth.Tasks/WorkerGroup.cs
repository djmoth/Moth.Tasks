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
    /// <remarks>
    /// This class is thread-safe.
    /// </remarks>
    public class WorkerGroup : IDisposable
    {
        private readonly bool disposeTasks;
        private readonly bool isBackground;
        private readonly EventHandler<TaskExceptionEventArgs> exceptionEventHandler;
        private readonly ProfilerProvider profilerProvider;
        private readonly WorkerThreadProvider workerThreadProvider;
        private Worker[] workers;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerGroup"/> class.
        /// </summary>
        /// <param name="workerCount">Number of workers. Must be greater than zero.</param>
        /// <param name="taskQueue">The <see cref="TaskQueue"/> of which the workers will be executing tasks from.</param>
        /// <param name="disposeTaskQueue">Determines whether the <see cref="TaskQueue"/> supplied with <paramref name="taskQueue"/> is disposed when <see cref="Dispose ()"/> is called.</param>
        /// <param name="options">Options for initializing the <see cref="WorkerGroup"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="workerCount"/> must be greater than zero.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="taskQueue"/> cannot be null.</exception>
        public WorkerGroup (int workerCount, ITaskQueue taskQueue, bool disposeTaskQueue, WorkerGroupOptions options)
        {
            Requires.Range (workerCount > 0, nameof (workerCount), $"{nameof (workerCount)} must be greater than zero.");

            Tasks = taskQueue ?? throw new ArgumentNullException (nameof (taskQueue), $"{nameof (taskQueue)} cannot be null.");
            disposeTasks = disposeTaskQueue;

            if (disposeTasks)
                GC.SuppressFinalize (taskQueue);

            exceptionEventHandler = options.ExceptionEventHandler;
            profilerProvider = options.ProfilerProvider;
            workerThreadProvider = options.WorkerThreadProvider;

            workers = new Worker[workerCount];

            WorkerOptions workerOptions = new WorkerOptions
            {
                ExceptionEventHandler = exceptionEventHandler,
                ProfilerProvider = profilerProvider,
                WorkerThreadProvider = workerThreadProvider,
            };

            for (int i = 0; i < workerCount; i++)
            {
                workers[i] = new Worker (taskQueue, false, workerOptions);
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
        public ITaskQueue Tasks { get; }

        /// <summary>
        /// Get or set the number of <see cref="Worker"/>s in this <see cref="WorkerGroup"/>. Must be greater than zero.
        /// </summary>
        /// <remarks>
        /// The <see cref="ProfilerProvider"/> and <see cref="EventHandler{TaskExceptionEventArgs}"/> provided in the <see cref="WorkerGroup"/> constructor will be used to initialize any new <see cref="Worker"/>s.
        /// </remarks>
        public int WorkerCount
        {
            get => workers.Length;

            set
            {
                if (disposed)
                    throw new ObjectDisposedException (nameof (WorkerGroup));

                if (value == workers.Length)
                    return;

                Requires.Range (value > 0, nameof (value), $"{nameof (value)} must be greater than zero.");

                int oldWorkerCount = workers.Length;

                // Dispose of the excess workers, if new value is less than the old worker count.
                for (int i = value; i < oldWorkerCount; i++)
                {
                    workers[i].Dispose ();
                }

                Array.Resize (ref workers, value);

                WorkerOptions workerOptions = new WorkerOptions
                {
                    ExceptionEventHandler = exceptionEventHandler,
                    ProfilerProvider = profilerProvider,
                };

                // Initialize new workers, if new value is greater than the old worker count
                for (int i = oldWorkerCount; i < value; i++)
                {
                    workers[i] = new Worker (Tasks, false, workerOptions);
                }
            }
        }

        /// <summary>
        /// Calls <see cref="Dispose()"/> and blocks the calling thread until all <see cref="Worker"/>s terminate.
        /// </summary>
        public void DisposeAndJoin ()
        {
            Dispose ();

            Join ();
        }

        /// <summary>
        /// Blocks the calling thread until all <see cref="Worker"/>s terminate.
        /// </summary>
        /// <remarks>
        /// <see cref="Dispose()"/> must be called beforehand.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <see cref="WorkerGroup"/> is not disposed.</exception>
        public void Join ()
        {
            if (!disposed)
                throw new InvalidOperationException ("Join may only be called after the WorkerGroup is disposed.");

            foreach (Worker worker in workers)
            {
                worker.Join ();
            }
        }

        /// <summary>
        /// Signals all workers to shutdown. Also disposes of <see cref="Tasks"/> if specified in <see cref="WorkerGroup"/> constructor.
        /// </summary>
        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
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

            if (disposeTasks && Tasks is IDisposable disposableTasks)
            {
                disposableTasks.Dispose ();
            }

            disposed = true;
        }
    }
}
