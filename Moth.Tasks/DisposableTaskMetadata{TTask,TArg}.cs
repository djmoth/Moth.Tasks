namespace Moth.Tasks
{
    using System;
    using Moth.IO.Serialization;

    /// <summary>
    /// Represents a task that can be disposed and takes an argument and returns no result.
    /// </summary>
    /// <inheritdoc />
    internal class DisposableTaskMetadata<TTask, TArg> : DisposableTaskMetadataBase<TTask>, IRunnableTaskMetadata<TArg>
        where TTask : struct, ITask<TArg>, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableTaskMetadata{TTask, TArg}"/> class.
        /// </summary>
        /// <inheritdoc cref="TaskMetadataBase{TTask}(int, IFormat{TTask})"/>
        public DisposableTaskMetadata (int id, IFormat<TTask> taskFormat)
            : base (id, taskFormat) { }

        /// <inheritdoc />
        public override bool HasArgs => true;

        /// <inheritdoc />
        public override bool HasResult => false;

        /// <summary>
        /// Runs the task with <see langword="default"/> as argument and disposes it.
        /// </summary>
        /// <inheritdoc />
        public void Run (TaskQueue.TaskDataAccess access)
        {
            TTask data = access.GetNextTaskData (this);

            try
            {
                data.Run (default);
            } finally
            {
                data.Dispose ();
            }
        }

        /// <summary>
        /// Runs the task with an argument and disposes it.
        /// </summary>
        /// <inheritdoc />
        public void Run (TaskQueue.TaskDataAccess access, TArg arg)
        {
            TTask data = access.GetNextTaskData (this);

            try
            {
                data.Run (arg);
            } finally
            {
                data.Dispose ();
            }
        }
    }
}
