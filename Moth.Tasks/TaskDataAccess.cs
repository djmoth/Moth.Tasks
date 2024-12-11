namespace Moth.Tasks
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;


    /// <summary>
    /// Provides a way for a task to access its data while locking the <see cref="TaskQueue"/>.
    /// </summary>
    public unsafe readonly struct TaskDataAccess : IDisposable
    {
        private readonly ITaskDataAccess access;
        private readonly bool* disposed;
        private readonly bool disposeOnGetNextTaskData;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskDataAccess"/> struct. Locks the <see cref="ITaskDataAccess"/>.
        /// </summary>
        /// <param name="access">Reference to the task data access object.</param>
        /// <param name="disposed">Pointer to the disposed flag.</param>
        /// <param name="disposeOnGetNextTaskData">Whether to dispose the lock after getting the next task data.</param>
        internal TaskDataAccess (ITaskDataAccess access, bool* disposed, bool disposeOnGetNextTaskData)
        {
            this.access = access;
            this.disposed = disposed;
            this.disposeOnGetNextTaskData = disposeOnGetNextTaskData;

            access.EnterLock ();
        }

        /// <summary>
        /// Gets a value indicating whether the lock is still held.
        /// </summary>
        public readonly bool Disposed => *disposed;

        /// <summary>
        /// Fetches next data of a task.
        /// </summary>
        /// <typeparam name="TTask">Type of task.</typeparam>
        /// <param name="taskInfo">TaskMetadata of task.</param>
        /// <returns>Task data.</returns>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        internal readonly TTask GetNextTaskData<TTask> (ITaskMetadata<TTask> taskInfo)
            where TTask : struct, ITask
        {
            TTask taskData = access.GetNextTaskData (taskInfo);

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

            access.ExitLock ();
            *disposed = true;
        }
    }
}
