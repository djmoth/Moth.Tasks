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
    public class TaskQueue<TArg, TResult> : ITaskQueue<TArg, TResult>
    {
        private readonly object taskLock = new object ();
        private readonly Queue<int> tasks;
        private readonly ManualResetEventSlim tasksEnqueuedEvent = new ManualResetEventSlim (); // Must be explicitly set by callers of EnqueueImpl
        private readonly ITaskMetadataCache taskCache;
        private readonly ITaskDataStore taskDataStore;
        private readonly ITaskHandleManager taskHandleManager;
        private readonly ITaskDataAccess dataAccess;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskQueue{TArg, TResult}"/> class.
        /// </summary>
        public TaskQueue ()
            : this (16, 1024, 32) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskQueue{TArg, TResult}"/> class.
        /// </summary>
        /// <param name="taskCapacity">Starting capacity for the internal task queue in no. of tasks.</param>
        /// <param name="unmanagedDataCapacity">Starting capacity for the internal task data array in bytes.</param>
        /// <param name="managedReferenceCapacity">Starting capacity for the internal task reference field store in no. of references.</param>
        /// <param name="taskCache">Optional <see cref="ITaskMetadataCache"/> for caching task types.</param>
        public TaskQueue (int taskCapacity, int unmanagedDataCapacity, int managedReferenceCapacity, ITaskMetadataCache taskCache = null)
            : this (taskCapacity, taskCache, new TaskDataStore (unmanagedDataCapacity, new TaskReferenceStore (managedReferenceCapacity)), new TaskHandleManager ()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskQueue{TArg, TResult}"/> class.
        /// </summary>
        /// <inheritdoc cref="TaskQueue{TArg,TResult}(int, int, int, ITaskMetadataCache)"/>/>
        /// <param name="taskCapacity"></param>
        /// <param name="taskCache"></param>
        /// <param name="taskDataStore"><see cref="ITaskDataStore"/> responsible for storing task data.</param>
        /// <param name="taskHandleManager"><see cref="ITaskHandleManager"/> responsible for managing task handles.</param>
        internal TaskQueue (int taskCapacity, ITaskMetadataCache taskCache, ITaskDataStore taskDataStore, ITaskHandleManager taskHandleManager)
        {
            tasks = new Queue<int> (taskCapacity);

            this.taskCache = taskCache ?? new TaskMetadataCache ();
            this.taskDataStore = taskDataStore;
            this.taskHandleManager = taskHandleManager;

            dataAccess = new DataAccess (this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TaskQueue{TArg, TResult}"/> class. Also disposes of any enqueued tasks implementing <see cref="IDisposable.Dispose"/>.
        /// </summary>
        ~TaskQueue () => Dispose (false);

        /// <summary>
        /// The number of tasks currently enqueued.
        /// </summary>
        public int Count
        {
            get
            {
                lock (taskLock)
                {
                    return tasks.Count;
                }
            }
        }

        /// <inheritdoc />
        /// <exception cref="ObjectDisposedException">The <see cref="TaskQueue{TArg, TResult}"/> has been disposed.</exception>
        public void Enqueue<T> (in T task)
            where T : struct, ITask<TArg, TResult>
        {
            lock (taskLock)
            {
                if (disposed)
                {
                    throw new ObjectDisposedException (nameof (TaskQueue<TArg, TResult>), "New tasks may not be enqueued after TaskQueue has been disposed.");
                }

                ITaskMetadata<T> taskInfo = taskCache.GetTask<T> ();

                tasks.Enqueue (taskInfo.ID); // Add task ID to the queue

                // Only write task data if present
                if (taskInfo.UnmanagedSize > 0 || taskInfo.IsManaged)
                {
                    taskDataStore.Enqueue (task, taskInfo);
                }
            }

            tasksEnqueuedEvent.Set (); // Signal potentially waiting threads that tasks are ready to be executed
        }

        /// <inheritdoc />
        /// <exception cref="ObjectDisposedException">The <see cref="TaskQueue{TArg, TResult}"/> has been disposed.</exception>
        public void Enqueue<TTask> (in TTask task, out TaskHandle handle)
            where TTask : struct, ITask<TArg, TResult>
        {
            handle = CreateTaskHandle ();

            Enqueue (new TaskWithHandle<TTask, TArg, TResult> (task, handle));
        }

        /// <inheritdoc />
        public TResult RunNextTask (TArg arg, IProfiler profiler = null, CancellationToken token = default) => RunNextTask (arg, out _, profiler, token);

        /// <inheritdoc />
        public TResult RunNextTask (TArg arg, out Exception exception, IProfiler profiler = null, CancellationToken token = default)
        {
            tasksEnqueuedEvent.Wait (token);

            if (token.IsCancellationRequested)
            {
                exception = null;
                return default;
            }

            TryRunNextTask (arg, out TResult result, out exception, profiler);
            return result;
        }

        /// <inheritdoc />
        public bool TryRunNextTask (TArg arg, out TResult result, IProfiler profiler = null) => TryRunNextTask (arg, out result, out _, profiler);

        /// <inheritdoc />
        public unsafe bool TryRunNextTask (TArg arg, out TResult result, out Exception exception, IProfiler profiler = null)
        {
            result = default;
            exception = null;

            bool accessDisposed = false;
            TaskDataAccess access = new TaskDataAccess (dataAccess, &accessDisposed, true);

            bool isProfiling = false;

            try
            {
                if (tasks.Count == 0)
                {
                    access.Dispose ();

                    return false;
                }

                int id = tasks.Dequeue ();

                if (tasks.Count == 0)
                {
                    tasksEnqueuedEvent.Reset (); // All tasks have been fetched, and as such the event can be reset.
                }

                ITaskMetadata task = taskCache.GetTask (id);

                if (profiler != null)
                {
                    profiler.BeginTask (task.Type);
                    isProfiling = true; // If profiler was started without throwing an exception
                }

                try
                {
                    // Run the task
                    ((ITaskMetadata<TArg, TResult>)task).Run (access, arg, out result);
                } catch (Exception ex)
                {
                    exception = ex;
                }
            } finally
            {
                // Internal error, TaskMetadata should always call TaskDataAccess.Dispose after getting task data in TaskMetadata.RunAndDispose
                if (!access.Disposed)
                {
                    access.Dispose ();
                }

                if (isProfiling)
                {
                    profiler.EndTask ();
                }
            }

            return true;
        }

        /// <summary>
        /// Removes all pending tasks from the queue. Also calls <see cref="IDisposable.Dispose"/> on tasks which implement the method.
        /// </summary>
        /// <param name="exceptionHandler">Method for handling an exception thrown by a task's <see cref="IDisposable.Dispose"/>.</param>
        /// <remarks>
        /// As the method iterates through all tasks in the queue and calls <see cref="IDisposable.Dispose"/> on tasks, it can hang for an unknown amount of time. If an exception is thrown in an <see cref="IDisposable.Dispose"/> call and <paramref name="exceptionHandler"/> is not provided, the method will throw the exception. If <paramref name="exceptionHandler"/> is provided, the method will call the handler with the exception as argument and continue on with disposing the remaining tasks.
        /// </remarks>
        public unsafe void Clear (Action<Exception> exceptionHandler = null)
        {
            bool accessDisposed = false;
            using TaskDataAccess access = new TaskDataAccess (dataAccess, &accessDisposed, false);

            foreach (int id in tasks)
            {
                ITaskMetadata taskMetadata = taskCache.GetTask (id);

                if (taskMetadata.IsDisposable)
                {
                    try
                    {
                        taskMetadata.Dispose (access);
                    } catch (Exception ex)
                    {
                        if (exceptionHandler != null)
                            exceptionHandler (ex);
                        else
                            throw;
                    }
                } else
                {
                    taskDataStore.Skip (taskMetadata);
                }
            }

            tasks.Clear ();
            taskHandleManager.Clear ();
            taskDataStore.Clear ();
        }

        /// <summary>
        /// Disposes all tasks which implement <see cref="IDisposable"/>.
        /// </summary>
        /// <remarks>
        /// As the method iterates through all tasks in the queue and calls <see cref="IDisposable.Dispose"/> on tasks, it can hang for an unknown amount of time. If an exception is thrown in an <see cref="IDisposable.Dispose"/> call, the method logs the exception with <see cref="Trace.TraceError(string)"/> and continues on with disposing the remaining tasks. If custom exception handling is required, use <see cref="Clear(Action{Exception})"/> first before disposing.
        /// </remarks>
        public void Dispose ()
        {
            lock (taskLock)
            {
                Dispose (true);
                GC.SuppressFinalize (this);
            }
        }

        /// <inheritdoc cref="Dispose()"/>
        /// <param name="disposing"><see langword="true"/> if called from <see cref="Dispose ()"/>, <see langword="false"/> if called from finalizer.</param>
        protected virtual void Dispose (bool disposing)
        {
            if (disposed)
                return;

            Clear (TraceException);
            disposed = true;

            static void TraceException (Exception ex) => Trace.TraceError (ex.ToString ());
        }

        /// <summary>
        /// Creates a new <see cref="TaskHandle"/>.
        /// </summary>
        /// <returns>A new unique <see cref="TaskHandle"/>.</returns>
        protected TaskHandle CreateTaskHandle () => taskHandleManager.CreateTaskHandle ();

        /// <summary>
        /// Provides access to task data for a tasks.
        /// </summary>
        public class DataAccess : ITaskDataAccess
        {
            private TaskQueue<TArg, TResult> queue;

            /// <summary>
            /// Initializes a new instance of the <see cref="DataAccess"/> class.
            /// </summary>
            /// <param name="queue"><see cref="TaskQueue{TArg, TResult}"/> to get task data from.</param>
            internal DataAccess (TaskQueue<TArg, TResult> queue)
            {
                this.queue = queue;
            }

            /// <inheritdoc />
            public void EnterLock ()
            {
                Monitor.Enter (queue.taskLock);
            }

            /// <inheritdoc />
            public void ExitLock ()
            {
                Monitor.Exit (queue.taskLock);
            }

            /// <inheritdoc />
            public TTask GetNextTaskData<TTask> (ITaskMetadata<TTask> taskMetadata) where TTask : struct, ITask
            {
                return queue.taskDataStore.Dequeue (taskMetadata);
            }
        }
    }
}
