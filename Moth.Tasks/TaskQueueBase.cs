using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System;

namespace Moth.Tasks
{
    public abstract class TaskQueueBase : IDisposable
    {
        private readonly object taskLock = new object ();
        private readonly TaskCache taskCache = new TaskCache ();
        private readonly ManualResetEventSlim tasksEnqueuedEvent = new ManualResetEventSlim (); // Must be explicitly set by callers of EnqueueImpl
        private readonly Queue<int> tasks;
        private readonly TaskDataStore taskData;
        private readonly TaskHandleManager taskHandleManager;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskQueueBase"/> class.
        /// </summary>
        public TaskQueueBase ()
            : this (16, 1024) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskQueueBase"/> class.
        /// </summary>
        /// <param name="taskCapacity">Starting capacity for the internal task queue.</param>
        /// <param name="dataCapacity">Starting capacity for the internal task data array.</param>
        internal TaskQueueBase (int taskCapacity, int dataCapacity)
        {
            tasks = new Queue<int> (taskCapacity);
            taskData = new TaskDataStore (dataCapacity);
            taskHandleManager = new TaskHandleManager ();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TaskQueueBase"/> class. Also disposes of any enqueued tasks implementing <see cref="IDisposable.Dispose"/>.
        /// </summary>
        ~TaskQueueBase () => Dispose (false);

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

        protected TaskCache TaskCache => taskCache;

        protected TaskHandleManager TaskHandleManager => taskHandleManager;

        /// <summary>
        /// Removes all pending tasks from the queue. Also calls <see cref="IDisposable.Dispose"/> on tasks which implement the method.
        /// </summary>
        /// <param name="exceptionHandler">Method for handling an exception thrown by a task's <see cref="IDisposable.Dispose"/>.</param>
        /// <remarks>
        /// As the method iterates through all tasks in the queue and calls <see cref="IDisposable.Dispose"/> on tasks, it can hang for an unknown amount of time. If an exception is thrown in an <see cref="IDisposable.Dispose"/> call, the method continues on with disposing the remaining tasks.
        /// </remarks>
        public void Clear (Action<Exception> exceptionHandler = null)
        {
            using TaskDataAccess access = new TaskDataAccess (this);

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
                    taskData.Skip (task);
                }
            }

            tasks.Clear ();
            taskHandleManager.Clear ();
            taskData.Clear ();
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
                    taskData.Enqueue (task, taskInfo);
                }
            }

            tasksEnqueuedEvent.Set (); // Signal potentially waiting threads that tasks are ready to be executed
        }

        protected void WaitForTask (CancellationToken token)
        {
            tasksEnqueuedEvent.Wait (token);
        }

        protected bool TryGetNextTask (out ITaskInfo task, out TaskDataAccess dataAccess)
        {
            dataAccess = new TaskDataAccess (this);

            if (tasks.Count == 0)
            {
                task = default;

                dataAccess.Dispose ();
                return false;
            }

            int id = tasks.Dequeue ();

            if (tasks.Count == 0)
            {
                tasksEnqueuedEvent.Reset (); // All tasks have been fetched, and as such the event can be reset.
            }

            task = taskCache.GetTask (id);
            return true;
        }

        private T GetNextTaskData<T> (ITaskInfo<T> taskInfo)
            where T : struct, ITaskType => taskData.Dequeue (taskInfo);

        /// <summary>
        /// Provides a way for a task to access its data while locking the <see cref="TaskQueue"/>.
        /// </summary>
        public unsafe ref struct TaskDataAccess
        {
            private TaskQueueBase queue;

            /// <summary>
            /// Initializes a new instance of the <see cref="TaskDataAccess"/> struct. Locks the <see cref="TaskQueue"/>.
            /// </summary>
            /// <param name="queue">Reference to the queue.</param>
            internal TaskDataAccess (TaskQueueBase queue)
            {
                this.queue = queue;
                Monitor.Enter (queue.taskLock);
            }

            /// <summary>
            /// Gets a value indicating whether the lock is still held.
            /// </summary>
            public readonly bool Disposed => queue == null;

            /// <summary>
            /// Fetches next data of a task.
            /// </summary>
            /// <typeparam name="T">Type of task.</typeparam>
            /// <param name="task">TaskInfo of task.</param>
            /// <returns>Task data.</returns>
            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            internal readonly T GetNextTaskData<T> (ITaskInfo<T> task) where T : struct, ITaskType => queue.GetNextTaskData (task);

            /// <summary>
            /// Exits the lock.
            /// </summary>
            public void Dispose ()
            {
                Monitor.Exit (queue.taskLock);
                queue = null;
            }
        }
    }
}
