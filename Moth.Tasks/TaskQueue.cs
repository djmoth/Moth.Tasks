using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Moth.Tasks
{
    public class TaskQueue : ITaskQueue
    {
        private readonly object taskLock = new object ();
        private readonly Queue<int> tasks;
        private readonly ManualResetEventSlim tasksEnqueuedEvent = new ManualResetEventSlim (); // Must be explicitly set by callers of EnqueueImpl
        private readonly ITaskCache taskCache;
        private readonly ITaskDataStore taskDataStore;
        private readonly ITaskHandleManager taskHandleManager;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskQueue"/> class.
        /// </summary>
        public TaskQueue ()
            : this (16, 1024, 32) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskQueue"/> class.
        /// </summary>
        /// <param name="taskCapacity">Starting capacity for the internal task queue in no. of tasks.</param>
        /// <param name="unmanagedDataCapacity">Starting capacity for the internal task data array in bytes.</param>
        /// <param name="managedReferenceCapacity">Starting capacity for the internal task reference field store in no. of references.</param>
        /// <param name="taskCache">Optional <see cref="ITaskCache"/> for caching task types.</param>
        public TaskQueue (int taskCapacity, int unmanagedDataCapacity, int managedReferenceCapacity, ITaskCache taskCache = null)
            : this (taskCapacity, taskCache, new TaskDataStore (unmanagedDataCapacity, new TaskReferenceStore (managedReferenceCapacity)), new TaskHandleManager ()) { }

        internal TaskQueue (int taskCapacity, ITaskCache taskCache, ITaskDataStore taskDataStore, ITaskHandleManager taskHandleManager)
        {
            tasks = new Queue<int> (taskCapacity);

            this.taskCache = taskCache ?? new TaskCache ();
            this.taskDataStore = taskDataStore;
            this.taskHandleManager = taskHandleManager;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TaskQueue"/> class. Also disposes of any enqueued tasks implementing <see cref="IDisposable.Dispose"/>.
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

        /// <summary>
        /// Enqueue an <see cref="ITask"/> to be run later.
        /// </summary>
        /// <typeparam name="T">Type of task to run.</typeparam>
        /// <param name="task">Task data.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="TaskQueue"/> has been disposed.</exception>
        public void Enqueue<T> (in T task) where T : struct, ITask => EnqueueTask (task);


        /// <summary>
        /// Enqueue an <see cref="ITask"/> to be run later, giving out a <see cref="TaskHandle"/> for checking task status.
        /// </summary>
        /// <typeparam name="TTask">Type of task to run.</typeparam>
        /// <param name="task">Task data.</param>
        /// <param name="handle"><see cref="TaskHandle"/> for checking task status.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="TaskQueue"/> has been disposed.</exception>
        public void Enqueue<TTask> (in TTask task, out TaskHandle handle)
            where TTask : struct, ITask
        {
            handle = CreateTaskHandle ();

            if (typeof (IDisposable).IsAssignableFrom (typeof (TTask))) // If T implements IDisposable
            {
                EnqueueTask (new DisposableTaskWithHandle<TaskWrapper<TTask>, Unit, Unit> (new TaskWrapper<TTask> (task), handle));
            } else
            {
                EnqueueTask (new TaskWithHandle<TaskWrapper<TTask>, Unit, Unit> (new TaskWrapper<TTask> (task), handle));
            }
        }

        /// <summary>
        /// Blocks until a task is ready in the queue, then runs it.
        /// </summary>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to observe when waiting for a task. Does not cancel actual task execution.</param>
        public void RunNextTask (IProfiler profiler = null, CancellationToken token = default) => RunNextTask (out _, profiler, token);

        /// <summary>
        /// Blocks until a task is ready in the queue, then runs it.
        /// </summary>
        /// <param name="exception"><see cref="Exception"/> thrown if task failed. Is <see langword="null"/> if task was run successfully.</param>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to observe when waiting for a task. Does not cancel actual task execution.</param>
        public void RunNextTask (out Exception exception, IProfiler profiler = null, CancellationToken token = default)
        {
            WaitForTask (token);

            if (token.IsCancellationRequested)
            {
                exception = null;
                return;
            }

            TryRunNextTask (out exception, profiler);
        }

        /// <summary>
        /// Tries to run the next task in the queue, if present. May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <returns><see langword="true"/> if a task was run, <see langword="false"/> if the <see cref="TaskQueue"/> is empty.</returns>
        /// <remarks>
        /// Please note that the return value does not indicate if a task was successful. The method will return <see langword="true"/> if a task was ready in the queue, regardless of whether an exception occured.
        /// </remarks>
        public bool TryRunNextTask (IProfiler profiler = null) => TryRunNextTask (out _, profiler);

        /// <summary>
        /// Tries to run the next task in the queue, if present. Provides an <see cref="Exception"/> thrown by the task, in case it fails.
        /// May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <param name="exception"><see cref="Exception"/> thrown if task failed. Is <see langword="null"/> if task was run successfully.</param>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <returns><see langword="true"/> if a task was run, <see langword="false"/> if the <see cref="TaskQueue"/> is empty.</returns>
        /// <remarks>
        /// Please note that the return value does not indicate if a task was successful. The method will return <see langword="true"/> if a task was ready in the queue, regardless of whether an exception occured.
        /// </remarks>
        public bool TryRunNextTask (out Exception exception, IProfiler profiler = null)
        {
            TaskRunWrapper taskRunWrapper = default;

            return TryRunNextTask (ref taskRunWrapper, out exception, profiler);
        }

        /// <summary>
        /// Removes all pending tasks from the queue. Also calls <see cref="IDisposable.Dispose"/> on tasks which implement the method.
        /// </summary>
        /// <param name="exceptionHandler">Method for handling an exception thrown by a task's <see cref="IDisposable.Dispose"/>.</param>
        /// <remarks>
        /// As the method iterates through all tasks in the queue and calls <see cref="IDisposable.Dispose"/> on tasks, it can hang for an unknown amount of time. If an exception is thrown in an <see cref="IDisposable.Dispose"/> call, the method continues on with disposing the remaining tasks.
        /// </remarks>
        public unsafe void Clear (Action<Exception> exceptionHandler = null)
        {
            bool accessDisposed = false;
            using TaskDataAccess access = new TaskDataAccess (this, &accessDisposed, false);

            foreach (int id in tasks)
            {
                ITaskInfo task = taskCache.GetTask (id);

                if (task is IDisposableTaskInfo disposableTaskInfo)
                {
                    try
                    {
                        disposableTaskInfo.Dispose (access);
                    } catch (Exception ex)
                    {
                        exceptionHandler?.Invoke (ex);
                    }
                } else
                {
                    taskDataStore.Skip (task);
                }
            }

            tasks.Clear ();
            taskHandleManager.Clear ();
            taskDataStore.Clear ();
        }

        /// <summary>
        /// Disposes all tasks which implements <see cref="IDisposable"/>.
        /// </summary>
        /// <remarks>
        /// As the method iterates through all tasks in the queue and calls <see cref="IDisposable.Dispose"/> on tasks, it can hang for an unknown amount of time. If an exception is thrown in an <see cref="IDisposable.Dispose"/> call, the method continues on with disposing the remaining tasks.
        /// </remarks>
        public void Dispose ()
        {
            lock (taskLock)
            {
                Dispose (true);
                GC.SuppressFinalize (this);
            }
        }

        /// <summary>
        /// Disposes all tasks which implements <see cref="IDisposable"/>.
        /// </summary>
        /// <remarks>
        /// As the method iterates through all tasks in the queue and calls <see cref="IDisposable.Dispose"/> on tasks, it can hang for an unknown amount of time. If an exception is thrown in an <see cref="IDisposable.Dispose"/> call, the method continues on with disposing the remaining tasks.
        /// </remarks>
        /// <param name="disposing"><see langword="true"/> if called from <see cref="Dispose ()"/>, <see langword="false"/> if called from finalizer.</param>
        protected virtual void Dispose (bool disposing)
        {
            if (disposed)
                return;

            Clear ();
            disposed = true;
        }

        protected TaskHandle CreateTaskHandle () => taskHandleManager.CreateTaskHandle ();

        /// <summary>
        /// Enqueue an <see cref="ITask"/> to be run later.
        /// </summary>
        /// <typeparam name="T">Type of task to run.</typeparam>
        /// <param name="task">Task data.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="TaskQueue"/> has been disposed.</exception>
        protected void EnqueueTask<T> (in T task) where T : struct, ITaskType
        {
            lock (taskLock)
            {
                if (disposed)
                {
                    throw new ObjectDisposedException (nameof (TaskQueue), "New tasks may not be enqueued after TaskQueue has been disposed.");
                }

                ITaskInfo<T> taskInfo = taskCache.GetTask<T> ();

                tasks.Enqueue (taskInfo.ID); // Add task ID to the queue

                // Only write task data if present
                if (taskInfo.UnmanagedSize > 0 || taskInfo.IsManaged)
                {
                    taskDataStore.Enqueue (task, taskInfo);
                }
            }

            tasksEnqueuedEvent.Set (); // Signal potentially waiting threads that tasks are ready to be executed
        }

        protected void WaitForTask (CancellationToken token)
        {
            tasksEnqueuedEvent.Wait (token);
        }

        protected void RunNextTask<TTask> (ref TTask taskRunWrapper, out Exception exception, IProfiler profiler = null, CancellationToken token = default)
            where TTask : struct, ITask<TaskRunWrapperArgs>
        {
            WaitForTask (token);

            if (token.IsCancellationRequested)
            {
                exception = null;
                return;
            }

            TryRunNextTask (ref taskRunWrapper, out exception, profiler);
        }

        protected unsafe bool TryRunNextTask<TTask> (ref TTask taskRunWrapper, out Exception exception, IProfiler profiler = null)
            where TTask : struct, ITask<TaskRunWrapperArgs>
        {
            exception = null;

            bool accessDisposed = false;
            TaskDataAccess access = new TaskDataAccess (this, &accessDisposed, true);

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

            ITaskInfo task = taskCache.GetTask (id);

            bool isProfiling = false;

            try
            {
                if (profiler != null)
                {
                    profiler.BeginTask (task.Type.FullName);
                    isProfiling = true; // If profiler was started without throwing an exception
                }

                taskRunWrapper.Run (new TaskRunWrapperArgs (task, access)); // Run the task

                if (isProfiling)
                {
                    isProfiling = false;
                    profiler.EndTask ();
                }
            } catch (Exception ex)
            {
                exception = ex;

                if (!access.Disposed) // Internal error, TaskInfo should always call TaskDataAccess.Dispose after getting task data in TaskInfo.RunAndDispose
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

        private T GetNextTaskData<T> (ITaskInfo<T> taskInfo)
            where T : struct, ITaskType => taskDataStore.Dequeue (taskInfo);

        /// <summary>
        /// Provides a way for a task to access its data while locking the <see cref="TaskQueue"/>.
        /// </summary>
        public unsafe readonly struct TaskDataAccess : IDisposable
        {
            private readonly TaskQueue queue;
            private readonly bool* disposed;
            private readonly bool disposeOnGetNextTaskData;

            /// <summary>
            /// Initializes a new instance of the <see cref="TaskDataAccess"/> struct. Locks the <see cref="TaskQueue"/>.
            /// </summary>
            /// <param name="queue">Reference to the queue.</param>
            internal TaskDataAccess (TaskQueue queue, bool* disposed, bool disposeOnGetNextTaskData)
            {
                this.queue = queue;
                this.disposed = disposed;
                this.disposeOnGetNextTaskData = disposeOnGetNextTaskData;

                Monitor.Enter (queue.taskLock);
            }

            /// <summary>
            /// Gets a value indicating whether the lock is still held.
            /// </summary>
            public readonly bool Disposed => *disposed;

            /// <summary>
            /// Fetches next data of a task.
            /// </summary>
            /// <typeparam name="TTask">Type of task.</typeparam>
            /// <param name="taskInfo">TaskInfo of task.</param>
            /// <returns>Task data.</returns>
            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            internal readonly TTask GetNextTaskData<TTask> (ITaskInfo<TTask> taskInfo)
                where TTask : struct, ITaskType
            {
                TTask taskData = queue.GetNextTaskData (taskInfo);

                if (disposeOnGetNextTaskData)
                {
                    Dispose ();
                }

                return taskData;
            }

            /// <summary>
            /// Exits the lock.
            /// </summary>
            public void Dispose ()
            {
                if (*disposed)
                    throw new ObjectDisposedException (nameof (TaskDataAccess));

                Monitor.Exit (queue.taskLock);
                *disposed = true;
            }
        }

        protected readonly struct TaskRunWrapperArgs
        {
            private readonly ITaskInfo task;

            public TaskRunWrapperArgs (ITaskInfo task, TaskDataAccess access)
            {
                this.task = task;
                Access = access;
            }

            public TaskDataAccess Access { get; }

            public T GetTaskInfoRunnable<T> () where T : IRunnableTaskInfo => (T)task;
        }

        private struct TaskRunWrapper : ITask<TaskRunWrapperArgs>
        {
            public readonly void Run (TaskRunWrapperArgs args) => args.GetTaskInfoRunnable<IRunnableTaskInfo> ().Run (args.Access);
        }
    }
}
