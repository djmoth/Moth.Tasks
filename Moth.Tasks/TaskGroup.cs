namespace Moth.Tasks
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;

    /// <summary>
    /// Represents a group of tasks that can be tracked and waited on.
    /// </summary>
    public unsafe class TaskGroup : IDisposable
    {
        private readonly object whenCompleteLock = new object ();
        private Counter* counter;
        private Action whenComplete;
        private GCHandle thisHandle;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskGroup"/> class.
        /// </summary>
        public TaskGroup ()
        {
            counter = (Counter*)Marshal.AllocHGlobal (sizeof (Counter));

            thisHandle = GCHandle.Alloc (this, GCHandleType.Normal);
            *counter = new Counter (thisHandle);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TaskGroup"/> class.
        /// </summary>
        ~TaskGroup ()
        {
            Dispose (false);
        }

        /// <summary>
        /// Gets a value indicating whether the object has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Gets the current progress of the task group, as a value between 0 and 1.
        /// </summary>
        /// <remarks>
        /// Will be 1 if <see cref="TaskCount"/> is 0.
        /// </remarks>
        public float Progress => GetProgress ();

        /// <summary>
        /// Gets the number of tasks in the group.
        /// </summary>
        public int TaskCount => !IsDisposed ? counter->TaskCount : throw new ObjectDisposedException (nameof (TaskGroup));

        /// <summary>
        /// Gets the number of tasks that have completed.
        /// </summary>
        public int CompletedCount => !IsDisposed ? counter->CompletedCount : throw new ObjectDisposedException (nameof (TaskGroup));

        /// <summary>
        /// Gets a value indicating whether all tasks in the group have completed.
        /// </summary>
        /// <remarks>
        /// Will be <see langword="true"/> if <see cref="TaskCount"/> is 0.
        /// </remarks>
        public bool IsComplete => !IsDisposed ? counter->IsComplete : throw new ObjectDisposedException (nameof (TaskGroup));

        /// <summary>
        /// Adds an action to be invoked when all tasks in the group have completed.
        /// If the tasks have already completed, the action is invoked immediately.
        /// </summary>
        /// <remarks>
        /// The action is invoked on the thread that completes the last task in the group.
        /// </remarks>
        /// <param name="action">The action to invoke.</param>
        public void WhenComplete (Action action)
        {
            if (IsDisposed)
                throw new ObjectDisposedException (nameof (TaskGroup));

            lock (whenCompleteLock)
            {
                if (counter->IsComplete)
                {
                    action ();
                } else
                {
                    if (whenComplete == null)
                        whenComplete = action;
                    else
                        whenComplete += action;
                }
            }
        }

        /// <summary>
        /// Enqueues a task in the specified queue.
        /// </summary>
        /// <typeparam name="T">Type of task.</typeparam>
        /// <param name="queue">The <see cref="TaskQueue"/> to enqueue the task in.</param>
        /// <param name="task">The task to enqueue.</param>
        public void Enqueue<T> (ITaskQueue queue, in T task)
            where T : struct, ITask
        {
            if (IsDisposed)
                throw new ObjectDisposedException (nameof (TaskGroup));

            counter->AddTask ();
            queue.Enqueue (new TaskGroupItem<T> (counter, task));
        }

        /// <summary>
        /// Disposes of the task group.
        /// </summary>
        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        /// <summary>
        /// Disposes of the task group.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if called from <see cref="Dispose()"/>.</param>
        /// <exception cref="InvalidOperationException"><see cref="IsComplete"/> is not false.</exception>
        protected virtual void Dispose (bool disposing)
        {
            if (!IsDisposed)
            {
                lock (whenCompleteLock)
                {
                    if (counter->IsComplete)
                    {
                        Marshal.FreeHGlobal ((IntPtr)counter);
                    } else
                    {
                        throw new InvalidOperationException ($"Cannot dispose {nameof(TaskGroup)} when {nameof(IsComplete)} is true.");
                    }
                }

                thisHandle.Free ();
                counter = null;
                IsDisposed = true;
            }
        }

        private float GetProgress ()
        {
            if (IsDisposed)
                throw new ObjectDisposedException (nameof (TaskGroup));

            if (counter->TaskCount == 0)
                return 1;
            else
                return (float)counter->CompletedCount / counter->TaskCount;
        }

        internal struct Counter
        {
            private GCHandle groupHandle;
            private int completedCount;
            private int taskCount;

            public Counter (GCHandle groupHandle)
            {
                this.groupHandle = groupHandle;
                completedCount = 0;
                taskCount = 0;
            }

            public readonly GCHandle GroupHandle => groupHandle;

            public int TaskCount => Volatile.Read (ref taskCount);

            public int CompletedCount => Volatile.Read (ref completedCount);

            public bool IsComplete => CompletedCount == TaskCount;

            public void AddTask ()
            {
                Interlocked.Increment (ref taskCount);
            }

            public void MarkComplete ()
            {
                int newCompletedCount = Interlocked.Increment (ref completedCount);

                if (newCompletedCount == Volatile.Read (ref taskCount))
                {
                    TaskGroup group = (TaskGroup)groupHandle.Target;

                    lock (group.whenCompleteLock)
                    {
                        group.whenComplete?.Invoke ();
                    }
                }
            }
        }

        internal unsafe struct TaskGroupItem<T> : ITask, IDisposable
            where T : struct, ITask
        {
            private readonly Counter* groupCounter;
            private T task;

            public TaskGroupItem (Counter* groupCounter, T task)
            {
                this.groupCounter = groupCounter;
                this.task = task;
            }

            public void Run () => task.Run ();

            public readonly void Dispose ()
            {
                groupCounter->MarkComplete ();
            }
        }
    }
}
