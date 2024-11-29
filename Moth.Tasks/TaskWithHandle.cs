namespace Moth.Tasks
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Encapsulates a task enqueued with a <see cref="TaskHandle"/>.
    /// </summary>
    /// <typeparam name="TTask">Type of task.</typeparam>
    /// <typeparam name="TArg">Type of task argument.</typeparam>
    [StructLayout (LayoutKind.Auto)]
    internal struct TaskWithHandle<TTask, TArg, TResult> : ITask<TArg, TResult>, IDisposable
        where TTask : struct, ITask<TArg, TResult>
    {
        private TTask task;
        private TaskHandle handle;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskWithHandle{TTask, TArg, TResult}"/> struct.
        /// </summary>
        /// <param name="task">Task data.</param>
        /// <param name="handle">Task handle.</param>
        public TaskWithHandle (in TTask task, TaskHandle handle)
        {
            this.task = task;
            this.handle = handle;
        }

        /// <inheritdoc/>
        public TResult Run (TArg arg) => task.Run (arg);

        /// <summary>
        /// Notify handle that task was completed.
        /// </summary>
        public void Dispose ()
        {
            handle.NotifyTaskCompletion ();
        }
    }
}
