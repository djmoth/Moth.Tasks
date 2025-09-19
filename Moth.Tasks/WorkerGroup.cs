namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// A group of <see cref="IWorker"/>s, executing tasks from a shared <see cref="ITaskQueue{TArg, TResult}"/>.
    /// </summary>
    /// <typeparam name="TArg">Type of task argument.</typeparam>
    /// <typeparam name="TResult">Type of task result.</typeparam>
    /// <remarks>
    /// This class is thread-safe.
    /// </remarks>
    public class WorkerGroup<TArg, TResult> : IDisposable
    {
        private readonly bool disposeTasks;
        private readonly WorkerProvider<TArg, TResult> workerProvider;
        private IWorker<TArg, TResult>[] workers;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerGroup{TArg, TResult}"/> class.
        /// </summary>
        /// <param name="workerCount">Number of workers. Must be greater than zero.</param>
        /// <param name="taskQueue">The <see cref="TaskQueue{TArg, TResult}"/> of which the workers will be executing tasks from.</param>
        /// <param name="disposeTaskQueue">Determines whether the <see cref="TaskQueue{TArg, TResult}"/> supplied with <paramref name="taskQueue"/> is disposed when <see cref="Dispose ()"/> is called.</param>
        /// <param name="workerProvider">A method that provides an <see cref="IWorker"/> for a <see cref="WorkerGroup{TArg, TResult}"/>. May be <see langword="null"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="workerCount"/> must be greater than zero.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="taskQueue"/> cannot be null.</exception>
        public WorkerGroup (int workerCount, ITaskQueue<TArg, TResult> taskQueue, bool disposeTaskQueue, WorkerProvider<TArg, TResult> workerProvider = null)
        {
            if (workerCount <= 0)
                throw new ArgumentOutOfRangeException (nameof (workerCount), workerCount, $"{nameof (workerCount)} must be greater than zero.");

            Tasks = taskQueue ?? throw new ArgumentNullException (nameof (taskQueue), $"{nameof (taskQueue)} cannot be null.");
            disposeTasks = disposeTaskQueue;

            if (disposeTasks)
                GC.SuppressFinalize (taskQueue);

            this.workerProvider = workerProvider;

            workers = new IWorker<TArg, TResult>[workerCount];

            for (int i = 0; i < workerCount; i++)
            {
                workers[i] = GetAndStartNewWorker (i);
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="WorkerGroup{TArg, TResult}"/> class.
        /// </summary>
        ~WorkerGroup () => Dispose (false);

        /// <summary>
        /// The <see cref="ITaskQueue{TArg, TResult}"/> of which the workers are executing tasks from.
        /// </summary>
        public ITaskQueue<TArg, TResult> Tasks { get; }

        /// <summary>
        /// Get or set the number of <see cref="IWorker"/>s in this <see cref="WorkerGroup{TArg, TResult}"/>. Must be greater than zero.
        /// </summary>
        /// <remarks>
        /// The <see cref="ProfilerProvider"/> and <see cref="EventHandler{TaskExceptionEventArgs}"/> provided in the <see cref="WorkerGroup{TArg, TResult}"/> constructor will be used to initialize any new <see cref="IWorker"/>s.
        /// </remarks>
        public int WorkerCount
        {
            get => workers.Length;

            set
            {
                if (disposed)
                    throw new ObjectDisposedException (nameof (WorkerGroup<TArg, TResult>));

                if (value == workers.Length)
                    return;

                if (value <= 0)
                    throw new ArgumentOutOfRangeException (nameof (value), $"{nameof (WorkerCount)} must be greater than zero.");

                int oldWorkerCount = workers.Length;

                // Dispose of the excess workers, if new value is less than the old worker count.
                for (int i = value; i < oldWorkerCount; i++)
                {
                    if (workers[i] is IDisposable disposableWorker)
                        disposableWorker.Dispose ();
                }

                Array.Resize (ref workers, value);

                // Initialize new workers, if new value is greater than the old worker count
                for (int i = oldWorkerCount; i < value; i++)
                {
                    workers[i] = GetAndStartNewWorker (i);
                }
            }
        }

        /// <summary>
        /// Calls <see cref="Dispose()"/> and blocks the calling thread until all <see cref="IWorker"/>s terminate.
        /// </summary>
        public void DisposeAndJoin ()
        {
            Dispose ();

            Join ();
        }

        /// <summary>
        /// Blocks the calling thread until all <see cref="IWorker"/>s terminate.
        /// </summary>
        /// <remarks>
        /// <see cref="Dispose()"/> must be called beforehand.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <see cref="WorkerGroup{TArg, TResult}"/> is not disposed.</exception>
        public void Join ()
        {
            if (!disposed)
                throw new InvalidOperationException ("Join may only be called after the WorkerGroup is disposed.");

            foreach (IWorker worker in workers)
            {
                worker.Join ();
            }
        }

        /// <summary>
        /// Signals all workers to shutdown. Also disposes of <see cref="Tasks"/> if specified in <see cref="WorkerGroup{TArg, TResult}"/> constructor.
        /// </summary>
        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        /// <summary>
        /// Signals all workers to shutdown. Also disposes of <see cref="Tasks"/> if specified in <see cref="WorkerGroup{TArg, TResult}"/> constructor.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if called from <see cref="Dispose ()"/>, <see langword="false"/> if called from finalizer.</param>
        protected virtual void Dispose (bool disposing)
        {
            if (disposed)
                return;

            foreach (IWorker<TArg, TResult> worker in workers)
            {
                if (worker is IDisposable disposableWorker)
                    disposableWorker.Dispose ();
            }

            if (disposeTasks && Tasks is IDisposable disposableTasks)
            {
                disposableTasks.Dispose ();
            }

            disposed = true;
        }

        private IWorker<TArg, TResult> GetAndStartNewWorker (int index)
        {
            IWorker<TArg, TResult> worker = workerProvider != null ? workerProvider (this, index) : new Worker<TArg, TResult> (Tasks, false, default);

            if (worker is IDisposable)
                GC.SuppressFinalize (worker);

            if (!worker.IsStarted)
                worker.Start ();

            return worker;
        }
    }
}
